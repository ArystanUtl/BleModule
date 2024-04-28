using System;
using System.Collections.Generic;
using System.Linq;
using CodeBase.Decompression;
using CodeBase.Models.BraydenModels.Breath;
using CodeBase.ReportModules.Models;
using CodeBase.Settings;
using Modules.Books;

namespace CodeBase
{
    public enum CycleResultType
    {
        CompressionCount,
        CompressionDepth,
        CompressionFrequency,
        HandPosition,
        Decompression,
        
        BreatheCount,
        VentilationVolume,
        VentilationDuration
    }

    public class CycleManager
    {
        private readonly BraydenConfigManager _configManager;
        private readonly Dictionary<CycleResultType, Func<BraydenCycle, CycleResultModel>> _calculationMethods;

        public CycleManager(BraydenConfigManager configManager)
        {
            _configManager = configManager;
            
            _calculationMethods = new Dictionary<CycleResultType, Func<BraydenCycle, CycleResultModel>>
            {
                { CycleResultType.CompressionCount, CalculateCompressionsCount },
                { CycleResultType.BreatheCount, CalculateBreatheCount },
                { CycleResultType.CompressionDepth, CalculateCompressionDepth },
                { CycleResultType.CompressionFrequency, CalculateCompressionFrequency },
                { CycleResultType.HandPosition, CalculateCompressionHandsPosition },
                { CycleResultType.Decompression, CalculateDecompressionStatus },
                { CycleResultType.VentilationVolume, CalculateBreatheVolume },
                { CycleResultType.VentilationDuration, CalculateBreatheDuration }
            };
        }
        
        public CycleTransition GetTransitionStatus(BraydenCycle prevCycle, BraydenCycle currentCycle)
        {
            var lastCompression = prevCycle.GetLastCompression();
            var currentCompression = currentCycle.GetFirstCompression();

            if (lastCompression == null)
                return new CycleTransition(0f, CycleTransitionStatus.NoCompressions);

            var differenceTime = currentCompression.StartedTime - lastCompression.StartedTime;
            var differenceSeconds = (float)differenceTime.TotalSeconds;

            var minNorma = _configManager.GetParameterValue(BraydenConfigKeys.CYCLE_TRANSITION_DELAY_MIN_NORMA_KEY);
            var maxNorma = _configManager.GetParameterValue(BraydenConfigKeys.CYCLE_TRANSITION_DELAY_MAX_NORMA_KEY);

            var result = CycleTransitionStatus.None;

            if (differenceSeconds >= minNorma && differenceSeconds <= maxNorma)
                result = CycleTransitionStatus.Correct;
            else if (differenceSeconds < minNorma)
                result = CycleTransitionStatus.LessThenNorma;
            else if (differenceSeconds > maxNorma)
                result = CycleTransitionStatus.MoreThenNorma;
            
            return new CycleTransition(differenceSeconds, result);
        }

        public CycleResultModel CalculateCycleResults(CycleResultType resultType, BraydenCycle cycle)
        {
            if (_calculationMethods.TryGetValue(resultType, out var calculationMethod))
                return calculationMethod(cycle);

            throw new ArgumentException("Unknown result type", nameof(resultType));
        }

        private static CycleResultModel CalculateCompressionsCount(BraydenCycle cycle)
        {
            var count = cycle.FullCompressionModels.Count;
          
            CycleActionCountModel model = new(TextData.Get(532), count);
            var resultModel = new CycleResultModel(model);
            return resultModel;
        }

        private static CycleResultModel CalculateBreatheCount(BraydenCycle cycle)
        {
            var count = cycle.FullBreatheModels.Count;
          
            CycleActionCountModel model = new(TextData.Get(533), count);
            var resultModel = new CycleResultModel(model);

            return resultModel;
        }

        private static CycleResultModel CalculateCompressionDepth(BraydenCycle cycle)
        {
            var underNorma = 0;
            var normCount = 0;
            var overNorma = 0;

            foreach (var compression in cycle.FullCompressionModels.Select(currentModel => currentModel.Value.Compression))
            {
                if (compression.IsEmpty)
                    continue;

                switch (compression.Status)
                {
                    case DepthStatus.Strong:
                        overNorma++;
                        break;
                    case DepthStatus.Norm:
                        normCount++;
                        break;
                    case DepthStatus.Weak:
                        underNorma++;
                        break;
                }
            }

            var totalCount = (float)(underNorma + normCount + overNorma);

            var underPercent = 100f * underNorma / totalCount;
            var normaPercent = 100f * normCount / totalCount;
            var overPercentage = 100f * overNorma / totalCount;

            var model = new CycleParameterModel(TextData.Get(534), underPercent, normaPercent, overPercentage);

            var resultModel = new CycleResultModel(model);
            return resultModel;
        }


        private static CycleResultModel CalculateCompressionFrequency(BraydenCycle cycle)
        {
            var underNorma = 0;
            var normCount = 0;
            var overNorma = 0;

            foreach (var compression in cycle.FullCompressionModels.Select(currentModel => currentModel.Value.Compression))
            {
                if (compression.IsEmpty)
                    continue;

                switch (compression.FrequencyValue.SpeedType)
                {
                    case FrequencyType.Fast:
                        overNorma++;
                        break;
                    case FrequencyType.Norm:
                        normCount++;
                        break;
                    case FrequencyType.Slow:
                        underNorma++;
                        break;
                }
            }

            var totalCount = (float)(underNorma + normCount + overNorma);

            var underPercent = 100f * underNorma / totalCount;
            var normaPercent = 100f * normCount / totalCount;
            var overPercentage = 100f * overNorma / totalCount;

            var model = new CycleParameterModel(TextData.Get(535), underPercent, normaPercent, overPercentage);

            var resultModel = new CycleResultModel(model);
            return resultModel;
        }

        private static CycleResultModel CalculateCompressionHandsPosition(BraydenCycle cycle)
        {
            var correctCount = 0;
            var incorrectCount = 0;

            foreach (var compression in cycle.FullCompressionModels.Select(currentModel => currentModel.Value.Compression))
            {
                if (compression.IsEmpty)
                    continue;

                switch (compression.PositionOfHands)
                {
                    case HandsPosition.Center:
                        correctCount++;
                        continue;
                    case HandsPosition.Down:
                    case HandsPosition.Left:
                    case HandsPosition.Right:
                    case HandsPosition.Top:
                    default:
                        incorrectCount++;
                        break;
                }
            }

            var totalCount = (float)(correctCount + incorrectCount);

            var overPercent = 100f * incorrectCount / totalCount;
            var normaPercent = 100f * correctCount / totalCount;

            var model = new CycleParameterModel(TextData.Get(536), 0, normaPercent, overPercent);

            var resultModel = new CycleResultModel(model);
            return resultModel;
        }


        private static CycleResultModel CalculateDecompressionStatus(BraydenCycle cycle)
        {
            var correctCount = 0;
            var incorrectCount = 0;

            foreach (var decompression in cycle.FullCompressionModels.Select(currentModel => currentModel.Value.Decompression))
            {
                if (decompression.IsEmpty)
                    continue;

                switch (decompression.Status)
                {
                    case DecompressionCorrectStatus.Correct:
                        correctCount++;
                        break;
                    case DecompressionCorrectStatus.Incorrect:
                        incorrectCount++;
                        break;
                }
            }

            var totalCount = (float)(correctCount + incorrectCount);

            var overPercent = 100f * incorrectCount / totalCount;
            var normaPercent = 100f * correctCount / totalCount;

            var model = new CycleParameterModel(TextData.Get(537), 0, normaPercent, overPercent);

            var resultModel = new CycleResultModel(model);
            return resultModel;
        }


        private static CycleResultModel CalculateBreatheVolume(BraydenCycle cycle)
        {
            var underNorma = 0;
            var normCount = 0;
            var overNorma = 0;

            foreach (var inhalation in cycle.FullBreatheModels.Select(currentModel => currentModel.Value.Inhalation))
            {
                if (inhalation.IsEmpty)
                    continue;

                switch (inhalation.LungCapacity.Status)
                {
                    case LungCapacityStatus.TooMuch:
                        overNorma++;
                        break;

                    case LungCapacityStatus.Normal:
                        normCount++;
                        break;

                    case LungCapacityStatus.NotEnough:
                        underNorma++;
                        break;
                }
            }

            var totalCount = (float)(underNorma + normCount + overNorma);

            var underPercent = 100f * underNorma / totalCount;
            var normaPercent = 100f * normCount / totalCount;
            var overPercentage = 100f * overNorma / totalCount;

            var model = new CycleParameterModel(TextData.Get(538), underPercent, normaPercent, overPercentage);

            var resultModel = new CycleResultModel(model);
            return resultModel;
        }


        private static CycleResultModel CalculateBreatheDuration(BraydenCycle cycle)
        {
            var underNorma = 0;
            var normCount = 0;
            var overNorma = 0;

            foreach (var inhalation in cycle.FullBreatheModels.Select(currentModel => currentModel.Value.Inhalation))
            {
                if (inhalation.IsEmpty)
                    continue;

                switch (inhalation.Duration.Status)
                {
                    case InhDurationStatus.Long:
                        overNorma++;
                        break;

                    case InhDurationStatus.Normal:
                        normCount++;
                        break;

                    case InhDurationStatus.Short:
                        underNorma++;
                        break;
                }
            }

            var totalCount = (float)(underNorma + normCount + overNorma);

            var underPercent = 100f * underNorma / totalCount;
            var normaPercent = 100f * normCount / totalCount;
            var overPercentage = 100f * overNorma / totalCount;

            var model = new CycleParameterModel(TextData.Get(539), underPercent, normaPercent, overPercentage);

            var resultModel = new CycleResultModel(model);
            return resultModel;
        }

        public static string GetCycleDescriptionByIndex(int index, bool needToIncrease = true)
        {
            if (needToIncrease)
                index++;
            
            return string.Concat(TextData.Get(543), " ", index);
        }
    }
}