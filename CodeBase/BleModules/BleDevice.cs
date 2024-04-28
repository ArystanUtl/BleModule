using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CodeBase.BleModules
{
    public class BleDevice : MonoBehaviour
    {
        [SerializeField] private TMP_Text deviceNameText;
        [SerializeField] private Toggle toggle;

        private static BleDevice _prevSelectedDevice;
        
        public bool IsSelected => toggle.isOn;
        public ScanDevice Device { get; private set; }
        
        private void Start()
        {
            toggle.onValueChanged.AddListener(OnToggleStateChanged);
        }

        private void OnToggleStateChanged(bool isOn)
        {
            if (isOn)
            {
                if (_prevSelectedDevice != null && _prevSelectedDevice != this)
                    _prevSelectedDevice.DeactivateToggle();

                _prevSelectedDevice = this;
            }   
        }

        public void SetupDeviceAndCallback(ScanDevice device)
        {
            Device = device;
            deviceNameText.text = device.Name;
        }

        private void DeactivateToggle()
        {
            toggle.isOn = false;
        }
    }
}