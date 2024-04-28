namespace CodeBase
{
    public struct InhalationDuration
    {
        public float DurationValue { get; private set; }
        public InhDurationStatus Status { get; private set; }

        public InhalationDuration(float duration, InhDurationStatus status)
        {
            DurationValue = duration;
            Status = status;
        }

        public override string ToString()
        {
            return $"Duration: {DurationValue:F1} | Status: {Status.ToString()}";
        }
    }

    public enum InhDurationStatus
    {
        None,
        Short,
        Normal,
        Long
    }
}