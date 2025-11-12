using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class ShadowController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] protected float _speed = 10.5f;
    [SerializeField] private float _centerThreshold = 0.15f;
    [SerializeField] private float _snapSpeedMultiplier = 1.5f;
    [SerializeField] private float _lightTileSpeedPenalityMultiplier = 0.5f;
    [SerializeField] private float _inactiveGhostSpeedMultiplier = 0.7f;

    [Header("AI Settings")]
    [SerializeField] private bool _enableAIMovement = true;
    [SerializeField] private float _directionChangeDelay = 0.5f;

    private Rigidbody2D _rb;
    private Movement _movement;
    public Movement Movement => _movement;
    private Grid _grid;

    private IShadowState _currentState;
    public bool EnableAI => _enableAIMovement;
    public float DirectionChangeDelay => _directionChangeDelay;
    public float LightTilePenalty => _lightTileSpeedPenalityMultiplier;
    public float InactivePenalty => _inactiveGhostSpeedMultiplier;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _grid = FindFirstObjectByType<Grid>();
        _movement = new Movement(_rb, transform, _grid, _speed, _centerThreshold, _snapSpeedMultiplier);
    }

    void Update()
    {
        _currentState?.Update(this);
    }

    public void SetState(IShadowState newState)
    {
        _currentState?.Exit(this);
        _currentState = newState;
        _currentState.Enter(this);
    }

    public void Move(Vector2 input)
    {
        _movement.OnMove(input);
    }

    public void SetColor(Color color)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = color;
    }

    public void FixedUpdate()
    {
        _movement.FixedTick();

        float speedMult = 1f;

        if (GameManager.Instance.Grid.IsLightTile(Grid.WorldToCell(transform.position, Grid.TilemapType.Light)))
            speedMult *= _lightTileSpeedPenalityMultiplier;

        _movement.SpeedMult = speedMult;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerLight"))
            GameManager.Instance.Timer.EndRound();
    }
}
