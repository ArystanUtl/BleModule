using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using CodeBase;
using CodeBase.BleModules;
using CodeBase.Hints;
using CodeBase.Settings;
using Cysharp.Threading.Tasks;
using Modules.Books;
using Modules.Loading;
using UnityEngine;
using Zenject;
using Timer = Modules.General.Timer;

namespace Modules.BluetoothBase.CodeBase.BleModules
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class BleController : MonoBehaviour
    {
        [SerializeField] private BleHintController hintController;
        [SerializeField] private BraydenMonitorController braydenMonitorController;
        [SerializeField] private BleDevicesVisualizer devicesVisualizer;
    
        [Inject] private IBluetoothCommandsController _bluetoothCommandsController;
        [Inject] private ILoading _loading;
        [Inject] private Timer _timer;
        [Inject] private BookDatabase _bookDatabase;

        public BraydenGlobalSettingsManager SettingsManager { get; private set; }
        private MannequinState MannequinCurrentState { get; set; } = MannequinState.Disconnected;
    
        private CancellationTokenSource _cts;
        

        public void ResetCTS()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
        }
    
        public void Initialize(Action onConnected)
        {
            SettingsManager = new BraydenGlobalSettingsManager(_bookDatabase);

            braydenMonitorController.Init(SettingsManager);
            
            _bluetoothCommandsController.Initialize(SettingsManager);

            devicesVisualizer.OnHelpButtonClicked = null;
            devicesVisualizer.OnHelpButtonClicked += () =>
            {
                ResetCTS();
                ShowHintPanel(onConnected);
            };

            if (BleHintController.IsNeedShowHintPanel())
            {
                ShowHintPanel(onConnected);
            }
            else
            {
                ResetCTS();
                ActivateScanWindow(onConnected, _cts.Token).Forget();
            }
        }

        private void ShowHintPanel(Action onFinished)
        {
            ResetCTS();
            devicesVisualizer.SetScanPanelVisibleStatus(false);
            devicesVisualizer.SetDevicesPanelVisibleStatus(false);

            hintController.ShowHintPanel(() =>
            {
                devicesVisualizer.SetScanPanelVisibleStatus(true);
                ScanDevicesAndShowPanel(onFinished, _cts.Token).Forget();
            });
        }

        private async UniTask ActivateScanWindow(Action onSuccess, CancellationToken token)
        {
            devicesVisualizer.SetDevicesPanelVisibleStatus(false);
            devicesVisualizer.SetScanPanelVisibleStatus(true);

            await ScanDevicesAndShowPanel(onSuccess, token);
        }

        private async UniTask ScanDevicesAndShowPanel(Action onSuccess, CancellationToken token)
        {
            var devices = await _bluetoothCommandsController.StartScanningAsync(token);

            devicesVisualizer.SetDevices(devices, device =>
            {
                ConnectToMannequin(device).Forget();
                onSuccess?.Invoke();

                devicesVisualizer.SetDevicesPanelVisibleStatus(false);
            });

            devicesVisualizer.SetDevicesPanelVisibleStatus(true);
            devicesVisualizer.SetScanPanelVisibleStatus(false);
        }

        public async UniTask ActivateBraydenMonitor(int needCyclesCount, Action onFinishCallback, Action<List<BraydenCycle>> onSaveResultsCallback)
        {
            _loading.Show(TextData.Get(494));

            await SetMannequinState(MannequinState.Run);

            braydenMonitorController.ActivateMonitor(needCyclesCount, onFinishCallback, onSaveResultsCallback);
            _loading.Hide();
            _timer.Pause();
        }

        public void DeactivateBraydenMonitor()
        {
            try
            {
                SetMannequinState(MannequinState.Disconnected).Forget();
            }
            catch (Exception ex)
            {
                Debug.Log($"Exception when turn off mannequin: {ex}");
            }
            finally
            {
                braydenMonitorController.DeactivateMonitorWithoutCallbacks();
            }
        }
    
        private async UniTask ConnectToMannequin(ScanDevice selectedDevice)
        {
            if (MannequinCurrentState is MannequinState.Run)
                return;

            await _bluetoothCommandsController.ConnectToDevice(selectedDevice, _cts.Token);
            await SetMannequinState(MannequinState.Idle);
        }

        public async UniTask SetMannequinState(MannequinState state)
        {
            switch (state)
            {
                case MannequinState.Disconnected:
                {
                    DisconnectFromMannequin();
                    break;
                }
                case MannequinState.Idle:
                {
                    _bluetoothCommandsController.UnsubscribeFromCharacteristic();
                    _bluetoothCommandsController.WriteCharacteristic(BleConstants.StartBytes);
                
                    break;
                }

                case MannequinState.Run:
                {
                    _bluetoothCommandsController.WriteCharacteristic(BleConstants.StartBytes);
                    _bluetoothCommandsController.WriteCharacteristic(BleConstants.RunMannequinBytes);

                    await UniTask.Delay(TimeSpan.FromSeconds(BleConstants.DELAY_SECONDS_BEFORE_MANNEQUIN_RUN));

                    _bluetoothCommandsController.SubscribeToCharacteristic();
                
                    break;
                }
                case MannequinState.Sleep:
                {
                    _timer.Unpause();
                    _bluetoothCommandsController.UnsubscribeFromCharacteristic();
                    _bluetoothCommandsController.WriteCharacteristic(BleConstants.StopMannequinBytes);
                
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, "Exception: UNKNOWN MANNEQUIN STATE VALUE");
            }

            MannequinCurrentState = state;
        }
    
        private void DisconnectFromMannequin()
        {
            try
            {
                _bluetoothCommandsController.DisconnectFromDevice();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BleController] Exception when disconnect: {ex.Message}");
            }
            finally
            {
                MannequinCurrentState = MannequinState.Disconnected;
            }
        }
        
        public async UniTask CaptureCycleScreens(List<BraydenCycle> cycleResults)
        {
            await braydenMonitorController.CaptureCycleScreens(cycleResults);
        }

        private void OnApplicationQuit()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        BleApi.Quit();
#endif
        }

        public void ResetData()
        {
            braydenMonitorController.CurrentCycleIndex = 0;
        }
    }
}