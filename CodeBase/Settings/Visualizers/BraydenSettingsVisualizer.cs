using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Modules.Books.BraydenSettingsBook;

namespace CodeBase.Settings.Visualizers
{
    public class BraydenSettingsVisualizer : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_InputField valueInputField;
        [SerializeField] private Toggle autoModeToggle;
        [SerializeField] private Toggle manualModeToggle;

        private BraydenGlobalSettingsManager _settings;
        private BraydenSettingParameter _currentParameter;

        private void Start()
        {
            autoModeToggle.onValueChanged.AddListener(OnAutoModeToggleChanged);
        }

        public void Init(BraydenGlobalSettingsManager manager)
        {
            _settings = manager;
        }

        public void SetupParameter(BraydenSettingParameter settingsParameter)
        {
            _currentParameter = settingsParameter;
            titleText.text = settingsParameter.Title;
        }
        
        private void OnAutoModeToggleChanged(bool isOn)
        {
            valueInputField.readOnly = isOn;
        }

        public void ShowCurrentSettings()
        {
            var isAutoMode = _settings.ConfigManager.IsAutoMode(_currentParameter.ID);
            
            var currentValue = isAutoMode
                ? _currentParameter.CoefValue
                : _settings.ConfigManager.GetParameterValue(_currentParameter.ID);

            valueInputField.text = currentValue.ToString(CultureInfo.InvariantCulture);
            valueInputField.readOnly = isAutoMode;
            
            autoModeToggle.isOn = isAutoMode;
            manualModeToggle.isOn = !isAutoMode;
        }

        public void SaveSettings()
        {
            var isSelectedAutoMode = autoModeToggle.isOn;
            var isUserValueParsed = float.TryParse(valueInputField.text, out var userValue);
            var isUserValueCorrect = isUserValueParsed && userValue >= _currentParameter.MinValue && userValue <= _currentParameter.MaxValue;

            if (isSelectedAutoMode || !isUserValueParsed || !isUserValueCorrect)
            {
                _settings.ConfigManager.SaveParameter(true, _currentParameter.ID);
                return;
            }

            _settings.ConfigManager.SaveParameter(false, _currentParameter.ID, userValue);
        }
    }
}