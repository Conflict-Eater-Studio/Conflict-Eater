using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class ShadowActiveState : IShadowState
{
    ShadowState IShadowState.State => ShadowState.Active;

    public void Enter(ShadowController shadow)
    {
        Debug.Log("Enter ActiveState: " + shadow.Type);
        shadow.Movement.Stop();

        var appearance = shadow.GetComponent<ShadowAppearanceManager>();
        appearance.SetActive();
    }

    public void Update(ShadowController shadow)
    {

    }

    public void Exit(ShadowController shadow)
    {

    }
}