namespace CodeBase
{
    public class GradationModel
    {
        public byte ByteValue { get; }

        public readonly byte[] AllBytes;
       
        public GradationModel(byte byteValue, byte[] fullBytes)
        {
            ByteValue = byteValue;
            AllBytes = fullBytes;
        }
        
        public override string ToString()
        {
            return ByteValue.ToString();
        }
    }
}