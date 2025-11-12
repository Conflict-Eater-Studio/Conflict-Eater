using UnityEngine;

public interface IShadowState
{
    void Enter(ShadowController shadow);
    void Update(ShadowController shadow);
    void Exit(ShadowController shadow);
}