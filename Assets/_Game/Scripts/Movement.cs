using UnityEngine;

/// <summary>
/// Reusable movement helper that encapsulates the movement/snapping/queue logic.
/// This is a plain class (not a MonoBehaviour). Instantiate it from controllers and call
/// OnMove(...) for input and FixedTick() from FixedUpdate.
/// </summary>
public class Movement
{
    private readonly Rigidbody2D _rb;
    private readonly Transform _transformRef;
    private readonly Grid _grid;

    private float _speed;
    private float _centerThreshold;
    private float _snapSpeedMultiplier;

    private Vector2 _moveDirection = Vector2.zero;
    private Vector2 _queuedDirection = Vector2.zero;

    // Snapping state when switching axis
    private bool _isSnapping = false;
    // 0 = x axis snapping, 1 = y axis snapping
    private int _snapAxis = 0;
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
    /// Call with raw move input (e.g. from Input System). Uses the same axis-snapping/queuing logic
    /// that was previously in PlayerController.
    /// </summary>
    public void OnMove(Vector2 moveInput)
    {
        Vector2 dir = Vector2.zero;
        if (Mathf.Abs(moveInput.x) >= Mathf.Abs(moveInput.y))
            dir = new Vector2(Mathf.Sign(moveInput.x), 0f);
        else
            dir = new Vector2(0f, Mathf.Sign(moveInput.y));

        Vector3Int nextCell = new Vector3Int(
            Mathf.FloorToInt(_transformRef.position.x + dir.x),
            Mathf.FloorToInt(_transformRef.position.y + dir.y),
            0
        );

        if (_grid.IsWalkable(nextCell))
        {
            if (_moveDirection == Vector2.zero)
            {
                _moveDirection = dir;
                _queuedDirection = Vector2.zero;
            }
            else if ((_moveDirection.x != 0 && dir.x != 0) || (_moveDirection.y != 0 && dir.y != 0))
            {
                _moveDirection = dir;
                _queuedDirection = Vector2.zero;
            }
            else
            {
                _queuedDirection = dir;
            }
        }
    }

    /// <summary>
    /// Call from FixedUpdate to advance movement. This will call Rigidbody2D.MovePosition.
    /// </summary>
    public void FixedTick()
    {
        Vector2 currentPos = _rb.position;

        Vector2 baseMovement = _moveDirection * _speed * Time.fixedDeltaTime;

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
                Vector3Int nextCell = new Vector3Int(
                    Mathf.FloorToInt(_transformRef.position.x + _queuedDirection.x),
                    Mathf.FloorToInt(_transformRef.position.y + _queuedDirection.y),
                    0
                );

                if (_grid.IsWalkable(nextCell))
                {
                    if (Mathf.Abs(_queuedDirection.x) > 0f)
                    {
                        _snapAxis = 1; // snap y
                        _snapTarget = Mathf.Floor(currentPos.y) + 0.5f;
                    }
                    else
                    {
                        _snapAxis = 0; // snap x
                        _snapTarget = Mathf.Floor(currentPos.x) + 0.5f;
                    }

                    _isSnapping = true;
                    _moveDirection = _queuedDirection;
                    _queuedDirection = Vector2.zero;
                }
                else
                {
                    _queuedDirection = Vector2.zero;
                }
            }
        }

        Vector2 nextPos = currentPos + baseMovement;

        if (_isSnapping)
        {
            float snapSpeed = _speed * _snapSpeedMultiplier * Time.fixedDeltaTime;
            if (_snapAxis == 0)
            {
                // snap x coordinate toward target
                nextPos.x = Mathf.MoveTowards(currentPos.x, _snapTarget, snapSpeed);
            }
            else
            {
                // snap y coordinate toward target
                nextPos.y = Mathf.MoveTowards(currentPos.y, _snapTarget, snapSpeed);
            }

            if (Mathf.Abs((_snapAxis == 0 ? nextPos.x : nextPos.y) - _snapTarget) <= 0.001f)
            {
                _isSnapping = false;
            }
        }

        _rb.MovePosition(nextPos);
    }

    public void SetSpeed(float newSpeed) => _speed = newSpeed;
}
