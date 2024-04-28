using System;
using System.Collections.Generic;
using System.Linq;
using CodeBase.Decompression;
using CodeBase.Models.BraydenModels.Breath;
using CodeBase.Models.BraydenModels.Breath.Exhalation;
using CodeBase.Settings;

namespace CodeBase
{
    public class BreatheManager
    {
        private readonly BraydenConfigManager _configManager;
        private readonly BraydenUnitConvertManager _convertManager;

        public BreatheManager(BraydenConfigManager configManager, BraydenUnitConvertManager convertManager)
        {
            _configManager = configManager;
            _convertManager = convertManager;
        }

        private LungCapacityStatus GetLungCapacityStatus(float capacity)
        {
            if (capacity == 0f)
                return LungCapacityStatus.None;

            var minNorma = _configManager.GetParameterValue(BraydenConfigKeys.MIN_CAPACITY_NORMA_KEY);
            var maxNorma = _configManager.GetParameterValue(BraydenConfigKeys.MAX_CAPACITY_NORMA_KEY);

            if (capacity < minNorma)
                return LungCapacityStatus.NotEnough;
            
            return capacity > maxNorma
                ? LungCapacityStatus.TooMuch
                : LungCapacityStatus.Normal;
        }
        
        public  LungCapacity GetLungCapacity(IReadOnlyCollection<GradationModel> values)
        {
            if (values == null || values.Count == 0)
                return new LungCapacity(0f, LungCapacityStatus.None);
            
            var bytes = values.Select(x => x.ByteValue).ToList();
            
            var (minIndex, maxIndex) = BraydenLogsManager.GetMinAndMaxIndices(values);
            var capacityShift = bytes[maxIndex] - bytes[minIndex];
            var capacity = _convertManager.GetMlFromAmounthOfBreathe((byte)capacityShift);
            var status = GetLungCapacityStatus(capacity);

            return new LungCapacity(capacity, status);
        }

        public  ExhalationVolume GetExhalationVolume(List<GradationModel> gradations)
        {
            var minByte = gradations.Select(x => x.ByteValue).Min();
            var volume = _convertManager.GetMlFromAmounthOfBreathe(minByte);

            var maxNorma = _configManager.GetParameterValue(BraydenConfigKeys.EXHALATION_CAPACITY_MAX_NORMA_KEY);
          
            var status = volume <= maxNorma
                ? ExhalationStatus.Correct
                : ExhalationStatus.Incorrect;
          
            return new ExhalationVolume(volume, status);
        }

        public  DelayDecompressionAndInhalation GetDelayDcmprAndInh(DecompressionModel lastDecompression, InhalationModel model, bool isFirst)
        {
            if (lastDecompression == null || !isFirst)
                return new DelayDecompressionAndInhalation(0f, DelayDcmprAndInhStatus.Correct);
           
            var timeDifference = model.StartedTime - lastDecompression.StartedTime;
            var differenceInSeconds =(float) timeDifference.TotalSeconds;

            var maxNorma = _configManager.GetParameterValue(BraydenConfigKeys.DELAY_AFTER_DECOMPRESSION_MAX_NORMA_KEY);

            return differenceInSeconds <= maxNorma
                ? new DelayDecompressionAndInhalation(differenceInSeconds, DelayDcmprAndInhStatus.Correct)
                : new DelayDecompressionAndInhalation(differenceInSeconds, DelayDcmprAndInhStatus.Incorrect);
        }

        public  DelayFirstAndSecondInhalation GetDelayFirstSecondInh(InhalationModel firstModel, InhalationModel secondModel)
        {
            if (secondModel == null || firstModel == null)
                return new DelayFirstAndSecondInhalation(0f, DelayFirstAndSecondInhStatus.Normal);

            var differenceTime = secondModel.StartedTime - firstModel.StartedTime;

            var differenceSeconds =(float) differenceTime.TotalSeconds;

            var minNorma = _configManager.GetParameterValue(BraydenConfigKeys.DELAY_FIRST_SECOND_INHALATIONS_MIN_NORMA_KEY);
            var maxNorma = _configManager.GetParameterValue(BraydenConfigKeys.DELAY_FIRST_SECOND_INHALATIONS_MAX_NORMA_KEY);

            if (differenceSeconds >= minNorma && differenceSeconds <= maxNorma)
                return new DelayFirstAndSecondInhalation(differenceSeconds, DelayFirstAndSecondInhStatus.Normal);

            if (differenceSeconds < minNorma)
                return new DelayFirstAndSecondInhalation(differenceSeconds, DelayFirstAndSecondInhStatus.Early);

            return differenceSeconds > maxNorma
                ? new DelayFirstAndSecondInhalation(differenceSeconds, DelayFirstAndSecondInhStatus.Late)
                : new DelayFirstAndSecondInhalation(0f, DelayFirstAndSecondInhStatus.None);
        }

        public  InhalationDuration GetDuration(InhalationModel model)
        {
            var durationTime = DateTime.Now - model.StartedTime;
            var durationSeconds = (float)durationTime.TotalSeconds;

            var minNorma = _configManager.GetParameterValue(BraydenConfigKeys.INHALATION_DURATION_MIN_NORMA_KEY);
            var maxNorma = _configManager.GetParameterValue(BraydenConfigKeys.INHALATION_DURATION_MAX_NORMA_KEY);
            
            InhDurationStatus status;

            if (durationSeconds >= minNorma && durationSeconds <= maxNorma)
                status = InhDurationStatus.Normal;
            else if (durationSeconds < minNorma)
                status = InhDurationStatus.Short;
            else if (durationSeconds > maxNorma)
                status = InhDurationStatus.Long;
            else
                status = InhDurationStatus.None;
            
            return new InhalationDuration(durationSeconds, status);
        }
        
        public  string GetBreatheDescription(int number = 0)
        {
            var requiredVentilationsCount = (int)_configManager.GetParameterValue(BraydenConfigKeys.CYCLE_REQUIRED_VENTILATIONS_COUNT_KEY);

            var numberDescription = number > requiredVentilationsCount
                ? $"<color=#F9544A>{number}</color>"
                : number.ToString();

            return $"{numberDescription}/{requiredVentilationsCount.ToString()}";
        }

        public  InhalationModel CreateEmptyInhalation(byte number)
        {
            var inhalation = new InhalationModel(number, new LungCapacity(0f, LungCapacityStatus.None), null);
            
            inhalation.UpdateDuration(new InhalationDuration(0, InhDurationStatus.None));
            inhalation.UpdateDelayDcmprAndInh(new DelayDecompressionAndInhalation(0, DelayDcmprAndInhStatus.None));
            inhalation.UpdateDelayFirstSecondInh(new DelayFirstAndSecondInhalation(0, DelayFirstAndSecondInhStatus.None));
            
            return inhalation;
        }

        public  ExhalationModel CreateEmptyExhalation(byte number)
        {
            var exhalation = new ExhalationModel(number);
            exhalation.UpdateVolume(new ExhalationVolume(0, ExhalationStatus.None));
            return exhalation;
        }
    }
}