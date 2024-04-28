using System;
using CodeBase.Extensions;
using CodeBase.Models.BraydenModels.Breath;
using static Modules.Books.TextData;

namespace CodeBase
{
    public static class BraydenReportManager
    {
        public static string GetCompressionAnswerDescription(FullCompressionModel model)
        {
            var cmpr = model.Compression;
            var dcmpr = model.Decompression;
            
            var depth = cmpr.Status.GetDescription();
            var frequency = cmpr.FrequencyValue.SpeedType.GetDescription();
            var handsPosition = cmpr.PositionOfHands.GetDescription();
            var decompression = dcmpr.Status.GetDescription();
            
            var result = string.Concat(
                Get(496) + " ", cmpr.ResultValue.ToString("F1"), $" {Get(526)}, ", depth, Environment.NewLine,
                Get(497) + " ", cmpr.FrequencyValue.ResultValue.ToString("F1"), ", ", frequency, Environment.NewLine,
                Get(498) + " ", handsPosition, Environment.NewLine,
                Get(499) + " ", dcmpr.ResultValue.ToString("F1"), $" {Get(526)}, ", decompression);

            return result;
        }

        public static string GetBreatheAnswerDescription(FullBreatheModel model)
        {
            var inhalation = model.Inhalation;
            var exhalation = model.Exhalation;

            var lungCapacity = inhalation.LungCapacity.Status.GetDescription();
            var exhCapacity = exhalation.ExhalationVolume.Status.GetDescription();
            var duration = inhalation.Duration.Status.GetDescription();

            var delayInh = inhalation.DelayDcmprInh.Status.GetDescription();
            var delayDescription = model.Number == 1
                ? string.Concat(Get(522) + " ", inhalation.DelayDcmprInh.DelayValue.ToString("F1"), $"{Get(528)}, ", delayInh, Environment.NewLine)
                : string.Empty;
                
            var result = string.Concat(
                delayDescription,
                Get(511) + " ", inhalation.LungCapacity.CapacityValue.ToString("F1"), $"{Get(527)}, ", lungCapacity, Environment.NewLine,
                Get(512) + " ", exhCapacity, Environment.NewLine,
                Get(518) + " ", inhalation.Duration.DurationValue.ToString("F1"), $"{Get(528)}, ", duration, Environment.NewLine);

            return result;
        }
    }
}