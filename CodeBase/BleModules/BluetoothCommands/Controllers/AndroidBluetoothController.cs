using System;
using System.Collections.Generic;
using System.Threading;
using Android.BLE;
using Android.BLE.Commands;
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
    [RequireComponent(typeof(BleManager))]
    [RequireComponent(typeof(BleAdapter))]
    
    public class AndroidBluetoothController : MonoBehaviour, IBluetoothCommandsController
    {
        [SerializeField] private BleManager bleManager;
        
        public Action OnDeviceStarted { get; set; }
        public Action<byte[]> OnDataReceived { get; set; }
        
        private static string _lastConnectedMacAddress;

        private DeviceConnectionStatus _deviceConnectionStatus = DeviceConnectionStatus.None;
        
        private ConnectToDevice _connectCommand;
        private DiscoverDevices _scanCommand;
        private WriteToCharacteristic _writeCommand;
        private SubscribeToCharacteristic _subscribeCommand;

        private List<ScanDevice> _findedDevices = new();

        private BraydenGlobalSettingsManager _settings;
        private int _scanTime;
        
        private void Awake()
        {
            if (bleManager == null)
                bleManager = GetComponent<BleManager>();
        }

        public void Initialize(BraydenGlobalSettingsManager settings)
        {
            bleManager.Initialize();
            WriteLog(this, BluetoothCommandType.Init, "Initialized");
            _settings = settings;
            _scanTime = _settings.ScanManager.ScanTime;
        }

        public void Deinitialize()
        {
            _deviceConnectionStatus = DeviceConnectionStatus.None;
            StopScanning();
            
            _subscribeCommand?.End();
            _writeCommand?.End();
            _connectCommand?.Disconnect();

            _subscribeCommand = null;
            _writeCommand = null;
            _connectCommand = null;
           
            _findedDevices?.Clear();
            _findedDevices = new List<ScanDevice>();
            
            WriteLog(this, BluetoothCommandType.Deinit, "Deinitialized");
        }

        #region Scan logic
        public async UniTask<List<ScanDevice>> StartScanningAsync(CancellationToken token)
        {
            WriteLog(this, BluetoothCommandType.Scanning, "Start scanning async...");
            _findedDevices = new List<ScanDevice>();
            
            var scanTimeMilliseconds = (int)TimeSpan.FromSeconds(_scanTime).TotalMilliseconds;
            
            _scanCommand = new DiscoverDevices(DeviceDiscoveredCallback, scanTimeMilliseconds);
            bleManager.QueueCommand(_scanCommand);
            
            await UniTask.Delay(TimeSpan.FromSeconds(_scanTime), cancellationToken: token);
            
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

        public void StopScanning()
        {
            _scanCommand?.End();
            _scanCommand = null;
            
            WriteLog(this, BluetoothCommandType.Scanning, "Scanning stopped");
        }

        #endregion

        public async UniTask<DeviceConnectionStatus> ConnectToDevice(ScanDevice device, CancellationToken token)
        {
            _deviceConnectionStatus = DeviceConnectionStatus.None;
            
            var deviceMac = device.MacAddress;
            _connectCommand = new ConnectToDevice(deviceMac, DeviceConnectedCallback, DeviceDisconnectedCallback, null, null);
            bleManager.QueueCommand(_connectCommand);
            
            WriteLog(this, BluetoothCommandType.Connection, $"Started connection to: {device}");
            
            await UniTask.WaitWhile(() => _deviceConnectionStatus == DeviceConnectionStatus.None, cancellationToken: token);
            
            return _deviceConnectionStatus;
        }

        private void DeviceConnectedCallback(string deviceMacAddress)
        {
            _lastConnectedMacAddress = deviceMacAddress;
            _deviceConnectionStatus = DeviceConnectionStatus.Connected;
            
            WriteLog(this, BluetoothCommandType.Connection, $"Connected to: {deviceMacAddress}");
        }

        private void DeviceDisconnectedCallback(string deviceMacAddress)
        {
            WriteLog(this, BluetoothCommandType.Disconnection, $"Disconnected from: {deviceMacAddress}");
            _deviceConnectionStatus = DeviceConnectionStatus.Disconnected;
        }

        public void DisconnectFromDevice()
        {
            Deinitialize();
        }

        public void WriteCharacteristic(byte[] bytes)
        {
            var deviceMac = _lastConnectedMacAddress;

            WriteLog(this, BluetoothCommandType.WriteCommand, "Starting write. " + "\n" +
                                                              "Parameters:\n" +
                                                              $"Device: {deviceMac} " + "\n" +
                                                              $"Service: {SERVICE_UUID} " + "\n" +
                                                              $"WriteRX: {WRITE_RX_UUID} " + "\n" +
                                                              $"Bytes: {string.Join(" | ", bytes)}");
            
            _writeCommand = new WriteToCharacteristic(deviceMac, SERVICE_UUID, WRITE_RX_UUID, bytes, true);
            bleManager.QueueCommand(_writeCommand);
        }

        public void SubscribeToCharacteristic()
        {
            var deviceMac = _lastConnectedMacAddress;

            WriteLog(this, BluetoothCommandType.SubscribeCommand, $"Starting to subscribe: {READ_TX_UUID}");

            _subscribeCommand = new SubscribeToCharacteristic(deviceMac, SERVICE_UUID, READ_TX_UUID, DeviceDataReceivedCallback, true);
            OnDeviceStarted?.Invoke();
            bleManager.QueueCommand(_subscribeCommand);
        }

        private void DeviceDataReceivedCallback(byte[] value)
        {
            OnDataReceived?.Invoke(value);
        }

        public void UnsubscribeFromCharacteristic()
        {
            _subscribeCommand?.End();
            WriteLog(this, BluetoothCommandType.Unsubscribe, $"Unsubscribed from {READ_TX_UUID}");
        }

        public string GetPlatformName()
        {
            return "Android";
        }
    }
}