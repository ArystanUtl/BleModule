using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeBase;
using CodeBase.BleModules;
using CodeBase.Settings;
using Cysharp.Threading.Tasks;
using Modules.Common;
using UnityEngine;
using static CodeBase.BleConstants;
using static CodeBase.BleModules.BleLogger;

namespace Modules.BluetoothBase.CodeBase.BleModules.BluetoothCommands.Controllers
{
    public class WinBluetoothController : MonoBehaviour, IBluetoothCommandsController
    {
        public Action OnDeviceStarted { get; set; }
        public Action<byte[]> OnDataReceived { get; set; }
        
        private static string _lastConnectedMacAddress;
        
        private DeviceConnectionStatus _deviceConnectionStatus = DeviceConnectionStatus.None;
        private List<ScanDevice> _findedDevices = new();
        
        private bool _isScanningDevices;
        private bool _isScanningServices;
        private bool _isSubscribedToCharacteristic;

        private BraydenGlobalSettingsManager _settings;
        private int _scanTime;

        public void Initialize(BraydenGlobalSettingsManager settings)
        {
            BleApi.Quit();
            WriteLog(this, BluetoothCommandType.Init, "Initialized");

            _settings = settings;
            _scanTime = _settings.ScanManager.ScanTime;
        }

        public void Deinitialize()
        {
            _deviceConnectionStatus = DeviceConnectionStatus.None;
            
            StopScanning();
            BleApi.Quit();

            _isScanningServices = false;
            _isScanningDevices = false;
            _isSubscribedToCharacteristic = false;
      
            _findedDevices?.Clear();
            _findedDevices = new List<ScanDevice>();
            
            WriteLog(this, BluetoothCommandType.Deinit, "Deinitialized");
        }

        #region Scan logic

        public async UniTask<List<ScanDevice>> StartScanningAsync(CancellationToken token)
        {
            WriteLog(this, BluetoothCommandType.Scanning, "Start scanning async...");
            
            _findedDevices = new List<ScanDevice>();
            BleApi.StartDeviceScan();
            _isScanningDevices = true;

            await UniTask.Delay(TimeSpan.FromSeconds(_scanTime), cancellationToken: token);

            _isScanningDevices = false;

            WriteLog(this, BluetoothCommandType.Scanning, $"Discovered devices count: {_findedDevices.Count}");

            var filteredDevices = _settings.ScanManager.FilterDevicesByName(_findedDevices);
            return filteredDevices;
        }

        private void DeviceDiscoveredCallback(string deviceMacAddress, string deviceName)
        {
            if (deviceMacAddress.IsNullOrEmpty() || deviceName.IsNullOrEmpty())
                return;
            
            var device = new ScanDevice(deviceName, deviceMacAddress);
            _findedDevices.Add(device);

            WriteLog(this, BluetoothCommandType.DeviceDiscovered, $"{device}");
        }

        private void PollDevices()
        {
            BleApi.ScanStatus status;
            var res = new BleApi.DeviceUpdate();
            do
            {
                status = BleApi.PollDevice(ref res, false);
                switch (status)
                {
                    case BleApi.ScanStatus.AVAILABLE:
                    {
                        if (res is { nameUpdated: true, isConnectableUpdated: true })
                        {
                            var deviceMacAddress = res.id;
                            var deviceName = res.name;

                            DeviceDiscoveredCallback(deviceMacAddress, deviceName);
                        }
                        
                        break;
                    }
                    case BleApi.ScanStatus.FINISHED:
                        _isScanningDevices = false;
                        break;
                    
                    case BleApi.ScanStatus.PROCESSING:
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            } 
            while (status == BleApi.ScanStatus.AVAILABLE);
        }

        private void PollServices()
        {
            BleApi.ScanStatus status;
            _isScanningDevices = false;
            BleApi.StopDeviceScan();
            
            do
            {
                status = BleApi.PollService(out _, false);
                    
                if (status is not BleApi.ScanStatus.FINISHED)
                    continue;
                    
                _isScanningServices = false;
                _isScanningDevices = false;
                _isSubscribedToCharacteristic = false;
                
                DeviceConnectedCallback(_lastConnectedMacAddress);
                break;
            } 
            while (status == BleApi.ScanStatus.AVAILABLE);
        }

        private void PollSubscribedCharacteristic()
        {
            while (BleApi.PollData(out var res, false))
            {
                if (!res.buf.IsCorrect() || res.buf.Length < RECEIVED_DATA_REQUIRED_LENGTH)
                    continue;
                    
                var gettingBytes = res.buf.ToList().GetRange(0, RECEIVED_DATA_REQUIRED_LENGTH).ToArray();
                DeviceDataReceivedCallback(gettingBytes);
            }
        }

        private void Update()
        {
            if (_isScanningDevices)
                PollDevices();
            
            if (_isScanningServices)
                PollServices();

            if (_isSubscribedToCharacteristic)
                PollSubscribedCharacteristic();
        }

        public void StopScanning()
        {
            BleApi.StopDeviceScan();
            _isScanningDevices = false;

            WriteLog(this, BluetoothCommandType.Scanning, "Scanning stopped");
        }

        #endregion

        public async UniTask<DeviceConnectionStatus> ConnectToDevice(ScanDevice device, CancellationToken token)
        {
            _deviceConnectionStatus = DeviceConnectionStatus.None;
            
            var deviceMac = device.MacAddress;
            _lastConnectedMacAddress = deviceMac;
           
            WriteLog(this, BluetoothCommandType.Connection, $"Started connection to: {device}");
            
            _isScanningServices = true;
            
            BleApi.ScanServices(deviceMac);
            
            await UniTask.WaitWhile(() => _deviceConnectionStatus == DeviceConnectionStatus.None, cancellationToken: token);
            
            _isScanningServices = false;
            
            return _deviceConnectionStatus;
        }

        private void DeviceConnectedCallback(string deviceMacAddress)
        {
            _deviceConnectionStatus = DeviceConnectionStatus.Connected;
            
            WriteLog(this, BluetoothCommandType.Connection, $"Connected to: {deviceMacAddress}");
        }
        
        public void DisconnectFromDevice()
        {
            Deinitialize();
        }

        public void WriteCharacteristic(byte[] bytes)
        {
            if (bytes.IsNullOrEmpty())
                return;
            
            var deviceMac = _lastConnectedMacAddress;
            var requiredLength = bytes.Length;
            var data = new BleApi.BLEData
            {
                buf = new byte[requiredLength],
                size = (short)requiredLength,
                deviceId = deviceMac,
                serviceUuid = SERVICE_UUID,
                characteristicUuid = WRITE_RX_UUID
            };

            for (var i = 0; i < bytes.Length; i++)
                data.buf[i] = bytes[i];

            BleApi.SendData(in data, false);
        }

        public void SubscribeToCharacteristic()
        {
            var deviceMac = _lastConnectedMacAddress;

            WriteLog(this, BluetoothCommandType.SubscribeCommand, $"Starting to subscribe: {READ_TX_UUID}");

            BleApi.SubscribeCharacteristic(deviceMac, SERVICE_UUID, READ_TX_UUID, false);
            _isSubscribedToCharacteristic = true;
            
            OnDeviceStarted?.Invoke();
        }

        private void DeviceDataReceivedCallback(byte[] value)
        {
            OnDataReceived?.Invoke(value);
        }

        public void UnsubscribeFromCharacteristic()
        {
            //TODO: Win plugin doesn't contains method for unsubscribe...
        }

        public string GetPlatformName()
        {
            return "Windows";
        }
    }
}