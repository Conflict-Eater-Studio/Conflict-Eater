using System;
using System.Data;
using Unity.VisualScripting;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class LightPlayerController : MonoBehaviour
{
    private const string MoveActionName = "Move";

    private Rigidbody2D _rb;
    private Vector2 _moveInput;
    private Movement _movement;
    private PlayerInput _playerInput;
    public Movement Movement => _movement;

    [Tooltip("Movement speed in units per second")]
    [SerializeField] protected float _speed = 10f;
    [Tooltip("How close to .5 before applying queued dir")]
    [SerializeField] private float _centerThreshold = 0.15f;
    [Tooltip("How fast to snap to center when switching axis (multiplier of normal speed)")]
    [SerializeField] private float _snapSpeedMultiplier = 1.5f;
    
    private Grid _grid;

    private Vector2Int _currentDirection = Vector2Int.up;
    public Vector2Int CurrentDirection
    {
        get => _currentDirection;
        private set => _currentDirection = value;
    }

    protected virtual void Awake()
    {
        _rb = GetComponentInParent<Rigidbody2D>();
        _grid = FindFirstObjectByType<Grid>();
        _movement = new Movement(_rb, transform, _grid, _speed, _centerThreshold, _snapSpeedMultiplier);

        _playerInput = GetComponentInParent<PlayerInput>();
        if (_playerInput)
        {
            InputAction moveAction = _playerInput.actions[MoveActionName];
            moveAction.performed += OnMove;
        }
    }

    public virtual void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _moveInput = context.ReadValue<Vector2>();

            if (_moveInput.magnitude > 0.1f)
            {
                if (Mathf.Abs(_moveInput.x) > Mathf.Abs(_moveInput.y))
                    CurrentDirection = _moveInput.x > 0 ? Vector2Int.right : Vector2Int.left;
                else
                    CurrentDirection = _moveInput.y > 0 ? Vector2Int.up : Vector2Int.down;
            }

            _movement.OnMove(_moveInput);
        }
    }

    protected virtual void FixedUpdate()
    {
        _movement.FixedTick();
        
        // NOTE: Marking tiles as active (potential issue)
        // If the player skips over a tile, it won't activate it.
        // This situation should not happen with proper movement speed and tile size.
        // Event at low FPS this SHOULD be fine.
        GameManager.Instance.Grid.SetTileToLight(
            Grid.WorldToCell(transform.position)
        );
    }

    private void OnDestroy()
    {
        if (_playerInput != null)
        {
            var moveAction = _playerInput.actions[MoveActionName];
            moveAction.performed -= OnMove;
        }
    }

  private void OnTriggerEnter2D(Collider2D collision)
  {
    if(collision)
    {
        Debug.Log(collision);
    }
  }
}