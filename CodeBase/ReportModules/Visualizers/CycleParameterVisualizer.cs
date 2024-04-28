using System;
using CodeBase.Extensions;
using CodeBase.ReportModules.Models;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CodeBase.ReportVisualizers
{
    public class CycleParameterVisualizer : MonoBehaviour
    {
        [Header("Main controls")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private RectTransform container;
        
        [Space(10)]
        [Header("Value images")]
        [SerializeField] private RectTransform underValueImage;
        [SerializeField] private RectTransform normalValueImage;
        [SerializeField] private RectTransform overValueImage;

        [Space(10)]
        [Header("Value percentage texts")]
        [SerializeField] private TMP_Text underValueText;
        [SerializeField] private TMP_Text normalValueText;
        [SerializeField] private TMP_Text overValueText;

        private CycleParameterModel _model;

        private void OnEnable()
        {
           // VisualizeModelData().Forget();
        }
        
        public void Setup(CycleParameterModel model)
        {
            if (model == null)
                return;
            
            _model = model;
        }

        public async UniTask VisualizeModelData()
        {
            ResetImages();
            if (_model == null)
                return;
            
            await UniTask.Yield();
            titleText.text = _model.Title;
            
            await UniTask.WaitWhile(() => container.rect.size.x < 100f);
            
            var parentWidthPercentage = container.rect.size.x / 100f;
            
            var underImageWidth = CalculateImageWidth(_model.UnderPercentage, parentWidthPercentage);
            var normalImageWidth = CalculateImageWidth(_model.NormalPercentage, parentWidthPercentage);
            var overImageWidth = CalculateImageWidth(_model.OverPercentage, parentWidthPercentage);
            
            underValueImage.SetWidthAndPosX(underImageWidth, 0, 0);
            normalValueImage.SetWidthAndPosX(normalImageWidth, underImageWidth, underValueImage.anchoredPosition.x);
            overValueImage.SetWidthAndPosX(overImageWidth, normalImageWidth, normalValueImage.anchoredPosition.x);
            
            SetupValueText(underValueText, _model.UnderPercentage);
            SetupValueText(normalValueText, _model.NormalPercentage);
            SetupValueText(overValueText, _model.OverPercentage);
            await UniTask.Yield();
        }

        private static float CalculateImageWidth(float value, float parentWidthPercentage)
        {
            var imageWidth = value * parentWidthPercentage;
            return imageWidth;
        }

        private static void SetupValueText(TMP_Text textControl, int value)
        {
            var resultText = $"{value}%";
            textControl.text = value >= 6
                ? resultText
                : "";
        }
        
        private void ResetImages()
        {
            underValueImage.SetWidthAndPosX(0, 0, 0);
            normalValueImage.SetWidthAndPosX(0, 0, 0);
            overValueImage.SetWidthAndPosX(0, 0, 0);

            normalValueText.text = "";
            underValueText.text = "";
            overValueText.text = "";
        }
    }
}