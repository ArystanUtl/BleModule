using System.Collections.Generic;

namespace CodeBase
{
    public abstract class BleResultModel
    {
        public readonly byte Number;
        public readonly List<GradationModel> Gradations = new();
        
        public float ResultValue { get; private set; }
    
        protected BleResultModel(byte number)
        {
            Number = number;
        }

        protected BleResultModel(byte number, float resultValue)
        {
            Number = number;
            ResultValue = resultValue;
        }

        public virtual void AddGradation(GradationModel model)
        {
            Gradations.Add(model);
        }
    }
}