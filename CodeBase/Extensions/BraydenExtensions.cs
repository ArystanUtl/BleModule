using CodeBase.Decompression;
using CodeBase.Models.BraydenModels.Breath;
using CodeBase.Models.BraydenModels.Breath.Exhalation;
using UnityEngine;
using static CodeBase.BraydenVisualizerConstants;
using static Modules.Books.TextData;

namespace CodeBase.Extensions
{
    public static class BraydenExtensions
    {
        public static Color GetColor(this FrequencyType frequencyType)
        {
            return frequencyType switch
            {
                FrequencyType.Fast => OverNormaColor,
                FrequencyType.Slow => UnderNormaColor,
                _ => NormaColor
            };
        }

        public static Color GetColor(this HandsPosition handsPosition)
        {
            return handsPosition switch
            {
                HandsPosition.Center => NormaColor,
                _ => OverNormaColor
            };
        }

        public static string GetDescription(this DepthStatus depth)
        {
            var descriptionIndex = depth switch
            {
                DepthStatus.None => 525,
                DepthStatus.Weak => 502,
                DepthStatus.Norm => 501,
                DepthStatus.Strong => 500,
                _ => 0
            };

            return Get(descriptionIndex);
        }

        public static string GetDescription(this FrequencyType frequencyType)
        {
            var descriptionIndex = frequencyType switch
            {
                FrequencyType.None => 525,
                FrequencyType.Slow => 505,
                FrequencyType.Norm => 504,
                FrequencyType.Fast => 503,
                _ => 0
            };

            return Get(descriptionIndex);
        }

        public static string GetDescription(this HandsPosition handsPosition)
        {
            var descriptionIndex = handsPosition switch
            {
                HandsPosition.None => 525,
                HandsPosition.Center => 508,
                _ => 509
            };

            return Get(descriptionIndex);
        }

        public static string GetDescription(this DecompressionCorrectStatus status)
        {
            var descriptionIndex = status switch
            {
                DecompressionCorrectStatus.Correct => 507,
                DecompressionCorrectStatus.Incorrect => 506,
                DecompressionCorrectStatus.None => 0,
                _ => 0
            };

            return Get(descriptionIndex);
        }


        public static string GetDescription(this LungCapacityStatus status)
        {
            var descriptionIndex = status switch
            {
                LungCapacityStatus.None => 525,
                LungCapacityStatus.Normal => 514,
                LungCapacityStatus.TooMuch => 513,
                LungCapacityStatus.NotEnough => 515,
                _ => 0
            };

            return Get(descriptionIndex);
        }

        public static string GetDescription(this ExhalationStatus status)
        {
            var descriptionIndex = status switch
            {
                ExhalationStatus.Incorrect => 516,
                ExhalationStatus.Correct => 517,
                ExhalationStatus.None => 525,
                _ => 0
            };

            return Get(descriptionIndex);
        }

        public static string GetDescription(this InhDurationStatus duration)
        {
            var descriptionIndex = duration switch
            {
                InhDurationStatus.None => 525,
                InhDurationStatus.Long => 519,
                InhDurationStatus.Normal => 520,
                InhDurationStatus.Short => 521,
                _ => 0
            };

            return Get(descriptionIndex);
        }

        public static string GetDescription(this DelayDcmprAndInhStatus status)
        {
            var descriptionIndex = status switch
            {
                DelayDcmprAndInhStatus.None => 525,
                DelayDcmprAndInhStatus.Correct => 523,
                DelayDcmprAndInhStatus.Incorrect => 524,
                _ => 0
            };

            return Get(descriptionIndex);
        }


        public static void SetWidthAndPosX(this RectTransform rectTransform, float width, float prevImgWidth, float prevImgX)
        {
            var currentHeight = rectTransform.sizeDelta.y;
            rectTransform.sizeDelta = new Vector2(width, currentHeight);

            var currentPosY = rectTransform.anchoredPosition.y;
            var currentPosX = prevImgWidth + prevImgX;
            rectTransform.anchoredPosition = new Vector2(currentPosX, currentPosY);
        }
    }
}