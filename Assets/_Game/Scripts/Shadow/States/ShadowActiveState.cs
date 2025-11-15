using UnityEngine;

public class ShadowActiveState : IShadowState
{
    public void Enter(ShadowController shadow)
    {
        shadow.SetColor(Color.blue);
    }

    public void Update(ShadowController shadow)
    {

    }

    public void Exit(ShadowController shadow)
    {

    }
}