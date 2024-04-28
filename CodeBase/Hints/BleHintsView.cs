using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CodeBase.Hints
{
    public class BleHintsView : MonoBehaviour
    {
        [SerializeField] private GameObject rootObject;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Toggle dontRepeatToggle;    
        
        private Action _onCancelButtonClicked;
        private Action _onNextButtonClicked;
        
        private void Start()
        {
            cancelButton.onClick.AddListener(() => _onCancelButtonClicked?.Invoke());
            nextButton.onClick.AddListener(() => _onNextButtonClicked?.Invoke());
        }

        public void SetVisibleStatus(bool status)
        {
            rootObject.SetActive(status);
        }

        public void Init(string hint, Action cancelAction, Action nextAction)
        {
            hintText.text = hint;
            
            _onCancelButtonClicked = cancelAction;
            _onNextButtonClicked = nextAction;
        }

        public bool GetRepeatToggleStatus()
        {
            return dontRepeatToggle.isOn;
        }
    }
}