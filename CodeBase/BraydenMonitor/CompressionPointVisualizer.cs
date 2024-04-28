using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

public class CompressionPointVisualizer : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image backgroundImage;
    
    public static CompressionPointVisualizer PrevPoint;
    
    private Color _defaultColor;
    
    private void Start()
    {
        _defaultColor = backgroundImage.color;
    }

    public void SetPosition(Vector2 position)
    {
        rectTransform.anchoredPosition = position;
    }

    public float GetHeight()
    {
        return rectTransform.sizeDelta.y;
    }

    private void OnEnable()
    {
        if (PrevPoint != null && PrevPoint.gameObject != null)
        {
            PrevPoint.backgroundImage.enabled = false;
        }

        PrevPoint = this;
        
        DeactivateBackgroundImage(1.25f);
    }

    public void DeactivatePointAnimation()
    {
        StopAllCoroutines();
        _tween?.Kill();
        backgroundImage.color = new Color(_defaultColor.r, _defaultColor.g, _defaultColor.b, 0f);
    }

    private TweenerCore<Color, Color, ColorOptions> _tween;
    
    private void DeactivateBackgroundImage(float duration)
    {
        var fadedColor = new Color(_defaultColor.r, _defaultColor.g, _defaultColor.b, 0f);
        _tween = backgroundImage.DOColor(fadedColor, duration);
    }
}
