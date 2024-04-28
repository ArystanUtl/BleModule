using System;
using System.Collections.Generic;
using System.Linq;
using CodeBase.Settings;
using static CodeBase.Settings.BraydenConfigKeys;

namespace CodeBase
{
    public class CompressionManager
    {
        private const int FREQUENCY_SECONDS = 60;
        
        private readonly BraydenConfigManager _configManager;
        private readonly BraydenUnitConvertManager _convertManager;

        public CompressionManager(BraydenConfigManager configManager, BraydenUnitConvertManager convertManager)
        {
            _configManager = configManager;
            _convertManager = convertManager;
        }

        public float GetCompressionDepth(IEnumerable<GradationModel> values)
        {
            var gradationModels = values as GradationModel[] ?? values.ToArray();
            var byteValues = gradationModels.Select(x => x.ByteValue).ToList();

            var (minIndex, maxIndex) = BraydenLogsManager.GetMinAndMaxIndices(gradationModels);
            var depthShift = byteValues[maxIndex] - byteValues[minIndex];
            var depth = _convertManager.GetCmFromDepthOfPressure((byte)depthShift);

            return depth;
        }

        public Frequency GetCompressionFrequency(CompressionModel prevModel, CompressionModel nextModel)
        {
            var maxFrequency = _configManager.GetParameterValue(MAX_FREQUENCY_NORMA_KEY);
            var minFrequency = _configManager.GetParameterValue(MIN_FREQUENCY_NORMA_KEY);
            
            if (prevModel == null)
                return new Frequency((maxFrequency + minFrequency) / 2f, FrequencyType.Norm);

            var timeDifference = nextModel.StartedTime - prevModel.StartedTime;
            var differenceInMilliseconds = (float)timeDifference.TotalMilliseconds;

            var frequencyValue = FREQUENCY_SECONDS * 1000f / differenceInMilliseconds;
            var frequencyType = GetFrequencyType(frequencyValue);

            return new Frequency(frequencyValue, frequencyType);
        }

        private FrequencyType GetFrequencyType(float frequencyValue)
        {
            var maxFrequency = _configManager.GetParameterValue(MAX_FREQUENCY_NORMA_KEY);
            var minFrequency = _configManager.GetParameterValue(MIN_FREQUENCY_NORMA_KEY);

            if (frequencyValue < minFrequency)
                return FrequencyType.Slow;
            
            return frequencyValue > maxFrequency
                ? FrequencyType.Fast
                : FrequencyType.Norm;
        }

        public DepthStatus GetCompressionStatus(float depth)
        {
            if (depth == 0f)
                return DepthStatus.None;

            var minDepthNorma = _configManager.GetParameterValue(MIN_DEPTH_NORMA_KEY);
            var maxDepthNorma = _configManager.GetParameterValue(MAX_DEPTH_NORMA_KEY);

            if (depth < minDepthNorma)
                return DepthStatus.Weak;
            
            return depth > maxDepthNorma
                ? DepthStatus.Strong
                : DepthStatus.Norm;
        }

        public string GetCompressionDescription(int number = 0)
        {
            var requiredCompressionsCount = (int) _configManager.GetParameterValue(BraydenConfigKeys.CYCLE_REQUIRED_COMPRESSIONS_COUNT_KEY);
            
            var numberDescription = number > requiredCompressionsCount
                ? $"<color=#F9544A>{number}</color>"
                : number.ToString();

            return $"{numberDescription}/{requiredCompressionsCount.ToString()}";
        }

        public HandsPosition GetCompressionPosition(List<GradationModel> gradations)
        {
            var maxBytes = Array.Empty<byte>();

            var maxDepth = 0f;
            foreach (var model in gradations)
            {
                var bytes = model.AllBytes;
                var depth = GetCompressionDepth(gradations);

                if (!(depth >= maxDepth))
                    continue;

                maxDepth = depth;
                maxBytes = bytes;
            }

            var positionValue = BraydenLogsManager.GetByteByLogType(LogType.CompressionHandsPosition, maxBytes);

            var pos = positionValue switch
            {
                0 => HandsPosition.None,
                1 => HandsPosition.Center,
                2 => HandsPosition.Down,
                4 => HandsPosition.Left,
                8 => HandsPosition.Right,
                16 => HandsPosition.Top,
                _ => HandsPosition.None
            };

            return pos;
        }

        public static CompressionModel CreateEmptyCompression(byte number)
        {
            var compression = new CompressionModel(number, 0f, DepthStatus.None, HandsPosition.None, null);
            compression.UpdateFrequency(new Frequency(0, FrequencyType.None));
            return compression;
        }
    }
}