using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ShadowIndicator : MonoBehaviour
{
    [SerializeField] private Image icon;

    private Tween scaleTween;
    private Tween colorTween;
    private Tween fadeTween;

    private Color baseColor;
    private float disabledAlpha = 0.1f; 

    private void Awake()
    {
        if (icon == null)
        {
            Debug.LogError("[ShadowIndicator] Brak referencji do Image!");
            return;
        }

        baseColor = icon.color;
    }

    public void SetColor(Color color)
    {
        baseColor = color;
        icon.color = color;
    }

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

    private void KillAll()
    {
        scaleTween?.Kill();
        colorTween?.Kill();
        fadeTween?.Kill();
    }
}
