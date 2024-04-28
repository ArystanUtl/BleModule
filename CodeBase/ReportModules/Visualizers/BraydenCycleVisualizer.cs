using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeBase.ReportModules.Models;
using Cysharp.Threading.Tasks;
using Modules.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CodeBase.BraydenVisualizerConstants;

namespace CodeBase.ReportVisualizers
{
    public class BraydenCycleVisualizer : MonoBehaviour
    {
        [SerializeField] private TMP_Text cycleNameText;
        [SerializeField] private Transform parametersContainer;
        [SerializeField] private CycleParameterVisualizer parameterVisualizer;
        [SerializeField] private CycleActionCountVisualizer countVisualizer;
        [SerializeField] private Image graphImage;
        [SerializeField] private RectTransform graphScreenPanel;


        private List<CycleParameterVisualizer> _visualizers = new();
        public void Setup(int index, List<CycleResultModel> models, bool isNeedSetupParameters = true)
        {
            cycleNameText.text = CycleManager.GetCycleDescriptionByIndex(index);

            foreach (var model in models)
            {
                var a = CreateParameterVisualizer(model.ParameterModel);
                _visualizers.Add(a);
                CreateCountVisualizer(model.ActionCountModel);
            }
            
            LoadCycleScreenshot(index).Forget();

            if (isNeedSetupParameters)
                AwaitParameters().Forget();
        }

        public async UniTask AwaitParameters()
        {
            _visualizers = _visualizers.Where(x => x != null && x.gameObject != null).ToList();
            foreach (var current in _visualizers)
            {
                await current.VisualizeModelData();
            }
        }
        
        public async UniTask LoadCycleScreenshot(int index)
        {
            var filePath = DirectoryPath.GetBraydenScreenPathByIndex(index);
        
            if (File.Exists(filePath))
            {
                try
                {
                    var bytes = await File.ReadAllBytesAsync(filePath);
                    var texture = new Texture2D(GraphSizeX, GraphSizeY);
                    texture.LoadImage(bytes);
                    
                    await UniTask.Yield();
                    
                    texture.Apply();
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    graphImage.sprite = sprite;

                    // await UniTask.WaitWhile(() => graphScreenPanel.rect.size.y <= 0f);
                    //
                    // var containerSize = graphScreenPanel.rect.size;
                    // var coef = containerSize.y / containerSize.x;
                    // var height = containerSize.y * 0.8f;
                    // var width = height / coef;

                    //graphImage.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Exception when load screenshot: {ex}");
                }
            }
            else
            {
                Debug.LogError("Screenshot file not found.");
            }
        }
        
        private CycleParameterVisualizer CreateParameterVisualizer(CycleParameterModel model)
        {
            if (model == null)
                return null;
            
            var visualizer = Instantiate(parameterVisualizer, parametersContainer, false);
            visualizer.Setup(model);
            return visualizer;
        }

        private void CreateCountVisualizer(CycleActionCountModel model)
        {
            if (model == null)
                return;
            
            var visualizer = Instantiate(countVisualizer, parametersContainer, false);
            visualizer.Setup(model);
        }

    }
}