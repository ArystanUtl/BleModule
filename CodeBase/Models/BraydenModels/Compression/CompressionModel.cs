using System;
using System.Collections.Generic;
using System.Linq;
using Modules.Common;

namespace CodeBase
{
    public class CompressionModel : BleResultModel
    {
        public DateTime StartedTime { get; }

        public bool IsCorrect =>
            PositionOfHands is HandsPosition.Center &&
            Status is DepthStatus.Norm && 
            FrequencyValue.SpeedType is FrequencyType.Norm;

        public bool IsEmpty =>
            PositionOfHands is HandsPosition.None &&
            Status is DepthStatus.None &&
            FrequencyValue.SpeedType is FrequencyType.None;
        
        
        
        public DepthStatus Status { get; } = DepthStatus.None;
        public HandsPosition PositionOfHands { get; } = HandsPosition.None;
        public Frequency FrequencyValue { get; private set; }
        
        
        public CompressionModel(byte number) : base(number)
        {
            StartedTime = DateTime.Now;
        }

        public CompressionModel(byte number, float depth, DepthStatus status, HandsPosition positionOfHands, List<GradationModel> gradations) 
            : base(number, depth)
        {
            StartedTime = DateTime.Now;
            
            Status = status;
            PositionOfHands = positionOfHands;
            gradations ??= new List<GradationModel>();
            
            Gradations.AddRange(gradations);
        }

        public void UpdateFrequency(Frequency frequency)
        {
            FrequencyValue = frequency;
        }

        public bool IsCompressionStartedCorrect()
        {
            if (Gradations.IsNullOrEmpty())
                return true;

            var minByte = Gradations.Select(x => x.ByteValue).Min();
            return minByte < 18;
        }

        public override string ToString()
        {
            return $"#{Number}\n" +
                   "[Params]\n"+
                        $"Depth: {ResultValue} cm | Status: {Status.ToString()}.\n" +
                        $"Pos: {PositionOfHands.ToString()}.\n" +
                        $"Frequency: {FrequencyValue.ToString()}";
        }
    }
}