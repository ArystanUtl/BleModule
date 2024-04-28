using System.Collections.Generic;
using System.Linq;
using CodeBase.Settings;
using CodeBase.Settings.Visualizers;
using Cysharp.Threading.Tasks;
using Modules.Books;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class BraydenSettingsViewController : MonoBehaviour
{
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private Transform container;
    [SerializeField] private BraydenSettingsVisualizer visualizerPrefab;
    [SerializeField] private Button saveButton;

    [Inject] private BookDatabase _bookDatabase;
    
    private List<BraydenSettingsVisualizer> _currentVisualizers = new();

    private BraydenGlobalSettingsManager _settings;
    private void Awake()
    {
        saveButton.onClick.AddListener(SaveSettings);
    }

    public void Init(BraydenGlobalSettingsManager settingsManager)
    {
        _settings = settingsManager;
    }
    
    private void SaveSettings()
    {
        foreach (var visualizer in _currentVisualizers)
            visualizer.SaveSettings();
        
        SetPanelVisibleStatus(false);
    }
    
   
    public void SetPanelVisibleStatus(bool status)
    {
        if (status)
        {
            ResetCurrentVisualizers();
            GenerateVisualizers().Forget();
        }

        rootPanel.SetActive(status);
    }

    private void LoadCurrentValues()
    {
        foreach (var current in _currentVisualizers)
            current.ShowCurrentSettings();
    }

    private void ResetCurrentVisualizers()
    {
        foreach (var current in _currentVisualizers)
            Destroy(current.gameObject);

        _currentVisualizers = new List<BraydenSettingsVisualizer>();
    }

    private async UniTask GenerateVisualizers()
    {
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        
        var currentSettings = _bookDatabase.BraydenBook.BraydenSettingParametersByID.Select(x => x.Value);
        foreach (var settingsModel in currentSettings)
        {
            if (settingsModel.IsBlocked)
                continue;
            
            var visualizer = Instantiate(visualizerPrefab, container, false);
            visualizer.Init(_settings);
            visualizer.SetupParameter(settingsModel);
            _currentVisualizers.Add(visualizer);
        }
        LoadCurrentValues();
    }
}
