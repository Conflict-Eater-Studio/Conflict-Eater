using UnityEngine;

/// <summary>
/// Reusable movement helper that encapsulates the movement/snapping/queue logic.
/// This is a plain class (not a MonoBehaviour). Instantiate it from controllers and call
/// OnMove(...) for input and FixedTick() from FixedUpdate.
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

    // Intent / input filtering
    private float _inputDeadzone = 0.15f;
    private float _axisSwitchHysteresis = 1.15f;
    private SnapAxis? _lastDominantAxis = null;

    private Vector2 _moveDirection = Vector2.zero;
    private Vector2 _queuedDirection = Vector2.zero;

    // Time [s] before player can change direction on the same axis
    private float _minDirectionTime = .05f;
    private float _timeSinceDirectionStart = 0f;

    // Snapping state when switching axis
    private bool _isSnapping = false;
    private SnapAxis _snapAxis = SnapAxis.X;
    private float _snapTarget = 0f;

    public Movement(Rigidbody2D rb, Transform transformRef, Grid grid, float speed, float centerThreshold = 0.06f, float snapSpeedMultiplier = 1.5f, float minDirectionTime = .05f)
    {
        _rb = rb;
        _transformRef = transformRef;
        _grid = grid;
        _speed = speed;
        _centerThreshold = centerThreshold;
        _snapSpeedMultiplier = snapSpeedMultiplier;
        _minDirectionTime = minDirectionTime;
    }

    // Getters
    public Vector2 MoveDirection => _moveDirection;
    public Vector2 QueuedDirection => _queuedDirection;
    public float MinDirectionTime { get => _minDirectionTime; set => _minDirectionTime = value; }

    /// <summary>
    /// Call with raw move input (e.g. from Input System). Uses the same axis-snapping/queuing logic
    /// that was previously in PlayerController.
    /// </summary>
    public void OnMove(Vector2 moveInput)
    {
        // Deadzone
        Vector2 raw = moveInput;
        if (raw.magnitude < _inputDeadzone)
            raw = Vector2.zero;

        float absX = Mathf.Abs(raw.x);
        float absY = Mathf.Abs(raw.y);

        // Determine dominant axis with hysteresis to capture player intent
        SnapAxis dominant;
        if (absX > absY * _axisSwitchHysteresis)
            dominant = SnapAxis.X;
        else if (absY > absX * _axisSwitchHysteresis)
            dominant = SnapAxis.Y;
        else if (_lastDominantAxis.HasValue)
            dominant = _lastDominantAxis.Value; // keep previous intent when ambiguous
        else
            dominant = (absX >= absY) ? SnapAxis.X : SnapAxis.Y;

        _lastDominantAxis = dominant;

        Vector2 dir = Vector2.zero;
        if (dominant == SnapAxis.X && absX > 0f)
            dir = new Vector2(Mathf.Sign(raw.x), 0f);
        else if (dominant == SnapAxis.Y && absY > 0f)
            dir = new Vector2(0f, Mathf.Sign(raw.y));

        // Input below deadzone => no new request
        if (raw == Vector2.zero)
        {
            return;
        }

        Vector3Int nextCell = Grid.WorldToCell(_rb.position + dir);
        bool canMoveNow = _grid.IsWalkable(nextCell);

        if (canMoveNow)
        {
            if (_moveDirection == Vector2.zero)
            {
                _moveDirection = dir;
                _queuedDirection = Vector2.zero;

                _timeSinceDirectionStart = 0f;
            }
            else if ((_moveDirection.x != 0 && dir.x != 0) || (_moveDirection.y != 0 && dir.y != 0))
            {
                _queuedDirection = dir;
            }
            else
            {
                _queuedDirection = dir;
            }
        }
        else
        {
            _queuedDirection = dir;
        }
    }

    /// <summary>
    /// Call from FixedUpdate to advance movement. This will call Rigidbody2D.MovePosition.
    /// </summary>
    public void FixedTick()
    {
        Vector2 currentPos = _rb.position;

        Vector2 baseMovement = _moveDirection * _speed * _speedMult * Time.fixedDeltaTime;

        bool wallAhead = false;
        if (_moveDirection != Vector2.zero)
        {
            Vector3Int curCell = Grid.WorldToCell(_rb.position);
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
                    bool minTimeOk = _timeSinceDirectionStart >= _minDirectionTime;
                    bool sameAxis = (Mathf.Abs(_moveDirection.x) > 0f && Mathf.Abs(_queuedDirection.x) > 0f) || (Mathf.Abs(_moveDirection.y) > 0f && Mathf.Abs(_queuedDirection.y) > 0f);

                    if ((!sameAxis) || minTimeOk || wallAhead)
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

                        var prevAxis = (Mathf.Abs(_moveDirection.x) > 0f) ? SnapAxis.X : SnapAxis.Y;
                        _moveDirection = _queuedDirection;

                        _timeSinceDirectionStart = 0f;

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

        if (_moveDirection != Vector2.zero)
            _timeSinceDirectionStart += Time.fixedDeltaTime;
    }

    public void SetSpeed(float newSpeed) => _speed = newSpeed;
    public void Stop()
    {
        _moveDirection = Vector2.zero;
        _queuedDirection = Vector2.zero;
        _isSnapping = false;
    }
}
