using Modules.Books;
using UnityEngine;
using static CodeBase.Settings.BraydenConfigKeys;

namespace CodeBase.Settings
{
    public class BraydenConfigManager
    {
        private readonly BookDatabase _bookDatabase;
        
        public BraydenConfigManager(BookDatabase bookDatabase)
        {
            _bookDatabase = bookDatabase;
        }

        public void SaveParameter(bool isAutoMode, string key, float value = 0f)
        {
            var modeResult = isAutoMode
                ? 1
                : 0;  
            
            var saveKey = GetSaveIdKeyForParameter(key);
            var saveMode = GetSaveModeKeyForParameter(key);
            
            PlayerPrefs.SetInt(saveMode, modeResult);

            if (!isAutoMode)
                PlayerPrefs.SetFloat(saveKey, value);
            
            PlayerPrefs.Save();
        }
        
        public float GetParameterValue(string parameterID)
        {
            var parameterModel = _bookDatabase.BraydenBook.BraydenSettingParametersByID[parameterID];
            var defaultValue = parameterModel.CoefValue;
            
            if (parameterModel.IsBlocked)
            {
                SaveParameter(true, parameterID);
                return defaultValue;
            }
            
            if (IsAutoMode(parameterID))
                return defaultValue;

            var savedResult = PlayerPrefs.GetFloat(GetSaveIdKeyForParameter(parameterID), defaultValue);
            return savedResult;
        }

        public bool IsAutoMode(string parameterID)
        {
            var savedMode = PlayerPrefs.GetInt(GetSaveModeKeyForParameter(parameterID), 1);
            return savedMode == 1;
        }
    }
}