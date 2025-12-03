using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

/// <summary>
/// Represents the active state of a shadow/ghost, where it is controllable by the player.
/// Handles appearance, glow, and transitions to frightened state if needed.
/// </summary>
public class ShadowActiveState : IShadowState
{
    ShadowState IShadowState.State => ShadowState.Active;

    private bool _isBlinking = false;

    /// <summary>
    /// Called when the shadow enters the active state.
    /// Stops movement and updates appearance to active with glow.
    /// </summary>
    public void Enter(ShadowController shadow)
    {
        shadow.Movement.Stop();

        var appearance = shadow.GetComponent<ShadowAppearanceManager>();
        appearance.SetActive();
        appearance.SetGlow(true);
    }

    /// <summary>
    /// Called every frame while in the active state.
    /// Handles transitions to frightened appearance if the game is in frightened mode.
    /// </summary>
    public void Update(ShadowController shadow)
    {
        var appearance = shadow.GetComponent<ShadowAppearanceManager>();

        if (GameManager.Instance.IsFrightenedShadowState)
        {
            if (!_isBlinking || appearance.CurrentState != ShadowAppearanceManager.VisualState.Frightened)
            {
                _isBlinking = true;
                appearance.SetFrightened(blinking: true, isActive: true);
            }
        }
        else
        {
            if (appearance.CurrentState != ShadowAppearanceManager.VisualState.Normal)
            { 
                _isBlinking = false;
                appearance.SetActive();
            }
        }
    }

    /// <summary>
    /// Called when exiting the active state.
    /// Disables glow.
    /// </summary>
    public void Exit(ShadowController shadow)
    {
        var appearance = shadow.GetComponent<ShadowAppearanceManager>();
        appearance.SetGlow(false);
    }
}