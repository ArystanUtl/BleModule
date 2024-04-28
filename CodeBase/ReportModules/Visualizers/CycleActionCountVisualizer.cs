using CodeBase.ReportModules.Models;
using TMPro;
using UnityEngine;

namespace CodeBase.ReportVisualizers
{
    public class CycleActionCountVisualizer : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text countText;

        public void Setup(CycleActionCountModel model)
        {
            titleText.text = string.Concat(model.ActionTitle, " ", model.ActionCount.ToString());
        }
    }
}