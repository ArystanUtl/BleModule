using System;
using System.Linq;
using CodeBase.Decompression;
using CodeBase.Settings;

namespace CodeBase
{
    public  class DecompressionManager
    {
        private readonly BraydenConfigManager _configManager;
        private readonly BraydenUnitConvertManager _convertManager;
        
        public DecompressionManager(BraydenConfigManager configManager, BraydenUnitConvertManager convertManager)
        {
            _configManager = configManager;
            _convertManager = convertManager;
        }

        public float GetDecompressionDepth(BleResultModel model)
        {
            var minByte = model.Gradations.Select(x => x.ByteValue).Min();
            var depth = _convertManager.GetCmFromDepthOfPressure(minByte);

            return depth;
        }

        public DecompressionCorrectStatus GetStatus(float depth)
        {
            var maxDepth = _configManager.GetParameterValue(BraydenConfigKeys.DECOMPRESSION_MAX_DEPTH_KEY);

            return depth <= maxDepth || depth == 0f
                ? DecompressionCorrectStatus.Correct
                : DecompressionCorrectStatus.Incorrect;
        }

        public static DecompressionModel CreateEmptyDecompression(byte number)
        {
            var decompression = new DecompressionModel(number, 0f, DateTime.Now, DecompressionCorrectStatus.None, null);
            return decompression;
        }
    }
}