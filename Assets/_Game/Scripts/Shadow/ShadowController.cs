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

    private Rigidbody2D _rb;
    private Vector2 _moveInput;
    private Movement _movement;
    public Movement Movement => _movement;
    private Grid _grid;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();

        _grid = FindFirstObjectByType<Grid>();
        _movement = new Movement(_rb, transform, _grid, _speed, _centerThreshold, _snapSpeedMultiplier);
    }
    public void Move(Vector2 moveInput)
    {
        _movement.OnMove(moveInput);
    }

    protected virtual void FixedUpdate()
    {
        _movement.FixedTick();

        if (GameManager.Instance.Grid.IsLightTile(
            Grid.WorldToCell(transform.position, Grid.TilemapType.Light)
        ))
        {
            _movement.SpeedMult = _lightTileSpeedPenalityMultiplier;
        }
        else
        {
            _movement.SpeedMult = 1f;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerLight"))
        {
            GameManager.Instance.Timer.EndRound();
        }
    }
}
