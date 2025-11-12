using UnityEngine;

public class ShadowPatrolState : IShadowState
{
    private Vector2 _aiDirection;
    private float _aiChangeTimer;
    private int _aiMoveCount;
    private float _directionDelay;

    public void Enter(ShadowController shadow)
    {
        _aiMoveCount = 0;
        _directionDelay = shadow.DirectionChangeDelay;
        shadow.SetColor(new Color(0.5f, 0.5f, 1f, 0.5f));

        PickRandomDirection(shadow);
    }

    public void Update(ShadowController shadow)
    {
        _aiChangeTimer -= Time.deltaTime;
        if (_aiChangeTimer <= 0f)
        {
            PickRandomDirection(shadow);
        }

        shadow.Move(_aiDirection);
        shadow.Movement.SpeedMult *= shadow.InactivePenalty;
    }

    public void Exit(ShadowController shadow) { }

    private void PickRandomDirection(ShadowController shadow)
    {
        _aiMoveCount++;

        if (_aiMoveCount == 1)
        {
            _aiDirection = Vector2.up;
        }
        else if (_aiMoveCount == 2)
        {
            float sign = Random.value > 0.5f ? 1f : -1f;
            _aiDirection = new Vector2(sign, 0f);
        }
        else
        {
            int axis = Random.Range(0, 2);
            float sign = Random.value > 0.5f ? 1f : -1f;
            _aiDirection = axis == 0 ? new Vector2(sign, 0f) : new Vector2(0f, sign);
        }

        _aiChangeTimer = _directionDelay;
    }
}
