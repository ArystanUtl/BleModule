
using UnityEngine;

namespace CodeBase.ReportModules.Models
{
    public class CycleActionCountModel
    {
        public string ActionTitle { get; private set; }
        public int ActionCount { get; private set; }
        
        public CycleActionCountModel(string title, int count)
        {
            ActionTitle = title;
            ActionCount = count;
        }
    }
}