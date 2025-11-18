using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class ShadowActiveState : IShadowState
{
    ShadowState IShadowState.State => ShadowState.Active;

    private bool _isBlinking = false;

    public void Enter(ShadowController shadow)
    {
        //Debug.Log("Enter ActiveState: " + shadow.Type);
        shadow.Movement.Stop();

        var appearance = shadow.GetComponent<ShadowAppearanceManager>();
        appearance.SetActive();
    }

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


    public void Exit(ShadowController shadow)
    {

    }
}