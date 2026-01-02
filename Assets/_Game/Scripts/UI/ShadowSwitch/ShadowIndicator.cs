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

    private GameObject _linkedShadow;
    public GameObject LinkedShadow
    {
        get => _linkedShadow;       
        set => _linkedShadow = value;
    }

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

    public Color GetColor()
    {
        return icon.color;
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
