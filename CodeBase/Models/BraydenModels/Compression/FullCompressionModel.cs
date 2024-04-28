using CodeBase.Decompression;

namespace CodeBase
{
    public class FullCompressionModel
    {
        public int Number { get; set; }

        public CompressionModel Compression { get; private set; }
        public DecompressionModel Decompression { get; private set; }

        public FullCompressionModel(int number)
        {
            Number = number;
        }

        public FullCompressionModel AddCompression(CompressionModel model)
        {
            Compression = model;
            return this;
        }

        public FullCompressionModel AddDecompression(DecompressionModel model)
        {
            Decompression = model;
            return this;
        }
    }
}