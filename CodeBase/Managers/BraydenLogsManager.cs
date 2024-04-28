using System.Collections.Generic;
using System.Linq;

namespace CodeBase
{
    public static class BraydenLogsManager
    {
        private const int COMPRESSION_NUMBER_INDEX = 12;
        private const int COMPRESSION_FREQUENCY_INDEX = 11;
        private const int COMPRESSION_POSITION_INDEX = 13;

        private const int RESP_NUMBER_INDEX = 17;
        private const int RESP_FREQUENCY_INDEX = 16;
        
        private static readonly int[] ReservedBytesIndexes = { 0, 18, 19 };
        private static readonly int[] RespVolumeIndexes = { 14, 15 };

        private static LogType GetLogTypeByIndex(int index)
        {
            return index switch
            {
                COMPRESSION_NUMBER_INDEX => LogType.CompressionNumber,
                COMPRESSION_FREQUENCY_INDEX => LogType.CompressionFrequency,
                COMPRESSION_POSITION_INDEX => LogType.CompressionHandsPosition,

                RESP_NUMBER_INDEX => LogType.BreatheNumber,
                RESP_FREQUENCY_INDEX => LogType.BreatheFrequency,

                _ => ReservedBytesIndexes.Contains(index)
                    ? LogType.Reserved
                    : RespVolumeIndexes.Contains(index)
                        ? LogType.BreatheCapacity
                        : LogType.CompressionDecompressionDepth
            };
        }
        
        
        public static byte GetByteByLogType(LogType logType, byte[] bytes)
        {
            for (var i = 0; i < bytes.Length; i++)
            {
                var currentByte = bytes[i];
                var currentLogType = GetLogTypeByIndex(i);
                if (logType == currentLogType)
                    return currentByte;
            }

            return 0;
        }
        
        
        public static List<byte> GetBytesByLogType(LogType logType, byte[] bytes)
        {
            var result = new List<byte>();
            
            for (var i = 0; i < bytes.Length; i++)
            {
                var currentByte = bytes[i];
                var currentLogType = GetLogTypeByIndex(i);
                if (logType == currentLogType)
                    result.Add(currentByte);
            }

            return result;
        }
        
        public static (int min, int max) GetMinAndMaxIndices(IEnumerable<GradationModel> values)
        {
            var gradationModels = values as GradationModel[] ?? values.ToArray();
            var bytes = gradationModels.Select(x => x.ByteValue).ToList();
            
            var minIndex= bytes.IndexOf(bytes.Min());
            var otherValues = bytes.GetRange(minIndex, bytes.Count - minIndex);
            var maxIndex = bytes.IndexOf(otherValues.Max());
            
            return (minIndex, maxIndex);
        }
    }
}