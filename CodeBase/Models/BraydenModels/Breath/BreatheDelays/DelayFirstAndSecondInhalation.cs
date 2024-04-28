namespace CodeBase.Models.BraydenModels.Breath
{
    public struct DelayFirstAndSecondInhalation
    {
        public float DelayValue { get; private set; }
        public DelayFirstAndSecondInhStatus Status { get; private set; }

        public DelayFirstAndSecondInhalation(float delayValue, DelayFirstAndSecondInhStatus status)
        {
            DelayValue = delayValue;
            Status = status;
        }

        public override string ToString()
        {
            return $"D: {DelayValue:F1}. DS: {Status.ToString()}";
        }
    }

    public enum DelayFirstAndSecondInhStatus
    {
        None,
        Early,
        Normal,
        Late
    }
}