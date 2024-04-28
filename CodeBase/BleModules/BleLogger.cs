using UnityEngine;

namespace CodeBase.BleModules
{
    public static class BleLogger
    {
        public static void WriteLog(IBluetoothCommandsController platformController, BluetoothCommandType commandType, string logMessage)
        {
            var platformName = platformController.GetPlatformName();
            
            if (commandType is BluetoothCommandType.Error)
            {
                Debug.LogError(string.Concat($"[BLE] [{platformName}] [", commandType.ToString(), "] ", logMessage));
                return;
            }

            Debug.Log(string.Concat($"[BLE] [{platformName}] [", commandType.ToString(), "] ", logMessage));
        }
    }
}