#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.Threading;
using CodeBase;
using CodeBase.BleModules;
using CodeBase.Settings;
using Cysharp.Threading.Tasks;
using Modules.Common;
using Plugins;
using UnityEngine;

using static CodeBase.BleConstants;
using static CodeBase.BleModules.BleLogger;

namespace Modules.BluetoothBase.CodeBase.BleModules.BluetoothCommands.Controllers
{
    public class IosBluetoothController: MonoBehaviour, IBluetoothCommandsController
    {
        public Action OnDeviceStarted { get; set; }
        public Action<byte[]> OnDataReceived { get; set; }
        
        private static string _lastConnectedMacAddress;
        private DeviceConnectionStatus _deviceConnectionStatus = DeviceConnectionStatus.None;
        
        private List<ScanDevice> _findedDevices = new();

        private BraydenGlobalSettingsManager _settings;
        private int _scanTime;

        public void Initialize(BraydenGlobalSettingsManager settings)
        {
            BluetoothLeHardwareInterface.Initialize(true, false, OnInitializedCallback, OnErrorCallback);
            _settings = settings;
            _scanTime = _settings.ScanManager.ScanTime;
            WriteLog(this, BluetoothCommandType.Init, "Initialized");
        }

        private void OnInitializedCallback()
        {
            WriteLog(this, BluetoothCommandType.Init, "Initialized");
        }

        private void OnErrorCallback(string obj)
        {
            WriteLog(this, BluetoothCommandType.Error, $"Not initialized: {obj}");
        }

        public void Deinitialize()
        {
            _deviceConnectionStatus = DeviceConnectionStatus.None;
            StopScanning();
           
            _findedDevices?.Clear();
            _findedDevices = new List<ScanDevice>();

            BluetoothLeHardwareInterface.DisconnectPeripheral(_lastConnectedMacAddress, DeviceDisconnectedCallback);
            BluetoothLeHardwareInterface.DisconnectAll();
            
            WriteLog(this, BluetoothCommandType.Deinit, "Deinitialized");
        }

        #region Scan logic
        public async UniTask<List<ScanDevice>> StartScanningAsync(CancellationToken token)
        {
            WriteLog(this, BluetoothCommandType.Scanning, "Start scanning async...");
            _findedDevices = new List<ScanDevice>();

            BluetoothLeHardwareInterface.ScanForPeripheralsWithServices(null, DeviceDiscoveredCallback, DeviceAdvertisingCallback, true);
            
            await UniTask.Delay(TimeSpan.FromSeconds(_scanTime), cancellationToken: token);
            
            BluetoothLeHardwareInterface.StopScan();
            
            WriteLog(this, BluetoothCommandType.Scanning, $"Discovered devices count: {_findedDevices.Count}");
            var filteredDevices = _settings.ScanManager.FilterDevicesByName(_findedDevices);
            return filteredDevices;
        }

        private void DeviceAdvertisingCallback(string deviceMacAddress, string deviceName, int signalValue, byte[] bytes)
        {
            DeviceDiscoveredCallback(deviceMacAddress, deviceName);
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
            BluetoothLeHardwareInterface.StopScan();
            WriteLog(this, BluetoothCommandType.Scanning, "Scanning stopped");
        }

        #endregion

        public async UniTask<DeviceConnectionStatus> ConnectToDevice(ScanDevice device, CancellationToken token)
        {
           var deviceMac = device.MacAddress;
           _deviceConnectionStatus = DeviceConnectionStatus.None;

            WriteLog(this, BluetoothCommandType.Connection, $"Started connection to: {device}");

            BluetoothLeHardwareInterface.ConnectToPeripheral(deviceMac, DeviceConnectedCallback, null, null, DeviceDisconnectedCallback);
            
            await UniTask.WaitWhile(() => _deviceConnectionStatus == DeviceConnectionStatus.None, cancellationToken: token);
            
            WriteLog(this, BluetoothCommandType.Connection, $"Status after connect: {_deviceConnectionStatus.ToString()}");
            
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
            _deviceConnectionStatus = DeviceConnectionStatus.Disconnected;
            WriteLog(this, BluetoothCommandType.Disconnection, $"Disconnected from: {deviceMacAddress}");
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

            BluetoothLeHardwareInterface.WriteCharacteristic(deviceMac, SERVICE_UUID, WRITE_RX_UUID, bytes, false, null);
        }

        public void SubscribeToCharacteristic()
        {
            var deviceMac = _lastConnectedMacAddress;

            WriteLog(this, BluetoothCommandType.SubscribeCommand, $"Starting to subscribe: {READ_TX_UUID}");

            OnDeviceStarted?.Invoke();
            
            BluetoothLeHardwareInterface.SubscribeCharacteristicWithDeviceAddress(deviceMac, SERVICE_UUID, READ_TX_UUID,null, OnSubscribeCallback);
        }

        private void OnSubscribeCallback(string macAddress, string characteristic, byte[] receivedBytes)
        {
            OnDataReceived?.Invoke(receivedBytes);
        }

        public void UnsubscribeFromCharacteristic()
        {
            WriteLog(this, BluetoothCommandType.Unsubscribe, $"Unsubscribed from {READ_TX_UUID}");
            
            if (_lastConnectedMacAddress.IsCorrect())
                BluetoothLeHardwareInterface.UnSubscribeCharacteristic(_lastConnectedMacAddress, SERVICE_UUID, READ_TX_UUID, null);
        }

        public string GetPlatformName()
        {
            return "IOS";
        }
    }
}
#endif