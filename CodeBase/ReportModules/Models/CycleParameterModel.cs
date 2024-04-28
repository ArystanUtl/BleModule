using UnityEngine;

namespace CodeBase.ReportModules.Models
{
    public class CycleParameterModel
    {
        public readonly string Title;
        
        public readonly int UnderPercentage;
        public readonly int NormalPercentage;
        public readonly int OverPercentage;

        private int TotalSum => UnderPercentage + NormalPercentage + OverPercentage;

        public CycleParameterModel(string title, float underPercentage, float normalPercentage, float overPercentage)
        {
            Title = title;
            
            UnderPercentage = Mathf.RoundToInt(underPercentage);
            NormalPercentage = Mathf.RoundToInt(normalPercentage);
            OverPercentage = Mathf.RoundToInt(overPercentage);

            if (TotalSum == 100)
                return;

            var offSet = TotalSum > 100
                ? TotalSum - 100
                : 100 - TotalSum;

            if (NormalPercentage > 0)
                NormalPercentage += offSet;
            else if (OverPercentage > 0)
                OverPercentage += offSet;
            else
                UnderPercentage += offSet;
        }
    }
}