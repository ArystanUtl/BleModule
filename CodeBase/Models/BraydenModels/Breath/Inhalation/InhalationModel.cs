using System;
using System.Collections.Generic;
using System.Linq;
using CodeBase.Models.BraydenModels.Breath;
using Modules.Common;

namespace CodeBase
{
    public class InhalationModel : BleResultModel
    {
        public LungCapacity LungCapacity;
        public DateTime StartedTime { get; private set; }
       
        #region Params

        public DelayDecompressionAndInhalation DelayDcmprInh { get; private set; }
        public DelayFirstAndSecondInhalation DelayFirstSecondInh { get; private set; }
        
        public InhalationDuration Duration { get; private set; }
        #endregion

        public bool IsCorrect =>
            LungCapacity.Status is LungCapacityStatus.Normal &&
            DelayDcmprInh.Status is DelayDcmprAndInhStatus.Correct &&
            DelayFirstSecondInh.Status is DelayFirstAndSecondInhStatus.Normal;

        public bool IsEmpty =>
            LungCapacity.Status is LungCapacityStatus.None &&
            DelayDcmprInh.Status is DelayDcmprAndInhStatus.None &&
            DelayFirstSecondInh.Status is DelayFirstAndSecondInhStatus.None;

        public InhalationModel(byte number) : base(number)
        {
        }

        public InhalationModel(byte number, LungCapacity lungCapacity, List<GradationModel> gradations) : base(number)
        {
            LungCapacity = lungCapacity;
            if (gradations.IsCorrect())
                Gradations.AddRange(gradations);
        }

        public override void AddGradation(GradationModel model)
        {
            if (Gradations.Any())
                if (Gradations.Last().ByteValue == 0 && model.ByteValue != 0)
                    StartedTime = DateTime.Now;

            base.AddGradation(model);
        }

        public void RegisterStartedTime(DateTime time)
        {
            StartedTime = time;
        }

        public override string ToString()
        {
            return $"#{Number}.\n" +
                   "[Params]\n" +
                        $"{LungCapacity.ToString()}.\n" +
                        $"{Duration.ToString()}\n" +
                   "[Delays]\n" +
                        $"DcmprInh: {DelayDcmprInh}\n"+
                        $"FirstSecondInh: {DelayFirstSecondInh}";
        }

        public void UpdateDelayDcmprAndInh(DelayDecompressionAndInhalation value)
        {
            DelayDcmprInh = value;
        }

        public void UpdateDelayFirstSecondInh(DelayFirstAndSecondInhalation value)
        {
            DelayFirstSecondInh = value;
        }

        public void UpdateDuration(InhalationDuration duration)
        {
            Duration = duration;
        }
    }
}