using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Loading screen overlay that covers the entire screen during scene transitions.
/// Uses DOTween for smooth fade in/out and loading bar animations.
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private CanvasGroup _canvasGroup;

    [SerializeField]
    private Slider _loadingBar;

    [SerializeField]
    private Image _loadingBarFill;

    [Header("Animation Settings")]
    [SerializeField]
    private float _fadeInDuration = 0.3f;

    [SerializeField]
    private float _fadeOutDuration = 0.5f;

    [SerializeField]
    private Ease _fadeInEase = Ease.OutCubic;

    [SerializeField]
    private Ease _fadeOutEase = Ease.InCubic;

    [SerializeField]
    private float _loadingBarSpeed = 1f;

    [SerializeField]
    private Ease _loadingBarEase = Ease.Linear;

    private Tween _fadeTween;
    private Tween _loadingBarTween;

    private void Awake()
    {
        // Start hidden
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        if (_loadingBar != null)
        {
            _loadingBar.value = 0f;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Shows the loading screen with a fade in animation.
    /// </summary>
    /// <param name="onComplete">Callback when fade in completes.</param>
    public void Show(Action onComplete = null)
    {
        gameObject.SetActive(true);

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = true;
        }

        if (_loadingBar != null)
        {
            _loadingBar.value = 0f;
        }

        // Kill any existing tweens
        _fadeTween?.Kill();
        _loadingBarTween?.Kill();

        // Fade in
        if (_canvasGroup != null)
        {
            _fadeTween = _canvasGroup
                .DOFade(1f, _fadeInDuration)
                .SetEase(_fadeInEase)
                .OnComplete(() => onComplete?.Invoke());
        }
        else
        {
            onComplete?.Invoke();
        }

        // Animate loading bar
        AnimateLoadingBar();
    }

    /// <summary>
    /// Hides the loading screen with a fade out animation.
    /// </summary>
    /// <param name="onComplete">Callback when fade out completes.</param>
    public void Hide(Action onComplete = null)
    {
        // Kill loading bar animation
        _loadingBarTween?.Kill();

        // Complete the loading bar
        if (_loadingBar != null)
        {
            _loadingBar.value = 1f;
        }

        // Fade out
        if (_canvasGroup != null)
        {
            _fadeTween?.Kill();
            _fadeTween = _canvasGroup
                .DOFade(0f, _fadeOutDuration)
                .SetEase(_fadeOutEase)
                .OnComplete(() =>
                {
                    if (_canvasGroup != null)
                    {
                        _canvasGroup.blocksRaycasts = false;
                    }
                    gameObject.SetActive(false);
                    onComplete?.Invoke();
                });
        }
        else
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Updates the loading bar progress manually.
    /// </summary>
    /// <param name="progress">Progress value between 0 and 1.</param>
    public void SetProgress(float progress)
    {
        if (_loadingBar != null)
        {
            _loadingBarTween?.Kill();
            _loadingBar.value = Mathf.Clamp01(progress);
        }
    }

    /// <summary>
    /// Animates the loading bar in a loop to show activity.
    /// </summary>
    private void AnimateLoadingBar()
    {
        if (_loadingBar == null)
            return;

        _loadingBarTween?.Kill();

        // Animate from 0 to 0.9 (we'll complete it to 1.0 when hiding)
        _loadingBarTween = _loadingBar.DOValue(0.9f, _loadingBarSpeed).SetEase(_loadingBarEase);
    }

    private void OnDestroy()
    {
        _fadeTween?.Kill();
        _loadingBarTween?.Kill();
    }
}
