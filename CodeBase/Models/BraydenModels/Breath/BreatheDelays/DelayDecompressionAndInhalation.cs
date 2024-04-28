namespace CodeBase.Models.BraydenModels.Breath
{
    public struct DelayDecompressionAndInhalation
    {
        public float DelayValue { get; }
        public DelayDcmprAndInhStatus Status { get; }

        public DelayDecompressionAndInhalation(float delay, DelayDcmprAndInhStatus status)
        {
            DelayValue = delay;
            Status = status;
        }

        public override string ToString()
        {
            return $"D: {DelayValue:F1} DS: {Status.ToString()}";
        }
    }
    
    public enum DelayDcmprAndInhStatus
    {
        None,
        Correct,
        Incorrect
    }
}