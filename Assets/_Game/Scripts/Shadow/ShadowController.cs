using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class ShadowController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed in units per second")]
    [SerializeField] protected float _speed = 10.5f;
    [Tooltip("How close to .5 before applying queued dir")]
    [SerializeField] private float _centerThreshold = 0.15f;
    [Tooltip("How fast to snap to center when switching axis (multiplier of normal speed)")]
    [SerializeField] private float _snapSpeedMultiplier = 1.5f;
    [Tooltip("Speed penalty multiplier when on light tiles")]
    [SerializeField] private float _lightTileSpeedPenalityMultiplier = 0.5f;
    [Tooltip("Speed penalty multiplier when the ghost is inactive (AI-controlled)")]
    [SerializeField] private float _inactiveGhostSpeedMultiplier = 0.7f;

    private Rigidbody2D _rb;
    private Movement _movement;
    public Movement Movement => _movement;
    private Grid _grid;

    [Header("AI Settings")]
    [Tooltip("Enable AI movement when this ghost is inactive")]
    [SerializeField] private bool _enableAIMovement = true;

    [Tooltip("Delay (in seconds) before AI picks a new random direction after hitting a wall")]
    [SerializeField] private float _directionChangeDelay = 0.5f;

    private bool _isActive = true;
    public bool IsActive => _isActive;
    private Vector2 _aiDirection = Vector2.zero;
    private float _aiChangeTimer = 0.5f;
    private int _aiMoveCount = 0;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        _grid = FindFirstObjectByType<Grid>();
        _movement = new Movement(_rb, transform, _grid, _speed, _centerThreshold, _snapSpeedMultiplier);
    }

    public void Move(Vector2 moveInput)
    {
        _movement.OnMove(moveInput);
    }

    public void SetActiveControl(bool isActive)
    {
        _isActive = isActive;

        if (!_isActive && _enableAIMovement)
        {
            PickRandomDirection();
        }
    }

    protected void FixedUpdate()
    {
        if (!_isActive && _enableAIMovement)
        {
            HandleAIMovement();
        }

        _movement.FixedTick();

        float speedMult = 1f;

        if (GameManager.Instance.Grid.IsLightTile(Grid.WorldToCell(transform.position, Grid.TilemapType.Light)))
        {
            speedMult *= _lightTileSpeedPenalityMultiplier;
        }

        if (!_isActive)
        {
            speedMult *= _inactiveGhostSpeedMultiplier;
        }

        _movement.SpeedMult = speedMult;
    }

    private void HandleAIMovement()
    {
        _aiChangeTimer -= Time.fixedDeltaTime;
        if (_aiChangeTimer <= 0f)
        {
            PickRandomDirection();
        }

        _movement.OnMove(_aiDirection);
    }

    private void PickRandomDirection()
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

        _aiChangeTimer = _directionChangeDelay;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerLight"))
        {
            GameManager.Instance.Timer.EndRound();
        }
    }
}
