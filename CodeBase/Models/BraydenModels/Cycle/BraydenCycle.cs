using System.Collections.Generic;
using System.Linq;
using CodeBase.Decompression;
using CodeBase.Models.BraydenModels.Breath;
using CodeBase.Models.BraydenModels.Breath.Exhalation;
using CodeBase.Settings;

namespace CodeBase
{
    public class BraydenCycle
    {
        public int Number { get; }
        public CycleTransition Transition { get; private set; }

        public int CompressionsCount => FullCompressionModels.Count;
        public int BreatheCount => FullBreatheModels.Count;

        public bool IsCorrectCycle
        {
            get
            {
                var requiredCompressionsCount = (int) _settings.ConfigManager.GetParameterValue(BraydenConfigKeys.CYCLE_REQUIRED_COMPRESSIONS_COUNT_KEY);
                var requiredVentilationsCount = (int)_settings.ConfigManager.GetParameterValue(BraydenConfigKeys.CYCLE_REQUIRED_VENTILATIONS_COUNT_KEY);

                return FullCompressionModels.Count == requiredCompressionsCount && FullBreatheModels.Count == requiredVentilationsCount;
            }
        }

        public bool IsNeedFinishCycle =>
            IsCorrectCycle || 
            BreatheCount > 0;

        public Dictionary<int, FullCompressionModel> FullCompressionModels { get; } = new();
        public Dictionary<int, FullBreatheModel> FullBreatheModels { get; } = new();

        private readonly BraydenGlobalSettingsManager _settings;
        public BraydenCycle(int number, BraydenGlobalSettingsManager settingsManager)
        {
            Number = number;
            _settings = settingsManager;
        }

        public void AddCompression(CompressionModel model)
        {
            var index = FullCompressionModels.Count + 1;
            var fcModel = new FullCompressionModel(index);

            var prevModel = FullCompressionModels.LastOrDefault().Value?.Compression;
            var frequency = _settings.CompressionManager.GetCompressionFrequency(prevModel, model);
            
            model.UpdateFrequency(frequency);
            
            FullCompressionModels.Add(index, fcModel.AddCompression(model));
        }


        public void AddInhalation(InhalationModel model)
        {
            var index = FullBreatheModels.Count + 1;
            var breatheModel = new FullBreatheModel(index);

            var prevDecompression = FullCompressionModels.LastOrDefault().Value?.Decompression;
            var isFirstInhalation = FullBreatheModels.Count == 0;
            var delayDcmprAndInh = _settings.BreatheManager.GetDelayDcmprAndInh(prevDecompression, model, isFirstInhalation);
            
            model.UpdateDelayDcmprAndInh(delayDcmprAndInh);
            
            DelayFirstAndSecondInhalation delayFirstSecondInh;

            if (FullBreatheModels.Count >= 2 || !FullBreatheModels.Any())
            {
                delayFirstSecondInh = _settings.BreatheManager.GetDelayFirstSecondInh(null, null);
            }
            else
            {
                var firstModel = FullBreatheModels.FirstOrDefault().Value?.Inhalation;
                delayFirstSecondInh = _settings.BreatheManager.GetDelayFirstSecondInh(firstModel, model);
            }
            
            model.UpdateDelayFirstSecondInh(delayFirstSecondInh);
            
            FullBreatheModels.Add(index, breatheModel.AddInhalation(model));
        }

        public void AddDecompression(DecompressionModel model)
        {
            var index = FullCompressionModels.Count;
            if (!FullCompressionModels.ContainsKey(index))
                return;

            FullCompressionModels[index].AddDecompression(model);
        }

        public void AddExhalation(ExhalationModel model)
        {
            var index = FullBreatheModels.Count;
            if (!FullBreatheModels.ContainsKey(index))
                return;

            FullBreatheModels[index].AddExhalation(model);
        }

        public void UpdateTransition(CycleTransition transition)
        {
            Transition = transition;
        }

        public CompressionModel GetFirstCompression()
        {
            if (FullCompressionModels.Count == 0)
                return null;

            var firstCompression = FullCompressionModels.First().Value.Compression;
            return firstCompression;
        }
        public CompressionModel GetLastCompression()
        {
            if (FullCompressionModels.Count == 0)
                return null;

            var lastCompression = FullCompressionModels.Last().Value.Compression;
            return lastCompression;
        }

        public void FillCycleIfNeeded()
        {
            var requiredCompressions = (int) _settings.ConfigManager.GetParameterValue(BraydenConfigKeys.CYCLE_REQUIRED_COMPRESSIONS_COUNT_KEY);
            
            var startedIndex = CompressionsCount;
            var needCount = requiredCompressions - startedIndex;

            for (var i = 0; i < needCount; i++)
            {
                var number = (byte)(i + 1 + startedIndex);

                var compression = CompressionManager.CreateEmptyCompression(number);
                var decompression = DecompressionManager.CreateEmptyDecompression(number);
                var fullModel = new FullCompressionModel(number);

                fullModel.AddCompression(compression);
                fullModel.AddDecompression(decompression);

                FullCompressionModels.Add(number, fullModel);
            }

            startedIndex = BreatheCount;

            var requiredVentilationsCount = (int) _settings.ConfigManager.GetParameterValue(BraydenConfigKeys.CYCLE_REQUIRED_VENTILATIONS_COUNT_KEY);
            needCount = requiredVentilationsCount - startedIndex;

            for (var i = 0; i < needCount; i++)
            {
                var number = (byte)(i + 1 + startedIndex);

                var inhalation = _settings.BreatheManager.CreateEmptyInhalation(number);
                var exhalation = _settings.BreatheManager.CreateEmptyExhalation(number);

                var fullModel = new FullBreatheModel(number);

                fullModel.AddInhalation(inhalation);
                fullModel.AddExhalation(exhalation);

                FullBreatheModels.Add(number, fullModel);
            }
        }

        public override string ToString()
        {
            return $"#{Number}.\n" +
                   "[Statuses]\n" +
                   $"CountStatus: {IsCorrectCycle.ToString()}\n" +
                   $"{Transition.ToString()}";
        }
    }
}