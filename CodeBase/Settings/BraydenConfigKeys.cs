namespace CodeBase.Settings
{
    /// <summary>
    /// Class contains keys for Getting parameters from BraydenBook.
    /// </summary>
    public static class BraydenConfigKeys
    {
        #region Compression parameters keys

        public const string DEPTH_COEFFICIENT_KEY = "DepthCoefficient";
        public const string MIN_DEPTH_NORMA_KEY = "MinDepthNorma";
        public const string MAX_DEPTH_NORMA_KEY = "MaxDepthNorma";
        public const string MIN_FREQUENCY_NORMA_KEY = "MinFrequencyNorma";
        public const string MAX_FREQUENCY_NORMA_KEY = "MaxFrequencyNorma";

        #endregion

        #region Decompression parameters keys

        public const string DECOMPRESSION_FINISH_BYTE_KEY = "DecompressionFinishByte";
        public const string DECOMPRESSION_MAX_DEPTH_KEY = "DecompressionMaxDepth";

        #endregion

        #region Ventilation parameters keys

        public const string CAPACITY_COEFFICIENT_KEY = "CapacityCoefficient";
        public const string EXHALATION_FINISH_BYTE_KEY = "ExhalationFinishByte";
        public const string MIN_CAPACITY_NORMA_KEY = "CapacityMinNorma";
        public const string MAX_CAPACITY_NORMA_KEY = "CapacityMaxNorma";
        public const string EXHALATION_CAPACITY_MAX_NORMA_KEY = "ExhalationCapacityMaxNorma";
        public const string DELAY_AFTER_DECOMPRESSION_MAX_NORMA_KEY = "DelayAfterDecompressionMaxNorma";
        public const string DELAY_FIRST_SECOND_INHALATIONS_MIN_NORMA_KEY = "DelayBetweenFirstSecondInhalationsMinNorma";
        public const string DELAY_FIRST_SECOND_INHALATIONS_MAX_NORMA_KEY = "DelayBetweenFirstSecondInhalationsMaxNorma";
        public const string INHALATION_DURATION_MIN_NORMA_KEY = "InhalationDurationMinNorma";
        public const string INHALATION_DURATION_MAX_NORMA_KEY = "InhalationDurationMaxNorma";

        #endregion


        #region Cycle parameters keys

        public const string CYCLE_REQUIRED_COMPRESSIONS_COUNT_KEY = "CycleRequiredCompressionsCount";
        public const string CYCLE_REQUIRED_VENTILATIONS_COUNT_KEY = "CycleRequiredVentilationsCount";
        public const string CYCLE_TRANSITION_DELAY_MIN_NORMA_KEY = "CycleTransitionDelayMinNorma";
        public const string CYCLE_TRANSITION_DELAY_MAX_NORMA_KEY = "CycleTransitionDelayMaxNorma";

        #endregion
        
        private const string SAVE_ID_PREFIX = "save_parameter_id";
        private const string SAVE_MODE_PREFIX = "save_parameter_mode";
        
        public static string GetSaveIdKeyForParameter(string parameterID)
        {
            return string.Concat(SAVE_ID_PREFIX, parameterID);
        }

        public static string GetSaveModeKeyForParameter(string parameterID)
        {
            return string.Concat(SAVE_MODE_PREFIX, parameterID);
        }
    }
}