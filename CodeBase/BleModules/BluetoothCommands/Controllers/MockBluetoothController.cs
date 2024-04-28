using System;
using System.Collections.Generic;
using System.Threading;
using CodeBase.BleModules;
using CodeBase.Settings;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Modules.BluetoothBase.CodeBase.BleModules.BluetoothCommands.Controllers
{
    public class MockBluetoothController : MonoBehaviour, IBluetoothCommandsController
    {
        public Action OnDeviceStarted { get; set; }
        public Action<byte[]> OnDataReceived { get; set; }
        
        public void Initialize(BraydenGlobalSettingsManager settings)
        {
           
        }

        public void Deinitialize()
        {
           
        }

        public async UniTask<List<ScanDevice>> StartScanningAsync(CancellationToken token)
        {
            await UniTask.Yield();
            return new List<ScanDevice>();
        }

        public void StopScanning()
        {
         
        }

        public async UniTask<DeviceConnectionStatus> ConnectToDevice(ScanDevice device, CancellationToken token)
        {
            await UniTask.Yield();
            return DeviceConnectionStatus.Disconnected;
        }

        public void DisconnectFromDevice()
        {
           
        }

        public void WriteCharacteristic(byte[] bytes)
        {
            
        }

        public void SubscribeToCharacteristic()
        {
          
        }

        public void UnsubscribeFromCharacteristic()
        {
           
        }

        public string GetPlatformName()
        {
            return $"Not supported platform: {Application.platform.ToString()}";
        }
    }
}