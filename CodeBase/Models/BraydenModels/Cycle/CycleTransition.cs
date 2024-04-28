namespace CodeBase
{

    public struct CycleTransition
    {
        public float Duration { get; private set; }
        public CycleTransitionStatus Status { get; }

        public CycleTransition(float duration, CycleTransitionStatus status)
        {
            Duration = duration;
            Status = status;
        }

        public override string ToString()
        {
            return "Transition:\n" +
                   $"Duration: {Duration:F1} | Status: {Status.ToString()}";
        }
    }
    
    
    public enum CycleTransitionStatus
    {
        None, 
        NoCompressions,
        LessThenNorma,
        MoreThenNorma,
        Correct
    }
}