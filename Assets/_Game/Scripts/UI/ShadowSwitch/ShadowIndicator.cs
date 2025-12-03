using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Handles the visual UI indicator for a Shadow (ghost) in the HUD.
/// Can show active/inactive states, availability, and color changes.
/// </summary>
public class ShadowIndicator : MonoBehaviour
{
    #region Properties
    [SerializeField] private Image icon;

    private Tween scaleTween;
    private Tween colorTween;
    private Tween fadeTween;

    private Color baseColor;
    private float disabledAlpha = 0.1f;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (icon == null)
        {
            Debug.LogError("[ShadowIndicator] Brak referencji do Image!");
            return;
        }

        baseColor = icon.color;
    }
    #endregion

    #region Apperence
    /// <summary>
    /// Changes the base color of the icon.
    /// </summary>
    public void SetColor(Color color)
    {
        baseColor = color;
        icon.color = color;
    }

    /// <summary>
    /// Sets the active state of the shadow.
    /// Active shadows pulse in scale and color.
    /// </summary>
    public void SetActive(bool isActive)
    {
        if (icon == null)
        {
            Debug.LogError("[ShadowIndicator] Image null!");
            return;
        }

        scaleTween?.Kill();
        colorTween?.Kill();

        icon.transform.localScale = Vector3.one;
        icon.color = baseColor;

        if (isActive)
        {
            scaleTween = icon.transform
                .DOScale(1.1f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);


            Color brighter = Color.Lerp(baseColor, Color.white, 0.6f);
            brighter.a = 1f;

            colorTween = icon
                .DOColor(brighter, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    /// <summary>
    /// Sets whether this shadow is available (e.g., can be switched to).
    /// Fades icon in/out accordingly.
    /// </summary>
    public void SetAvailable(bool available)
    {
        KillAll();

        if (available)
        {
            icon.DOFade(1f, 0.6f)
                .SetEase(Ease.InOutSine);
        }
        else
        {
            icon.DOFade(disabledAlpha, 0.6f)
                .SetEase(Ease.InOutSine);
        }
    }

    /// <summary>
    /// Stops all running tweens to prevent overlapping animations.
    /// </summary>
    private void KillAll()
    {
        scaleTween?.Kill();
        colorTween?.Kill();
        fadeTween?.Kill();
    }

    #endregion
}
