namespace CodeBase.Models.BraydenModels.Breath
{
    public struct LungCapacity
    {
        public float CapacityValue { get; }
        public LungCapacityStatus Status { get; }

        public LungCapacity(float value, LungCapacityStatus status)
        {
            CapacityValue = value;
            Status = status;
        }

        public override string ToString()
        {
            return $"Capacity: {CapacityValue:F1} ml | Status: {Status.ToString()}";
        }
    }
    
    public enum LungCapacityStatus
    {
        None,
        NotEnough,
        Normal,
        TooMuch
    }
}