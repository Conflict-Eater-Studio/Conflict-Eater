using UnityEngine;
using System.Collections;

public class ShadowAppearanceManager : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private SpriteRenderer bodyRenderer;

    [Header("Base Colors")]
    public Color blinkyColor = Color.red;
    public Color pinkyColor = new Color(1f, 0.6f, 0.9f);
    public Color inkyColor = Color.cyan;
    public Color clydeColor = new Color(1f, 0.7f, 0.3f);

    [Header("Active Colors")]
    public Color activeColor = Color.red;

    [Header("Frightened Look")]
    public Color frightenedNormalColor = new Color(0f, 0f, 0.6f);
    public Color frightenedActiveColor = new Color(0f, 0f, 0.6f);
    public Color frightenedBlinkColor = Color.white;
    public float blinkInterval = 0.2f;

    [Header("Next Active Shadow Marker")]
    [SerializeField] private GameObject _nextShadowMarker;

    [Header("Eater Shadow")]
    [SerializeField] private Collider2D _collider;

    private ShadowController controller;
    private Coroutine blinkRoutine;

    public Color colorBeforeFrightened = Color.white;

    public enum VisualState
    {
        Normal,
        Frightened,
        Dead,
        ExitingBase
    }

    public VisualState CurrentState { get; private set; }

    private void Awake()
    {
        controller = GetComponent<ShadowController>();
    }

    public void SetActive()
    {
        CurrentState = VisualState.Normal;
        StopBlinking();
        bodyRenderer.enabled = true;

        bodyRenderer.color = activeColor;
    }

    public void SetNormal()
    {
        CurrentState = VisualState.Normal;
        StopBlinking();
        bodyRenderer.enabled = true;
        _collider.enabled = true;
        bodyRenderer.color = GetBaseColor();
    }

    public void SetExitingBase()
    {
        CurrentState = VisualState.ExitingBase;
        StopBlinking();
        bodyRenderer.enabled = true;
        bodyRenderer.color = GetBaseColor();
    }

    public void SetFrightened(bool blinking = false, bool isActive = false)
    {
        CurrentState = VisualState.Frightened;
        bodyRenderer.enabled = true;

        bodyRenderer.color = isActive ? frightenedActiveColor : colorBeforeFrightened;

        if (blinking)
            StartBlinking(isActive);
        else
            StopBlinking();
    }

    public void SetColorBeforeFrightened()
    {
        bodyRenderer.color = colorBeforeFrightened;
    }

    public void SetColorAfterEaten()
    {
        CurrentState = VisualState.Normal;
        StopBlinking();
        bodyRenderer.enabled = true;
        _collider.enabled = true;
        //bodyRenderer.color = colorBeforeFrightened;
    }

    public void SetDead()
    {
        CurrentState = VisualState.Dead;
        StopBlinking();
        bodyRenderer.enabled = false;
        _collider.enabled = false;
    }

    public void ActiveNextShadowMarker(bool active)
    {
        if (_nextShadowMarker != null)
            _nextShadowMarker.SetActive(active);
    }

    private void StartBlinking(bool isActive)
    {
        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);
        blinkRoutine = StartCoroutine(BlinkRoutine(isActive));
    }


    private void StopBlinking()
    {
        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);
        blinkRoutine = null;
    }

    private IEnumerator BlinkRoutine(bool isActive = false)
    {
        bool toggle = false;
        while (true)
        {
            bodyRenderer.color = toggle ? (isActive ? frightenedActiveColor : colorBeforeFrightened) : frightenedBlinkColor;
            toggle = !toggle;
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    public Color GetBaseColor()
    {
        return controller.Type switch
        {
            ShadowType.Blinky => blinkyColor,
            ShadowType.Pinky => pinkyColor,
            ShadowType.Inky => inkyColor,
            ShadowType.Clyde => clydeColor,
            _ => Color.white
        };
    }

    public void SetCustomColor(Color color)
    {
        StopBlinking();
        CurrentState = VisualState.Normal;
        bodyRenderer.enabled = true;
        _collider.enabled = true;
        bodyRenderer.color = color;
    }
    public Color GetCurrentColor()
    {
        return bodyRenderer != null ? bodyRenderer.color : Color.white;
    }
}
