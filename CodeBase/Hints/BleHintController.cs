using System;
using Modules.Books;
using UnityEngine;
using UnityEngine.Android;

namespace CodeBase.Hints
{
    [RequireComponent(typeof(BleHintsView))]
    public class BleHintController : MonoBehaviour
    {
        private const string DONT_REPEAT_HINT_FLAG = "Ble_Hint_Repeat_Status";
        
        [SerializeField] private BleHintsView view;
        
        private int _currentHintIndex;
        
        private static readonly int[] HintKeys =
        {
            487,
            488,
            489
        };

        private Action _onSuccessAction;
        
        public void ShowHintPanel(Action onSuccess)
        {
            _currentHintIndex = 0;
            _onSuccessAction = onSuccess;
            ShowHint();
            view.SetVisibleStatus(true);
        }

        private void ClosePanel()
        {
            view.SetVisibleStatus(false);
            _currentHintIndex = 0;
        }

        private void ShowHint()
        {
#if  UNITY_ANDROID
               if (_currentHintIndex == HintKeys.Length - 1)
                Permission.RequestUserPermissions(BleConstants.AndroidPermissions);
#endif
            if (_currentHintIndex >= HintKeys.Length)
            {
                var repeatStatus = view.GetRepeatToggleStatus()
                    ? 0
                    : 1;
                
                PlayerPrefs.SetInt(DONT_REPEAT_HINT_FLAG, repeatStatus);
                ClosePanel();
                _onSuccessAction?.Invoke();
                return;
            }

            var currentHint = TextData.Get(HintKeys[_currentHintIndex]);

            view.Init(currentHint, ClosePanel, ShowHint);
            _currentHintIndex++;
        }

        public static bool IsNeedShowHintPanel()
        {
            var repeatStatus = PlayerPrefs.GetInt(DONT_REPEAT_HINT_FLAG, 1);
            return repeatStatus == 1;
        }
    }
}