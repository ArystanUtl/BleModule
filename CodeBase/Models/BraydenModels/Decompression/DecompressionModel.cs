using System;
using System.Collections.Generic;
using Modules.Common;

namespace CodeBase.Decompression
{
    public class DecompressionModel : BleResultModel
    {
        public DecompressionCorrectStatus Status { get; }
        public DateTime StartedTime { get; private set; }

        public bool IsEmpty => Status is DecompressionCorrectStatus.None;

        public DecompressionModel(byte number) : base(number)
        {
           
        }

        public DecompressionModel(byte number, float resultValue, DateTime startedTime, DecompressionCorrectStatus status, List<GradationModel> gradations) : base(number, resultValue)
        {
            Status = status;
            StartedTime = startedTime;

            if (gradations.IsCorrect())
                Gradations.AddRange(gradations);
        }

        public void RegisterStartedTime()
        {
            StartedTime = DateTime.Now;
        }

        public override string ToString()
        {
            return $"#{Number}.\n" +
                   $"[Params]\n" +
                        $"Depth: {ResultValue} cm | Status: {Status.ToString()}";
        }
    }
}