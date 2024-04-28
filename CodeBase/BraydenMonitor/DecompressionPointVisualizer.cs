using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CodeBase
{
    public class DecompressionPointVisualizer: MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image backgroundImage;

        private Color _defaultColor;
        private static DecompressionPointVisualizer _prevPoint;
    
        private void Start()
        {
            _defaultColor = backgroundImage.color;
        }

        public void SetPosition(Vector2 position)
        {
            rectTransform.anchoredPosition = position;
        }
        
        private void OnEnable()
        {
            if (_prevPoint != null && _prevPoint.gameObject != null)
            {
                _prevPoint.backgroundImage.enabled = false;
            }

            _prevPoint = this;
        
            DeactivateBackgroundImageAsync(1.25f, 0f).Forget();
        }
    
        private async UniTaskVoid DeactivateBackgroundImageAsync(float duration, float delay)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
        
            var fadedColor = new Color(_defaultColor.r, _defaultColor.g, _defaultColor.b, 0f);
            backgroundImage.DOColor(fadedColor, duration);
        }
    }
}