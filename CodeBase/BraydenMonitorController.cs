using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeBase.BleModules;
using CodeBase.BleModules.Enums;
using CodeBase.Decompression;
using CodeBase.Models.BraydenModels.Breath.Exhalation;
using CodeBase.Settings;
using Cysharp.Threading.Tasks;
using Modules.BluetoothBase.CodeBase.BleModules;
using Modules.Loading;
using Modules.WDCore;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CodeBase
{
    public class BraydenMonitorController : MonoBehaviour
    {
        [Header("Main controls")]
        [Space(10)] 
        [SerializeField] private BraydenResultsViewController braydenResultsViewController;
        [SerializeField] private BraydenSettingsViewController settingsViewController;
        [SerializeField] private GameObject rootObject;
        [SerializeField] private Button finishButton;
        [SerializeField] private Button settingsButton;

#if !UNITY_XR
        [Inject] private AppControls _appControls;
#endif
        [Inject] private BleController _bleController;
        [Inject] private IBluetoothCommandsController _bluetoothCommandsController;
        [Inject] private ILoading _loading;
        
        [NonSerialized] public int CurrentCycleIndex;

        private BraydenCycleController _braydenCycleController;
        
        private BreatheState _breatheState = BreatheState.Started;
        private CompressionState _compressionState = CompressionState.Started;

        private CompressionModel _prevCompressionModel;
        private InhalationModel _prevInhalationModel;
        private DecompressionModel _currentDecompressionModel;
        private ExhalationModel _currentExhalationModel;

        private BraydenGlobalSettingsManager _settings;

        private Action<List<BraydenCycle>> _onCycleResultsCallback;
        private Action _onFinishButtonClicked;
        
        private bool _isActivateLogs;
        private int _needCyclesCount;
        
        private DateTime _startedTime;
        private CancellationTokenSource _timeCts;

        private void Awake()
        {
            AddListeners();
          
        }

        public void Init(BraydenGlobalSettingsManager settingsManager)
        {
            _settings = settingsManager;
            
            braydenResultsViewController.Init(settingsManager);
            settingsViewController.Init(settingsManager);
            
            InitCycleController();
        }
        
        private void InitCycleController()
        {
            _braydenCycleController = new BraydenCycleController(_settings);
        }

        private void AddListeners()
        {
            _bluetoothCommandsController.OnDataReceived += ShowAndCalculateLogs;
            _bluetoothCommandsController.OnDeviceStarted += ResetCurrentModels;
            
            finishButton.onClick.AddListener(() => DeactivateMonitor().Forget());
            settingsButton.onClick.AddListener(ActivateSettingsWindow);
        }

        private void ActivateSettingsWindow()
        {
            if (GameManager.Instance.isDevMode)
                settingsViewController.SetPanelVisibleStatus(true);
        }

        public void DeactivateMonitorWithoutCallbacks()
        {
            rootObject.gameObject.SetActive(false);
        }

        private async UniTask DeactivateMonitor()
        {
            settingsViewController.SetPanelVisibleStatus(false);
            
            _loading.Show("Deactivating mannequin...");
            
            _isActivateLogs = false;
            ResetTimeCts();
            rootObject.SetActive(false);

            await _bleController.SetMannequinState(MannequinState.Sleep);

            braydenResultsViewController.ResetUI();
            braydenResultsViewController.ResetTimeTexts();
            
            _onCycleResultsCallback?.Invoke(_braydenCycleController.Cycles);
            _onFinishButtonClicked?.Invoke();
#if !UNITY_XR
            _appControls.SetControlBoxStatus(true);
#endif
            _loading.Hide();
        }

        public async UniTask CaptureCycleScreens(List<BraydenCycle> cycleResults)
        {
            await braydenResultsViewController.CaptureCycleScreens(cycleResults);
        }

        private void ResetCurrentModels()
        {
            _prevCompressionModel = null;
            _prevInhalationModel = null;
            _currentDecompressionModel = null;
            _currentExhalationModel = null;
            _compressionState = CompressionState.Started;
            _breatheState = BreatheState.Started;
        }

        public void ActivateMonitor(int needCyclesCount, Action onFinishCallback, Action<List<BraydenCycle>> onSaveResultsCallback)
        {
            if (!GameManager.Instance.isDevMode)
                settingsButton.gameObject.SetActive(false);

            InitCycleController();
            ResetCurrentModels();

            _needCyclesCount = needCyclesCount;
            ResetMonitor();
            braydenResultsViewController.ResetTimeTexts();

            rootObject.SetActive(true);

            _onCycleResultsCallback = onSaveResultsCallback;
            _onFinishButtonClicked = onFinishCallback;

            _isActivateLogs = true;

#if !UNITY_XR
            _appControls.SetControlBoxStatus(false);
#endif
        }

        private void ResetMonitor()
        {
            braydenResultsViewController.ResetUI();
            braydenResultsViewController.UpdateCycleText(_braydenCycleController.CurrentCycle);
            ResetTimeCts();
            StartTimer();
        }

        private void ResetTimeCts()
        {
            _timeCts?.Cancel();
            _timeCts = new CancellationTokenSource();
        }

        private void StartTimer()
        {
            _startedTime = DateTime.Now;
            ResetTimeCts();
            StartTimerAsync().Forget();
        }

        private async UniTask StartTimerAsync()
        {
            var timeSpan = DateTime.Now - _startedTime;

            braydenResultsViewController.ShowTime(timeSpan);

            await UniTask.Yield(_timeCts.Token);
            
            if (_timeCts is { IsCancellationRequested: true })
                return;

            StartTimerAsync().Forget();
        }

        private void ShowAndCalculateLogs(byte[] receivedBytes)
        {
            if (!_isActivateLogs)
                return;

            CalculateCompressionResult(receivedBytes);
            CalculateBreathResult(receivedBytes);
        }

        private void CalculateCompressionResult(byte[] bytes)
        {
            var compressionBytes = BraydenLogsManager.GetBytesByLogType(LogType.CompressionDecompressionDepth, bytes);
            var compressionValue = compressionBytes.Aggregate(0f, (current, currentByte) => current + currentByte);

            compressionValue /= compressionBytes.Count;

            if (compressionBytes.Any(x => x == 0))
                compressionValue = 0;

            var currentNumber = (byte)(BraydenLogsManager.GetByteByLogType(LogType.CompressionNumber, bytes) + 1);
            var currentByte = (byte)compressionValue;

            var currentGradation = new GradationModel(currentByte, bytes);

            if (currentByte == 0 && _compressionState is CompressionState.Started)
            {
                braydenResultsViewController.VisualizeCompressionDepth(null);
                return;
            }

            if (_prevCompressionModel == null)
            {
                _compressionState = CompressionState.Started;

                _prevCompressionModel = new CompressionModel(currentNumber);
                _prevCompressionModel.AddGradation(currentGradation);

                _currentDecompressionModel = new DecompressionModel(currentNumber);
                braydenResultsViewController.VisualizeCompressionDepth(_prevCompressionModel);

                return;
            }

            switch (_compressionState)
            {
                case CompressionState.Started when _prevCompressionModel.Number == currentNumber:
                {
                    _prevCompressionModel.AddGradation(currentGradation);
                    braydenResultsViewController.VisualizeCompressionDepth(_prevCompressionModel);

                    return;
                }

                case CompressionState.Started:
                {
                    _currentDecompressionModel ??= new DecompressionModel(currentNumber);
                    _currentDecompressionModel.AddGradation(currentGradation);
                    _compressionState = CompressionState.Finished;

                    return;
                }

                case CompressionState.Finished:
                {
                    RegisterFinishedCompression(_prevCompressionModel).Forget();

                    _prevCompressionModel = new CompressionModel(currentNumber);
                    _prevCompressionModel.AddGradation(currentGradation);
                    _compressionState = CompressionState.Decompression;

                    return;
                }

                case CompressionState.Decompression:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_compressionState != CompressionState.Decompression)
                return;

            _currentDecompressionModel.RegisterStartedTime();


            var finishByte = _settings.ConfigManager.GetParameterValue(BraydenConfigKeys.DECOMPRESSION_FINISH_BYTE_KEY);
            
            if (_currentDecompressionModel.Gradations.Any())
            {
                var lastByte = _currentDecompressionModel.Gradations.Last().ByteValue;
                _currentDecompressionModel.AddGradation(currentGradation);

                //auto decompressed or manual cancelled compression and started new
                if (currentByte <= finishByte || lastByte < currentByte)
                {
                    var depth = _settings.DecompressionManager.GetDecompressionDepth(_currentDecompressionModel);
                    var status = _settings.DecompressionManager.GetStatus(depth);

                    var decompression = new DecompressionModel(_currentDecompressionModel.Number, depth, _currentDecompressionModel.StartedTime, status, _currentDecompressionModel.Gradations);

                    _braydenCycleController.RegisterDecompression(decompression);
                    braydenResultsViewController.VisualizeDecompressionDepth(decompression, true);

                    _currentDecompressionModel = new DecompressionModel(currentNumber);
                    _compressionState = CompressionState.Started;

                    return;
                }

                braydenResultsViewController.VisualizeDecompressionDepth(_currentDecompressionModel);
            }
            else
            {
                _currentDecompressionModel.AddGradation(currentGradation);
                braydenResultsViewController.VisualizeDecompressionDepth(_currentDecompressionModel);
            }
        }

        private async UniTask RegisterFinishedCompression(CompressionModel model)
        {
            var manager = _settings.CompressionManager;
            
            var gradations = model.Gradations;
            var depth = manager.GetCompressionDepth(gradations);
            var pos = manager.GetCompressionPosition(gradations);
            var status = manager.GetCompressionStatus(depth);
            
            model = new CompressionModel(model.Number, depth, status, pos, gradations);

            var cyclesCount = _braydenCycleController.RegisterCompression(model);

            if (cyclesCount == 0)
            {
                OnCycleContinued(model);
            }
            else
            {
                _isActivateLogs = false;

                await OnCyclesFinished(cyclesCount);
                _isActivateLogs = true;

                OnCycleContinued(model);
            }
        }

        private void OnCycleContinued(CompressionModel model)
        {
            braydenResultsViewController.VisualizeFinishedCompression(model);

            var compressionDescription = _settings.CompressionManager.GetCompressionDescription(_braydenCycleController.CurrentCycle.CompressionsCount);
            braydenResultsViewController.UpdateCompressionsText(compressionDescription);
        }

        private async UniTask OnCyclesFinished(int finishedCyclesCount)
        {
            await braydenResultsViewController.CaptureCycleScreenshot(CurrentCycleIndex);
            await ResetUIMonitor(finishedCyclesCount);
        }

        private async UniTask ResetUIMonitor(int finishedCyclesCount)
        {
            CurrentCycleIndex++;
            braydenResultsViewController.ResetUI();

            braydenResultsViewController.UpdateCycleText(_braydenCycleController.CurrentCycle);

            if (finishedCyclesCount >= _needCyclesCount)
                await DeactivateMonitor();
        }

        private void CalculateBreathResult(byte[] bytes)
        {
            var breatheBytes = BraydenLogsManager.GetBytesByLogType(LogType.BreatheCapacity, bytes);
            var breathValue = breatheBytes.Aggregate(0f, (current, currentByte) => current + currentByte);

            breathValue /= breatheBytes.Count;

            var currentNumber = (byte)(BraydenLogsManager.GetByteByLogType(LogType.BreatheNumber, bytes) + 1);
            var currentByte = (byte)breathValue;

            var currentGradation = new GradationModel(currentByte, bytes);

            if (_prevInhalationModel == null)
            {
                _breatheState = BreatheState.Started;

                _prevInhalationModel = new InhalationModel(currentNumber);
                _prevInhalationModel.AddGradation(currentGradation);

                braydenResultsViewController.VisualizeInhalation(_prevInhalationModel);

                _currentExhalationModel = new ExhalationModel(currentNumber);
                return;
            }

            switch (_breatheState)
            {
                case BreatheState.Started when _prevInhalationModel.Number == currentNumber:
                {
                    _prevInhalationModel.AddGradation(currentGradation);
                    braydenResultsViewController.VisualizeInhalation(_prevInhalationModel);

                    return;
                }

                case BreatheState.Started:
                {
                    _breatheState = BreatheState.Finished;

                    return;
                }

                case BreatheState.Finished:
                {
                    RegisterFinishedInhalation(_prevInhalationModel);
                    _breatheState = BreatheState.Exhalation;

                    return;
                }

                case BreatheState.Exhalation:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_breatheState != BreatheState.Exhalation)
                return;

            var minByte =(byte) _settings.ConfigManager.GetParameterValue(BraydenConfigKeys.EXHALATION_FINISH_BYTE_KEY);

            if (_currentExhalationModel.Gradations.Any())
            {
                var lastByte = _currentExhalationModel.Gradations.Last().ByteValue;

                //auto decompressed or manual cancelled compression and started new
                if (currentByte <= minByte || lastByte < currentByte)
                {
                    _currentExhalationModel.AddGradation(currentGradation);
                    var exhalationVolume = _settings.BreatheManager.GetExhalationVolume(_currentExhalationModel.Gradations);

                    _currentExhalationModel.UpdateVolume(exhalationVolume);
                    braydenResultsViewController.VisualizeExhalation(_currentExhalationModel);

                    _braydenCycleController.RegisterExhalation(_currentExhalationModel);

                    _currentExhalationModel = new ExhalationModel(currentNumber);
                    _breatheState = BreatheState.Started;

                    _prevInhalationModel = new InhalationModel(currentNumber);
                    _prevInhalationModel.AddGradation(currentGradation);

                    return;
                }

                _currentExhalationModel.AddGradation(currentGradation);
                braydenResultsViewController.VisualizeExhalation(_currentExhalationModel);
            }
            else
            {
                _currentExhalationModel.AddGradation(currentGradation);
                braydenResultsViewController.VisualizeExhalation(_currentExhalationModel);
            }
        }

        private void RegisterFinishedInhalation(InhalationModel model)
        {
            var gradations = model.Gradations;
            var lungCapacity = _settings.BreatheManager.GetLungCapacity(model.Gradations);
            var duration = _settings.BreatheManager.GetDuration(model);
            var startedTime = model.StartedTime;

            model = new InhalationModel(model.Number, lungCapacity, gradations);
            model.UpdateDuration(duration);
            model.RegisterStartedTime(startedTime);

            _braydenCycleController.RegisterInhalation(model);

            braydenResultsViewController.VisualizeInhalation(model, true);

            var breatheDescription = _settings.BreatheManager.GetBreatheDescription(_braydenCycleController.CurrentCycle.BreatheCount);
            braydenResultsViewController.UpdateBreatheText(breatheDescription);
        }
    }
}