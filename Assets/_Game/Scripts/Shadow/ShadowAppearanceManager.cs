using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Manages visual appearance of a shadow/ghost character in Unity.
/// Handles states such as Normal, Frightened, Dead, and ExitingBase.
/// Includes blinking logic and color management.
/// </summary>
public class ShadowAppearanceManager : MonoBehaviour
{
    #region Inspector Fields
    [Header("Renderers")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer skullRenderer;
    [SerializeField] private SpriteRenderer faceRenderer;

    [Header("Base Colors")]
    public Color blinkyColor = Color.red;
    public Color inkyColor = Color.cyan;
    public Color clydeColor = new Color(1f, 0.7f, 0.3f);

    [Header("Active Colors")]
    [SerializeField] private GameObject _glow;

    [Header("Frightened Look")]
    public Color frightenedBlinkColor = Color.white;
    public float blinkInterval = 0.2f;

    #endregion

    #region Properties
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

    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        controller = GetComponent<ShadowController>();
    }
    #endregion

    #region Appearance States

    /// <summary>
    /// Sets the ghost to the normal active state and applies the active color.
    /// </summary>
    public void SetActive()
    {
        CurrentState = VisualState.Normal;
        StopBlinking();
        bodyRenderer.enabled = true;
        skullRenderer.enabled = true;
        faceRenderer.enabled = false;
    }

    /// <summary>
    /// Applies the base color while the ghost is exiting its home/base.
    /// </summary>
    public void SetExitingBase()
    {
        CurrentState = VisualState.ExitingBase;
        StopBlinking();
        bodyRenderer.enabled = true;
        skullRenderer.enabled = true;
        faceRenderer.enabled = false;
        bodyRenderer.color = GetBaseColor();
    }

    /// <summary>
    /// Sets the ghost into the frightened state, with optional blinking and active-mode coloring.
    /// </summary>
    public void SetFrightened(bool blinking = false, bool isActive = false)
    {
        CurrentState = VisualState.Frightened;
        bodyRenderer.enabled = true;
        skullRenderer.enabled = true;
        faceRenderer.enabled = false;

        bodyRenderer.color = colorBeforeFrightened;

        if (blinking)
            StartBlinking(isActive);
        else
            StopBlinking();
    }

    /// <summary>
    /// Restores the color the ghost had before entering the frightened state.
    /// </summary>
    public void SetColorBeforeFrightened()
    {
        bodyRenderer.color = colorBeforeFrightened;
    }

    /// <summary>
    /// Restores the ghost appearance after being eaten.
    /// </summary>
    public void SetColorAfterEaten()
    {
        CurrentState = VisualState.Normal;
        StopBlinking();
        bodyRenderer.enabled = true;
        skullRenderer.enabled = true;
        faceRenderer.enabled = false;
    }

    /// <summary>
    /// Applies the dead state, hiding the body sprite.
    /// </summary>
    public void SetDead()
    {
        CurrentState = VisualState.Dead;
        StopBlinking();
        bodyRenderer.enabled = false;
        skullRenderer.enabled = false;
        faceRenderer.enabled = true;
    }
    #endregion

    #region Color Helpers
    /// <summary>
    /// Manually sets a custom color and resets to the normal state.
    /// </summary>
    public void SetCustomColor(Color color)
    {
        StopBlinking();
        CurrentState = VisualState.Normal;
        bodyRenderer.enabled = true;
        bodyRenderer.color = color;
    }

    /// <summary>
    /// Returns the current color of the body renderer.
    /// </summary>
    public Color GetCurrentColor()
    {
        return bodyRenderer != null ? bodyRenderer.color : Color.white;
    }

    /// <summary>
    /// Returns the color associated with the ghost's type.
    /// </summary>
    public Color GetBaseColor()
    {
        return controller.Type switch
        {
            ShadowType.Blinky => blinkyColor,
            ShadowType.Inky => inkyColor,
            ShadowType.Clyde => clydeColor,
            _ => Color.white
        };
    }

    #endregion

    #region Glow Effect
    /// <summary>
    /// Enables or disables the glow effect.
    /// </summary>
    public void SetGlow(bool glow)
    {
        _glow.SetActive(glow);
    }
    #endregion

    #region Blinking Logic
    /// <summary>
    /// Starts the blinking coroutine for the frightened state.
    /// </summary>
    private void StartBlinking(bool isActive)
    {
        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);
        blinkRoutine = StartCoroutine(BlinkRoutine(isActive));
    }

    /// <summary>
    /// Stops the blinking coroutine if running.
    /// </summary>
    private void StopBlinking()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            skullRenderer.color = Color.white;
        }
        blinkRoutine = null;
    }

    /// <summary>
    /// Coroutine that alternates colors to create a blinking effect.
    /// </summary>
    private IEnumerator BlinkRoutine(bool isActive = false)
    {
        bool toggle = false;
        while (true)
        {
            skullRenderer.color = toggle ? colorBeforeFrightened : frightenedBlinkColor;
            toggle = !toggle;
            yield return new WaitForSeconds(blinkInterval);
        }
    }
    #endregion
}
