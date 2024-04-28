using CodeBase.Settings;
using static CodeBase.Settings.BraydenConfigKeys;

namespace CodeBase
{
    public class BraydenUnitConvertManager
    { 
        private readonly BraydenConfigManager _configManager;
        
        public BraydenUnitConvertManager(BraydenConfigManager configManager)
        {
            _configManager = configManager;
        }
        
        public float GetCmFromDepthOfPressure(byte pressureValue)
        {
            var coefficient = _configManager.GetParameterValue(DEPTH_COEFFICIENT_KEY);
            return pressureValue / coefficient;
        }

        public float GetMlFromAmounthOfBreathe(byte breathValue)
        {
            var coefficient = _configManager.GetParameterValue(CAPACITY_COEFFICIENT_KEY);
            return breathValue * coefficient;
        }
    }
}