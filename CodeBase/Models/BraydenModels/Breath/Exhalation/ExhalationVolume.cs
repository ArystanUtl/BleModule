namespace CodeBase.Models.BraydenModels.Breath.Exhalation
{
    public struct ExhalationVolume
    {
        public float CapacityValue { get; private set; }
        public ExhalationStatus Status { get; private set; }

        public ExhalationVolume(float capacity, ExhalationStatus status)
        {
            CapacityValue = capacity;
            Status = status;
        }

        public override string ToString()
        {
            return $"Capacity: {CapacityValue:F1} | Status: {Status.ToString()}";
        }
    }

    public enum ExhalationStatus
    {
        None,
        Correct,
        Incorrect
    }
}