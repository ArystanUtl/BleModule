namespace CodeBase
{
    public struct Frequency
    {
        public float ResultValue { get; private set; }
        public FrequencyType SpeedType { get; private set; }

        public Frequency(float result, FrequencyType frequencyType)
        {
            ResultValue = result;
            SpeedType = frequencyType;
        }

        public override string ToString()
        {
            return $"{ResultValue:F1} | {SpeedType.ToString()}";
        }
    }
    
    public enum FrequencyType
    {
        None,
        Slow,
        Norm,
        Fast
    }
}