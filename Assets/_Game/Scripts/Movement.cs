using UnityEngine;

/// <summary>
/// Movement processing with hysteresis and dual-thresholds.
/// </summary>
public class Movement
{
    #region Constants and enums
    private const float CELL_CENTER_OFFSET = 0.5f;
    private const float SNAP_COMPLETE_TOLERANCE = 0.000001f;
    private const float WALL_STOP_TOLERANCE = 0.001f;

    private enum SnapAxis
    {
        X,
        Y,
    }
    #endregion

    #region Fields
    private readonly Rigidbody2D _rb;
    private readonly Transform _transformRef;
    private readonly Grid _grid;

    private float _speed;
    private float _speedMult = 1f;
    public float SpeedMult { get; set; } = 1f;
    private float _centerThreshold;
    private float _snapSpeedMultiplier;

    private Vector2 _moveDirection = Vector2.zero;
    private bool _isDirectionLocked = false;

    // Minimum stick magnitude to initially activate or switch direction
    private float _activationThreshold = 0.65f;

    // Stick must drop below this to consider it 'released' (but keeps moving)
    private float _deactivationThreshold = 0.2f;

    // Higher threshold required to switch to a different direction
    private float _switchDirectionThreshold = 0.8f;

    // Stores the current controller stick input state
    private Vector2 _currentStickInput = Vector2.zero;

    private Vector2 _queuedDirection = Vector2.zero;

    private bool _isSnapping = false;
    private SnapAxis _snapAxis = SnapAxis.X;
    private float _snapTarget = 0f;
    #endregion

    #region Construction & Properties
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

    public Vector2 MoveDirection => _moveDirection;
    public Vector2 QueuedDirection => _queuedDirection;
    #endregion

    #region Public API
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

        if (_isDirectionLocked && maxAxis < _deactivationThreshold)
        {
            _isDirectionLocked = false;
            _queuedDirection = Vector2.zero;
            return;
        }

        if (maxAxis < _activationThreshold)
        {
            _queuedDirection = Vector2.zero;
            return;
        }

        Vector2 newDirection = CalculateDirectionFromStick(stick);

        if (!_isDirectionLocked)
        {
            _queuedDirection = newDirection;
            _isDirectionLocked = true;
        }
        else
        {
            if (newDirection != _moveDirection)
            {
                if (maxAxis >= _switchDirectionThreshold)
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
    #endregion

    #region Direction queue handling
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

        // Allow immediate direction change if not moving or will cross center soon
        if (_moveDirection == Vector2.zero || WillCrossMiddleInTwoTicks(currentPos))
        {
            // Check if queued direction is walkable from current cell
            Vector3Int curCell = Grid.WorldToCell(currentPos);
            Vector3Int queuedNeighbor =
                curCell + new Vector3Int((int)_queuedDirection.x, (int)_queuedDirection.y, 0);

            if (!_grid.IsWalkable(queuedNeighbor))
            {
                _queuedDirection = Vector2.zero;

                // Moved into wall, re-evaluate stick input
                if (_moveDirection == Vector2.zero)
                {
                    ReEvaluateStickInput();
                }
                return;
            }

            _moveDirection = _queuedDirection;
            return;
        }

        // Check if we're approaching an intersection where we can turn
        if (IsCenteredOnCurrentAxis(currentPos) || WillCrossCenterThisTick(currentPos))
        {
            // Check if queued direction will be walkable from the intersection cell
            Vector3Int intersectionCell = Grid.WorldToCell(currentPos);
            Vector3Int queuedNeighbor =
                intersectionCell
                + new Vector3Int((int)_queuedDirection.x, (int)_queuedDirection.y, 0);

            if (!_grid.IsWalkable(queuedNeighbor))
            {
                // Can't turn here, clear the queue but keep moving forward
                _queuedDirection = Vector2.zero;
                return;
            }

            InitiateDirectionChange(currentPos);
        }
    }
    #endregion

    #region Input evaluation & direction calculation
    /// <summary>
    /// Re-evaluates the current stick input to generate a queued direction.
    /// Used when _queuedDirection was cleared but stick is still held.
    /// </summary>
    private void ReEvaluateStickInput()
    {
        float maxAxis = Mathf.Max(Mathf.Abs(_currentStickInput.x), Mathf.Abs(_currentStickInput.y));

        // Only re-evaluate if stick is above activation threshold
        if (maxAxis < _activationThreshold)
            return;

        Vector2 newDirection = CalculateDirectionFromStick(_currentStickInput);
        _queuedDirection = newDirection;
        _isDirectionLocked = true;
    }

    /// <summary>
    /// Calculates the primary direction from stick input (cardinal directions only).
    /// </summary>
    private Vector2 CalculateDirectionFromStick(Vector2 stick)
    {
        float absX = Mathf.Abs(stick.x);
        float absY = Mathf.Abs(stick.y);

        if (absX > absY)
        {
            return new Vector2(stick.x > 0 ? 1 : -1, 0);
        }
        else
        {
            return new Vector2(0, stick.y > 0 ? 1 : -1);
        }
    }
    #endregion

    #region Collision & movement calculation
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
    /// Prevents forward movement while snapping to avoid corner cutting.
    /// </summary>
    private Vector2 CalculateNextPosition(Vector2 currentPos, bool wallAhead)
    {
        Vector2 nextPos = currentPos;

        if (wallAhead)
        {
            nextPos = StopAtCellCenter(currentPos);
        }
        else if (_moveDirection != Vector2.zero && !_isSnapping)
        {
            // Only move forward when NOT snapping (prevents corner cutting)
            nextPos += _moveDirection * _speed * _speedMult * Time.fixedDeltaTime;
        }

        // ALWAYS enforce axis locking (handles snapping movement)
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

        if (IsMovingHorizontally())
        {
            // Moving horizontally, lock Y to center
            float targetY = Mathf.Floor(position.y) + CELL_CENTER_OFFSET;

            if (_isSnapping && _snapAxis == SnapAxis.Y)
            {
                // Snap smoothly during transition
                float snapSpeed = _speed * _snapSpeedMultiplier * Time.fixedDeltaTime;
                lockedPos.y = Mathf.MoveTowards(position.y, targetY, snapSpeed);

                if (Mathf.Abs(lockedPos.y - targetY) <= WALL_STOP_TOLERANCE)
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
        else if (IsMovingVertically())
        {
            // Moving vertically, lock X to center
            float targetX = Mathf.Floor(position.x) + CELL_CENTER_OFFSET;

            if (_isSnapping && _snapAxis == SnapAxis.X)
            {
                // Snap smoothly during transition
                float snapSpeed = _speed * _snapSpeedMultiplier * Time.fixedDeltaTime;
                lockedPos.x = Mathf.MoveTowards(position.x, targetX, snapSpeed);

                if (Mathf.Abs(lockedPos.x - targetX) <= WALL_STOP_TOLERANCE)
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
        if ((nextPos - targetCenter).sqrMagnitude <= SNAP_COMPLETE_TOLERANCE)
        {
            nextPos = targetCenter;
            _moveDirection = Vector2.zero;
        }

        return nextPos;
    }

    /// <summary>
    /// Initiates a direction change by setting up snapping for the perpendicular axis.
    /// Clamps position to prevent corner cutting.
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
            // Clamp to current cell center to prevent corner cutting
            Vector2 clampedPos = currentPos;
            if (IsMovingHorizontally())
            {
                // Was moving horizontally, clamp X to current cell center
                clampedPos.x = Mathf.Floor(currentPos.x) + CELL_CENTER_OFFSET;
            }
            else if (IsMovingVertically())
            {
                // Was moving vertically, clamp Y to current cell center
                clampedPos.y = Mathf.Floor(currentPos.y) + CELL_CENTER_OFFSET;
            }
            _rb.position = clampedPos;

            if (Mathf.Abs(_queuedDirection.x) > 0f)
            {
                // New direction is horizontal, snap Y axis
                _snapAxis = SnapAxis.Y;
                _snapTarget = Mathf.Floor(clampedPos.y) + CELL_CENTER_OFFSET;
            }
            else
            {
                // New direction is vertical, snap X axis
                _snapAxis = SnapAxis.X;
                _snapTarget = Mathf.Floor(clampedPos.x) + CELL_CENTER_OFFSET;
            }

            _isSnapping = true;
            _moveDirection = _queuedDirection;
            _queuedDirection = Vector2.zero;
        }
    }
    #endregion

    #region Centering & crossing checks
    /// <summary>
    /// Checks if the player is centered on the axis perpendicular to current movement.
    /// </summary>
    private bool IsCenteredOnCurrentAxis(Vector2 currentPos)
    {
        if (IsMovingHorizontally())
        {
            // Moving horizontally, check Y centering
            float frac = currentPos.y - Mathf.Floor(currentPos.y);
            return Mathf.Abs(frac - CELL_CENTER_OFFSET) <= _centerThreshold;
        }
        else if (IsMovingVertically())
        {
            // Moving vertically, check X centering
            float frac = currentPos.x - Mathf.Floor(currentPos.x);
            return Mathf.Abs(frac - CELL_CENTER_OFFSET) <= _centerThreshold;
        }

        return false;
    }

    /// <summary>
    /// Checks if the player will cross the center of their current cell this tick.
    /// </summary>
    private bool WillCrossCenterThisTick(Vector2 currentPos)
    {
        float moveAmount = _speed * _speedMult * Time.fixedDeltaTime;

        if (IsMovingHorizontally())
        {
            // Moving horizontally, check X crossing (center at integer.5)
            float frac = currentPos.x - Mathf.Floor(currentPos.x);
            float projFrac = frac + (_moveDirection.x > 0 ? moveAmount : -moveAmount);

            // Check if we cross 0.5
            if (
                (
                    _moveDirection.x > 0f
                    && frac < CELL_CENTER_OFFSET
                    && projFrac >= CELL_CENTER_OFFSET
                )
                || (
                    _moveDirection.x < 0f
                    && frac > CELL_CENTER_OFFSET
                    && projFrac <= CELL_CENTER_OFFSET
                )
            )
                return true;
        }
        else if (IsMovingVertically())
        {
            // Moving vertically, check Y crossing (center at integer.5)
            float frac = currentPos.y - Mathf.Floor(currentPos.y);
            float projFrac = frac + (_moveDirection.y > 0 ? moveAmount : -moveAmount);

            // Check if we cross 0.5
            if (
                (
                    _moveDirection.y > 0f
                    && frac < CELL_CENTER_OFFSET
                    && projFrac >= CELL_CENTER_OFFSET
                )
                || (
                    _moveDirection.y < 0f
                    && frac > CELL_CENTER_OFFSET
                    && projFrac <= CELL_CENTER_OFFSET
                )
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

        if (IsMovingHorizontally())
        {
            float frac = currentPos.x - Mathf.Floor(currentPos.x);
            float projectedPos = currentPos.x + _moveDirection.x * doubleMovement;
            float projectedFrac = projectedPos - Mathf.Floor(projectedPos);

            if (
                (
                    _moveDirection.x > 0f
                    && frac < CELL_CENTER_OFFSET
                    && projectedFrac >= CELL_CENTER_OFFSET
                )
                || (
                    _moveDirection.x < 0f
                    && frac > CELL_CENTER_OFFSET
                    && projectedFrac <= CELL_CENTER_OFFSET
                )
            )
                return true;
        }
        else if (IsMovingVertically())
        {
            float frac = currentPos.y - Mathf.Floor(currentPos.y);
            float projectedPos = currentPos.y + _moveDirection.y * doubleMovement;
            float projectedFrac = projectedPos - Mathf.Floor(projectedPos);

            if (
                (
                    _moveDirection.y > 0f
                    && frac < CELL_CENTER_OFFSET
                    && projectedFrac >= CELL_CENTER_OFFSET
                )
                || (
                    _moveDirection.y < 0f
                    && frac > CELL_CENTER_OFFSET
                    && projectedFrac <= CELL_CENTER_OFFSET
                )
            )
                return true;
        }

        return false;
    }
    #endregion

    #region Utilities
    /// <summary>
    /// Checks if two directions are on the same axis.
    /// </summary>
    private bool IsSameAxis(Vector2 dir1, Vector2 dir2)
    {
        return (Mathf.Abs(dir1.x) > 0f && Mathf.Abs(dir2.x) > 0f)
            || (Mathf.Abs(dir1.y) > 0f && Mathf.Abs(dir2.y) > 0f);
    }

    /// <summary>
    /// Helper to check if moving horizontally (reduces repeated Mathf.Abs calls).
    /// </summary>
    private bool IsMovingHorizontally() => Mathf.Abs(_moveDirection.x) > 0f;

    /// <summary>
    /// Helper to check if moving vertically (reduces repeated Mathf.Abs calls).
    /// </summary>
    private bool IsMovingVertically() => Mathf.Abs(_moveDirection.y) > 0f;
    #endregion

    #region Public setters
    /// <summary>
    /// Sets the movement speed.
    /// </summary>
    /// <param name="newSpeed">New speed value.</param>
    public void SetSpeed(float newSpeed) => _speed = newSpeed;

    /// <summary>
    /// Stops all movement immediately.
    /// </summary>
    public void Stop()
    {
        _moveDirection = Vector2.zero;
        _queuedDirection = Vector2.zero;
        _currentStickInput = Vector2.zero;
        _isSnapping = false;
    }
    #endregion
}
