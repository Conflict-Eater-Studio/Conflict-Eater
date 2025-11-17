using UnityEngine;
using System.Collections;

public class ShadowAppearanceManager : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer eyesRenderer;

    [Header("Base Colors")]
    public Color blinkyColor = Color.red;
    public Color pinkyColor = new Color(1f, 0.6f, 0.9f);
    public Color inkyColor = Color.cyan;
    public Color clydeColor = new Color(1f, 0.7f, 0.3f);

    [Header("Frightened Look")]
    public Color frightenedColor = new Color(0f, 0f, 0.6f);
    public Color frightenedBlinkColor = Color.white;
    public float blinkInterval = 0.2f;

    private ShadowController controller;
    private Coroutine blinkRoutine;

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
        eyesRenderer.enabled = true;

        bodyRenderer.color = Color.blue;
    }

    public void SetNormal()
    {
        CurrentState = VisualState.Normal;
        StopBlinking();
        bodyRenderer.enabled = true;
        eyesRenderer.enabled = true;
        bodyRenderer.color = GetBaseColor();
    }

    public void SetExitingBase()
    {
        CurrentState = VisualState.ExitingBase;
        StopBlinking();
        bodyRenderer.enabled = true;
        eyesRenderer.enabled = true;
        bodyRenderer.color = GetBaseColor();
    }

    public void SetFrightened(bool blinking = false)
    {
        CurrentState = VisualState.Frightened;
        eyesRenderer.enabled = false;
        bodyRenderer.enabled = true;
        bodyRenderer.color = frightenedColor;

        if (blinking)
            StartBlinking();
        else
            StopBlinking();
    }

    public void SetDead()
    {
        CurrentState = VisualState.Dead;
        StopBlinking();
        bodyRenderer.enabled = false;
        eyesRenderer.enabled = true;
    }

    private void StartBlinking()
    {
        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);
        blinkRoutine = StartCoroutine(BlinkRoutine());
    }

    private void StopBlinking()
    {
        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);
        blinkRoutine = null;
    }

    private IEnumerator BlinkRoutine()
    {
        bool toggle = false;
        while (true)
        {
            bodyRenderer.color = toggle ? frightenedColor : frightenedBlinkColor;
            toggle = !toggle;
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    private Color GetBaseColor()
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
}
