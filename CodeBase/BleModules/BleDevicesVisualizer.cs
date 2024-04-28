using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.BluetoothBase.CodeBase.BleModules;
using Modules.Common;
using Modules.Signals;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CodeBase.BleModules
{
    public class BleDevicesVisualizer : MonoBehaviour
    {
        [Header("Devices panel controls")]
        [Space(5)]
        [SerializeField] private GameObject devicesPanelRoot;
        [SerializeField] private Transform container;
        [SerializeField] private BleDevice devicePrefab;
        [SerializeField] private Button selectDeviceButton;
        [SerializeField] private Button closeDevicesPanelButton;
        [SerializeField] private Button repeatScanButton;
        [Space(20)]

        [Header("Scan panel controls")]
        [Space(5)]
        [SerializeField] private GameObject scanPanelRoot;
        [SerializeField] private Button helpButton;
        [SerializeField] private Button closeScanPanelButton;
        [SerializeField] private Image loadingImage;
        [SerializeField] private List<Sprite> animSprites;

        [Inject] private IBluetoothCommandsController _bluetoothCommandsController;
        [Inject] private BleController _bleController;
        public Action OnHelpButtonClicked { get; set; }
        
        private List<BleDevice> _currentDevices = new();
        private Action<ScanDevice> _onDeviceSelected;
        
        private void Start()
        {
            closeDevicesPanelButton.onClick.AddListener(ClosePanel);
            closeScanPanelButton.onClick.AddListener(ClosePanel);
            
            repeatScanButton.onClick.AddListener(() => StartScanning().Forget());
            helpButton.onClick.AddListener(() => OnHelpButtonClicked?.Invoke());
            
            selectDeviceButton.onClick.AddListener(SelectDevice);
        }

        private void SelectDevice()
        {
            var selectedDevice = _currentDevices.FirstOrDefault(x => x.IsSelected);
            if (selectedDevice == null)
                return;
            
            _onDeviceSelected?.Invoke(selectedDevice.Device);
        }

        private CancellationTokenSource _animCts;
        private void ResetAnimCts()
        {
            _animCts?.Cancel();
            _animCts = new CancellationTokenSource();
        }

        private CancellationTokenSource _scanCts;

        private void ResetScanCts()
        {
            _scanCts?.Cancel();
            _scanCts = new();
        }

        private async UniTask StartScanning()
        {
            ResetAnimCts();
            ResetScanCts();
            SetDevicesPanelVisibleStatus(false);
            repeatScanButton.interactable = false;
            SetScanPanelVisibleStatus(true);

            StartLoadingAnim();
            
            ResetCurrentDevices();
            
            await UniTask.Delay(TimeSpan.FromSeconds(2f), cancellationToken: _scanCts.Token);
            
            var devices = await _bluetoothCommandsController.StartScanningAsync(_scanCts.Token);
            SetDevices(devices, _onDeviceSelected);
            
            repeatScanButton.interactable = true;
            SetScanPanelVisibleStatus(false);
            SetDevicesPanelVisibleStatus(true);
        }

        private int _spriteIndex;
        public void StartLoadingAnim()
        {
            var rnd = new System.Random();

            _spriteIndex = rnd.Next(0, animSprites.Count);
            ResetAnimCts();
            StartLoadingAnimAsync().Forget();
        }

        private async UniTaskVoid StartLoadingAnimAsync()
        {
            while (true)
            {
                if (_spriteIndex >= animSprites.Count)
                    _spriteIndex = 0;

                loadingImage.sprite = animSprites[_spriteIndex];
                await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: _animCts.Token);
                _spriteIndex++;
            }
        }

        private void ClosePanel()
        {
            ResetScanCts();
            ResetAnimCts();
            
            SetScanPanelVisibleStatus(false);
            SetDevicesPanelVisibleStatus(false);
            
            ResetCurrentDevices();
            _bleController.ResetCTS();
            Supyrb.Signals.Get<MainMenuShowMenuSignal>().Dispatch(MainMenuModuleName.ScenarioDetails);
        }
        
        public void SetDevicesPanelVisibleStatus(bool status)
        {
            devicesPanelRoot.SetActive(status);
        }

        public void SetScanPanelVisibleStatus(bool status)
        {
            scanPanelRoot.SetActive(status);
            ResetAnimCts();
            
            if (status)
                StartLoadingAnim();
        }
        private static List<ScanDevice> FilterSameDevices(IReadOnlyCollection<ScanDevice> devices)
        {
            List<ScanDevice> results = new();
            if (devices.IsNullOrEmpty())
                return results;

            foreach (var device in devices.Where(x => x.Name.IsCorrect()))
                if (results.All(x => x.MacAddress != device.MacAddress))
                    results.Add(device);

            return results;
        }
        public void SetDevices(List<ScanDevice> devices, Action<ScanDevice> onDeviceSelected)
        {
            _onDeviceSelected = onDeviceSelected;
            ResetCurrentDevices();

            var filteredDevices = FilterSameDevices(devices);
            
            foreach (var device in filteredDevices)
            {
                var deviceObject = Instantiate(devicePrefab, container, false);
                deviceObject.SetupDeviceAndCallback(device);
                _currentDevices.Add(deviceObject);
            }
        }
        
        private void ResetCurrentDevices()
        {
            foreach (var device in _currentDevices.Where(device => device != null && device.gameObject != null))
                Destroy(device.gameObject);

            _currentDevices = new List<BleDevice>();
        }
    }
}