using System;
using System.Collections.Generic;
using System.Threading;
using CodeBase.Settings;
using Cysharp.Threading.Tasks;

namespace CodeBase.BleModules
{
    public interface IBluetoothCommandsController
    {
        public Action OnDeviceStarted { get; set; }
        public Action<byte[]> OnDataReceived { get; set; }
        
        public void Initialize(BraydenGlobalSettingsManager settings);
        public void Deinitialize();

        public UniTask<List<ScanDevice>> StartScanningAsync(CancellationToken token);
        public void StopScanning();
        
        public UniTask<DeviceConnectionStatus> ConnectToDevice(ScanDevice device, CancellationToken token);
        
        public void DisconnectFromDevice();

        public void WriteCharacteristic(byte[] bytes);
        public void SubscribeToCharacteristic();
        public void UnsubscribeFromCharacteristic();

        public string GetPlatformName();
    }
}