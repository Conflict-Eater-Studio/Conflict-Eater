using TMPro;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Movement processing with hysteresis and dual-thresholds.
/// </summary>
public class Movement
{
    private enum SnapAxis
    {
        X, Y
    }

    private readonly Rigidbody2D _rb;
    private readonly Transform _transformRef;
    private readonly Grid _grid;

    private float _speed;
    private float _speedMult = 1f;
    public float SpeedMult
    {
        get => _speedMult;
        set => _speedMult = value;
    }
    private float _centerThreshold;
    private float _snapSpeedMultiplier;

    private Vector2 _moveDirection = Vector2.zero;
    private bool isDirectionLocked = false;

    // Minimum stick magnitude to initially activate or switch direction
    private float activationThreshold = 0.65f;
    // Stick must drop below this to consider it 'released' (but keeps moving)
    private float deactivationThreshold = 0.2f;
    // Higher threshold required to switch to a different direction
    private float switchDirectionThreshold = 0.8f;

    private Vector2 _queuedDirection = Vector2.zero;

    private bool _isSnapping = false;
    private SnapAxis _snapAxis = SnapAxis.X;
    private float _snapTarget = 0f;

    public Movement(Rigidbody2D rb, Transform transformRef, Grid grid, float speed, float centerThreshold = 0.06f, float snapSpeedMultiplier = 1.5f)
    {
        _rb = rb;
        _transformRef = transformRef;
        _grid = grid;
        _speed = speed;
        _centerThreshold = centerThreshold;
        _snapSpeedMultiplier = snapSpeedMultiplier;
    }

    // Getters
    public Vector2 MoveDirection => _moveDirection;
    public Vector2 QueuedDirection => _queuedDirection;

    /// <summary>
    /// Movement processing with hysteresis and dual-thresholds.
    /// </summary>
    public void OnMove(Vector2 stick)
    {
        float absX = Mathf.Abs(stick.x);
        float absY = Mathf.Abs(stick.y);
        float maxAxis = Mathf.Max(absX, absY);

        if (isDirectionLocked && maxAxis < deactivationThreshold)
        {
            isDirectionLocked = false;
            _queuedDirection = Vector2.zero;
            return;
        }

        if (maxAxis < activationThreshold)
        {
            _queuedDirection = Vector2.zero;
            return;
        }

        Vector2 newDirection;

        if (absX > absY)
        {
            newDirection = new Vector2(stick.x > 0 ? 1 : -1, 0);
        }
        else
        {
            newDirection = new Vector2(0, stick.y > 0 ? 1 : -1);
        }

        if (!isDirectionLocked)
        {
            _queuedDirection = newDirection;
            isDirectionLocked = true;
        }
        else
        {
            if (newDirection != _moveDirection)
            {
                if (maxAxis >= switchDirectionThreshold)
                {
                    _queuedDirection = newDirection;
                }
            }
        }
    }

    /// <summary>
    /// Call from FixedUpdate to advance movement. This will call Rigidbody2D.MovePosition.
    /// </summary>
    public void FixedTick()
    {
        Vector2 currentPos = _rb.position;
        Vector2 baseMovement = _moveDirection * _speed * _speedMult * Time.fixedDeltaTime;
        Vector3Int curCell = default;

        Vector3Int queuedNeighbor = Vector3Int.zero;
        if (_queuedDirection != Vector2.zero)
        {
            curCell = Grid.WorldToCell(currentPos);
            queuedNeighbor = curCell + new Vector3Int((int)_queuedDirection.x, (int)_queuedDirection.y, 0);
        }

        // Helper
        bool WillCrossMiddleInTwoTicks()
        {
            if (_moveDirection == Vector2.zero) return false;

            if (Mathf.Abs(_moveDirection.x) > 0f)
            {
                float frac = currentPos.x - Mathf.Floor(currentPos.x);
                float projectedPos = currentPos.x + _moveDirection.x * _speed * _speedMult * Time.fixedDeltaTime * 2f;
                float projectedFrac = projectedPos - Mathf.Floor(projectedPos);

                if ((_moveDirection.x > 0f && frac < 0.5f && projectedFrac >= 0.5f)
                    || (_moveDirection.x < 0f && frac > 0.5f && projectedFrac <= 0.5f))
                    return true;
            }
            else if (Mathf.Abs(_moveDirection.y) > 0f)
            {
                float frac = currentPos.y - Mathf.Floor(currentPos.y);
                float projectedPos = currentPos.y + _moveDirection.y * _speed * _speedMult * Time.fixedDeltaTime * 2f;
                float projectedFrac = projectedPos - Mathf.Floor(projectedPos);

                if ((_moveDirection.y > 0f && frac < 0.5f && projectedFrac >= 0.5f)
                    || (_moveDirection.y < 0f && frac > 0.5f && projectedFrac <= 0.5f))
                    return true;
            }

            return false;
        }

        bool canPreSwitch = _moveDirection != _queuedDirection
                            && _queuedDirection != Vector2.zero
                            && _grid.IsWalkable(queuedNeighbor)
                            && (_moveDirection == Vector2.zero || WillCrossMiddleInTwoTicks());

        if (canPreSwitch)
        {
            _moveDirection = _queuedDirection;
        }

        bool wallAhead = false;
        if (_moveDirection != Vector2.zero)
        {
            curCell = Grid.WorldToCell(_rb.position);
            Vector3Int neighbor = curCell + new Vector3Int((int)_moveDirection.x, (int)_moveDirection.y, 0);
            if (!_grid.IsWalkable(neighbor))
            {
                wallAhead = true;
            }
        }

        if (_queuedDirection != Vector2.zero && _moveDirection != Vector2.zero)
        {
            bool centered = false;
            if (Mathf.Abs(_moveDirection.x) > 0f)
            {
                float frac = currentPos.x - Mathf.Floor(currentPos.x);
                centered = Mathf.Abs(frac - 0.5f) <= _centerThreshold;
            }
            else if (Mathf.Abs(_moveDirection.y) > 0f)
            {
                float frac = currentPos.y - Mathf.Floor(currentPos.y);
                centered = Mathf.Abs(frac - 0.5f) <= _centerThreshold;
            }

            if (centered)
            {
                Vector3Int nextCell = Grid.WorldToCell(_rb.position + _queuedDirection);

                if (_grid.IsWalkable(nextCell))
                {
                    bool sameAxis = (Mathf.Abs(_moveDirection.x) > 0f && Mathf.Abs(_queuedDirection.x) > 0f) || (Mathf.Abs(_moveDirection.y) > 0f && Mathf.Abs(_queuedDirection.y) > 0f);

                    if ((!sameAxis) || wallAhead)
                    {
                        if (Mathf.Abs(_queuedDirection.x) > 0f)
                        {
                            _snapAxis = SnapAxis.Y;
                            _snapTarget = Mathf.Floor(currentPos.y) + 0.5f;
                        }
                        else
                        {
                            _snapAxis = SnapAxis.X;
                            _snapTarget = Mathf.Floor(currentPos.x) + 0.5f;
                        }

                        _isSnapping = true;
                        _moveDirection = _queuedDirection;
                        _queuedDirection = Vector2.zero;
                    }
                }
            }
        }

        Vector2 nextPos = currentPos + baseMovement;

        if (_isSnapping)
        {
            float snapSpeed = _speed * _snapSpeedMultiplier * Time.fixedDeltaTime;
            if (_snapAxis == SnapAxis.X)
            {
                nextPos.x = Mathf.MoveTowards(currentPos.x, _snapTarget, snapSpeed);
            }
            else
            {
                nextPos.y = Mathf.MoveTowards(currentPos.y, _snapTarget, snapSpeed);
            }

            if (Mathf.Abs((_snapAxis == SnapAxis.X ? nextPos.x : nextPos.y) - _snapTarget) <= 0.001f)
            {
                _isSnapping = false;
            }
        }

        _rb.MovePosition(nextPos);
    }

    public void SetSpeed(float newSpeed) => _speed = newSpeed;

    public void Stop()
    {
        _moveDirection = Vector2.zero;
        _queuedDirection = Vector2.zero;
        _isSnapping = false;
    }
}
