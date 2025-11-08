using System;
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
        X,
        Y,
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

    // Stores the current controller stick input state
    private Vector2 _currentStickInput = Vector2.zero;

    private Vector2 _queuedDirection = Vector2.zero;

    private bool _isSnapping = false;
    private SnapAxis _snapAxis = SnapAxis.X;
    private float _snapTarget = 0f;

    public Movement(
        Rigidbody2D rb,
        Transform transformRef,
        Grid grid,
        float speed,
        float centerThreshold = 0.06f,
        float snapSpeedMultiplier = 1.5f
    )
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
        // Store the current stick input
        _currentStickInput = stick;

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

        // Try to switch direction if queued
        TryHandleQueuedDirection(currentPos);

        // Check for wall collision
        bool wallAhead = CheckWallAhead();

        // Calculate next position
        Vector2 nextPos = CalculateNextPosition(currentPos, wallAhead);

        // Apply movement
        _rb.MovePosition(nextPos);
    }

    /// <summary>
    /// Attempts to switch to queued direction if conditions are met.
    /// If no queued direction but stick is held, re-evaluate the stick input.
    /// </summary>
    private void TryHandleQueuedDirection(Vector2 currentPos)
    {
        if (_queuedDirection == Vector2.zero && _moveDirection == Vector2.zero)
        {
            ReEvaluateStickInput();
        }

        if (_queuedDirection == Vector2.zero || _queuedDirection == _moveDirection)
            return;

        Vector3Int curCell = Grid.WorldToCell(currentPos);
        Vector3Int queuedNeighbor =
            curCell + new Vector3Int((int)_queuedDirection.x, (int)_queuedDirection.y, 0);

        if (!_grid.IsWalkable(queuedNeighbor))
        {
            _queuedDirection = Vector2.zero;

            if (_moveDirection == Vector2.zero)
            {
                ReEvaluateStickInput();
            }
            return;
        }

        if (_moveDirection == Vector2.zero || WillCrossMiddleInTwoTicks(currentPos))
        {
            _moveDirection = _queuedDirection;
            return;
        }

        if (IsCenteredOnCurrentAxis(currentPos) || WillCrossCenterThisTick(currentPos))
        {
            InitiateDirectionChange(currentPos);
        }
    }

    /// <summary>
    /// Re-evaluates the current stick input to generate a queued direction.
    /// Used when _queuedDirection was cleared but stick is still held.
    /// </summary>
    private void ReEvaluateStickInput()
    {
        float absX = Mathf.Abs(_currentStickInput.x);
        float absY = Mathf.Abs(_currentStickInput.y);
        float maxAxis = Mathf.Max(absX, absY);

        // Only re-evaluate if stick is above activation threshold
        if (maxAxis < activationThreshold)
            return;

        Vector2 newDirection;

        if (absX > absY)
        {
            newDirection = new Vector2(_currentStickInput.x > 0 ? 1 : -1, 0);
        }
        else
        {
            newDirection = new Vector2(0, _currentStickInput.y > 0 ? 1 : -1);
        }

        _queuedDirection = newDirection;
    }

    /// <summary>
    /// Checks if moving in current direction will hit a wall.
    /// </summary>
    private bool CheckWallAhead()
    {
        if (_moveDirection == Vector2.zero)
            return false;

        Vector3Int curCell = Grid.WorldToCell(_rb.position);
        Vector3Int neighbor =
            curCell + new Vector3Int((int)_moveDirection.x, (int)_moveDirection.y, 0);

        return !_grid.IsWalkable(neighbor);
    }

    /// <summary>
    /// Calculates the next position based on current movement and snapping state.
    /// </summary>
    private Vector2 CalculateNextPosition(Vector2 currentPos, bool wallAhead)
    {
        Vector2 nextPos = currentPos;

        if (wallAhead)
        {
            nextPos = StopAtCellCenter(currentPos);
        }
        else if (_moveDirection != Vector2.zero)
        {
            nextPos += _moveDirection * _speed * _speedMult * Time.fixedDeltaTime;
        }

        // ALWAYS enforce axis locking
        nextPos = EnforceAxisLocking(nextPos);

        return nextPos;
    }

    /// <summary>
    /// Enforces that the player is always centered on the axis perpendicular to movement.
    /// If moving horizontally (X), Y must be at cell center (#.5f).
    /// If moving vertically (Y), X must be at cell center (#.5f).
    /// </summary>
    private Vector2 EnforceAxisLocking(Vector2 position)
    {
        if (_moveDirection == Vector2.zero)
            return position;

        Vector2 lockedPos = position;

        if (Mathf.Abs(_moveDirection.x) > 0f)
        {
            // Moving horizontally, lock Y to center
            float targetY = Mathf.Floor(position.y) + 0.5f;

            if (_isSnapping && _snapAxis == SnapAxis.Y)
            {
                // Snap smoothly during transition
                float snapSpeed = _speed * _snapSpeedMultiplier * Time.fixedDeltaTime;
                lockedPos.y = Mathf.MoveTowards(position.y, targetY, snapSpeed);

                if (Mathf.Abs(lockedPos.y - targetY) <= 0.001f)
                {
                    lockedPos.y = targetY;
                    _isSnapping = false;
                }
            }
            else
            {
                // Hard lock to center
                lockedPos.y = targetY;
            }
        }
        else if (Mathf.Abs(_moveDirection.y) > 0f)
        {
            // Moving vertically, lock X to center
            float targetX = Mathf.Floor(position.x) + 0.5f;

            if (_isSnapping && _snapAxis == SnapAxis.X)
            {
                // Snap smoothly during transition
                float snapSpeed = _speed * _snapSpeedMultiplier * Time.fixedDeltaTime;
                lockedPos.x = Mathf.MoveTowards(position.x, targetX, snapSpeed);

                if (Mathf.Abs(lockedPos.x - targetX) <= 0.001f)
                {
                    lockedPos.x = targetX;
                    _isSnapping = false;
                }
            }
            else
            {
                // Hard lock to center
                lockedPos.x = targetX;
            }
        }

        return lockedPos;
    }

    /// <summary>
    /// Stops movement at the center of the current cell when hitting a wall.
    /// </summary>
    private Vector2 StopAtCellCenter(Vector2 currentPos)
    {
        Vector3 cellCenter = Grid.GetCellCenterWorld(Grid.WorldToCell(currentPos));
        Vector2 targetCenter = new Vector2(cellCenter.x, cellCenter.y);

        // Move towards center
        float moveAmount = _speed * _speedMult * Time.fixedDeltaTime;
        Vector2 nextPos = Vector2.MoveTowards(currentPos, targetCenter, moveAmount);

        // If we're at or very close to center, snap exactly and clear move direction
        if (Vector2.Distance(nextPos, targetCenter) <= 0.001f)
        {
            nextPos = targetCenter;
            _moveDirection = Vector2.zero;
        }

        return nextPos;
    }

    /// <summary>
    /// Initiates a direction change by setting up snapping for the perpendicular axis.
    /// </summary>
    private void InitiateDirectionChange(Vector2 currentPos)
    {
        bool sameAxis = IsSameAxis(_moveDirection, _queuedDirection);

        if (sameAxis)
        {
            _moveDirection = _queuedDirection;
            _queuedDirection = Vector2.zero;
            _isSnapping = false;
        }
        else
        {
            if (Mathf.Abs(_queuedDirection.x) > 0f)
            {
                // New direction is horizontal, snap Y axis
                _snapAxis = SnapAxis.Y;
                _snapTarget = Mathf.Floor(currentPos.y) + 0.5f;
            }
            else
            {
                // New direction is vertical, snap X axis
                _snapAxis = SnapAxis.X;
                _snapTarget = Mathf.Floor(currentPos.x) + 0.5f;
            }

            _isSnapping = true;
            _moveDirection = _queuedDirection;
            _queuedDirection = Vector2.zero;
        }
    }

    /// <summary>
    /// Checks if the player is centered on the axis perpendicular to current movement.
    /// </summary>
    private bool IsCenteredOnCurrentAxis(Vector2 currentPos)
    {
        if (Mathf.Abs(_moveDirection.x) > 0f)
        {
            // Moving horizontally, check Y centering
            float frac = currentPos.y - Mathf.Floor(currentPos.y);
            return Mathf.Abs(frac - 0.5f) <= _centerThreshold;
        }
        else if (Mathf.Abs(_moveDirection.y) > 0f)
        {
            // Moving vertically, check X centering
            float frac = currentPos.x - Mathf.Floor(currentPos.x);
            return Mathf.Abs(frac - 0.5f) <= _centerThreshold;
        }

        return false;
    }

    /// <summary>
    /// Checks if the player will cross the center of their current cell this tick.
    /// </summary>
    private bool WillCrossCenterThisTick(Vector2 currentPos)
    {
        Vector2 baseMovement = _moveDirection * _speed * _speedMult * Time.fixedDeltaTime;
        Vector2 projectedPos = currentPos + baseMovement;

        if (Mathf.Abs(_moveDirection.x) > 0f)
        {
            // Moving horizontally, check Y crossing
            float frac = currentPos.y - Mathf.Floor(currentPos.y);
            float projFrac = projectedPos.y - Mathf.Floor(projectedPos.y);

            // Check if we cross 0.5
            if (
                (_moveDirection.y >= 0f && frac < 0.5f && projFrac >= 0.5f)
                || (_moveDirection.y < 0f && frac > 0.5f && projFrac <= 0.5f)
            )
                return true;
        }
        else if (Mathf.Abs(_moveDirection.y) > 0f)
        {
            // Moving vertically, check X crossing
            float frac = currentPos.x - Mathf.Floor(currentPos.x);
            float projFrac = projectedPos.x - Mathf.Floor(projectedPos.x);

            // Check if we cross 0.5
            if (
                (_moveDirection.x >= 0f && frac < 0.5f && projFrac >= 0.5f)
                || (_moveDirection.x < 0f && frac > 0.5f && projFrac <= 0.5f)
            )
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the player will cross the cell middle in the next two ticks.
    /// Used for pre-emptive direction switching.
    /// </summary>
    private bool WillCrossMiddleInTwoTicks(Vector2 currentPos)
    {
        if (_moveDirection == Vector2.zero)
            return false;

        float doubleMovement = _speed * _speedMult * Time.fixedDeltaTime * 2f;

        if (Mathf.Abs(_moveDirection.x) > 0f)
        {
            float frac = currentPos.x - Mathf.Floor(currentPos.x);
            float projectedPos = currentPos.x + _moveDirection.x * doubleMovement;
            float projectedFrac = projectedPos - Mathf.Floor(projectedPos);

            if (
                (_moveDirection.x > 0f && frac < 0.5f && projectedFrac >= 0.5f)
                || (_moveDirection.x < 0f && frac > 0.5f && projectedFrac <= 0.5f)
            )
                return true;
        }
        else if (Mathf.Abs(_moveDirection.y) > 0f)
        {
            float frac = currentPos.y - Mathf.Floor(currentPos.y);
            float projectedPos = currentPos.y + _moveDirection.y * doubleMovement;
            float projectedFrac = projectedPos - Mathf.Floor(projectedPos);

            if (
                (_moveDirection.y > 0f && frac < 0.5f && projectedFrac >= 0.5f)
                || (_moveDirection.y < 0f && frac > 0.5f && projectedFrac <= 0.5f)
            )
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if two directions are on the same axis.
    /// </summary>
    private bool IsSameAxis(Vector2 dir1, Vector2 dir2)
    {
        return (Mathf.Abs(dir1.x) > 0f && Mathf.Abs(dir2.x) > 0f)
            || (Mathf.Abs(dir1.y) > 0f && Mathf.Abs(dir2.y) > 0f);
    }

    public void SetSpeed(float newSpeed) => _speed = newSpeed;

    public void Stop()
    {
        _moveDirection = Vector2.zero;
        _queuedDirection = Vector2.zero;
        _currentStickInput = Vector2.zero;
        _isSnapping = false;
    }
}
