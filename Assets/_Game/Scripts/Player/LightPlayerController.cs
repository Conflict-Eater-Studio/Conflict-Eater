using System;
using System.Data;
using Unity.VisualScripting;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LightPlayerController : MonoBehaviour
{
    private const string MoveActionName = "Move";
    private const string PauseActionName = "Pause";

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

    [SerializeField] private Animator _animator;

    private const string AnimatorBoolIsMovingBottom = "IsMovingBottom";
    private const string AnimatorBoolIsMovingUp = "IsMovingUp";
    private const string AnimatorBoolIsMovingRight = "IsMovingRight";
    private const string AnimatorBoolIsMovingLeft = "IsMovingLeft";

    public float Speed => _speed;
    
    private Grid _grid;
    private bool _isRoundStarted = false;

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
        _animator = GetComponentInParent<Animator>();
        _movement = new Movement(_rb, transform, _grid, _speed, _centerThreshold, _snapSpeedMultiplier);

        _playerInput = GetComponentInParent<PlayerInput>();
        if (_playerInput)
        {
            InputAction moveAction = _playerInput.actions[MoveActionName];
            moveAction.performed += OnMove;

            InputAction pauseAction = _playerInput.actions[PauseActionName];
            pauseAction.performed += OnPause;
        }

        if(GameManager.Instance)
        {
            GameManager.Instance.Timer.OnRoundStart += Timer_OnRoundStart;
            GameManager.Instance.Timer.OnRoundEnd += Timer_OnRoundEnd;

            GameManager.Instance.Timer.OnMatchPause += Light_OnMatchPause;
            GameManager.Instance.Timer.OnMatchResume += Light_OnMatchResume;
        }
    }

    private void Light_OnMatchResume(object sender, EventArgs e)
    {
        _movement.IsLocked = false;
    }

    private void Light_OnMatchPause(object sender, EventArgs e)
    {
        _movement.IsLocked = true;
    }

    private void Timer_OnRoundEnd(object sender, EventArgs e)
    {
        _isRoundStarted = false;
    }

    private void Timer_OnRoundStart(object sender, EventArgs e)
    {
        _isRoundStarted = true;
    }

    public virtual void OnMove(InputAction.CallbackContext context)
    {
        if (!_isRoundStarted) return;

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

    public virtual void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (GameManager.Instance.Timer.IsGamePaused)
        {
            SceneManager.UnloadSceneAsync("GamePause").completed += (_) =>
            {
                GameManager.Instance.Timer.Resume();
            };
        }
        else
        {
            GameManager.Instance.Timer.Pause();
            SceneManager.LoadSceneAsync("GamePause", LoadSceneMode.Additive);
        }
    }

    protected virtual void FixedUpdate()
    {
        _movement.FixedTick();
        UpdateAnimation();

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

        if (GameManager.Instance)
        {
            GameManager.Instance.Timer.OnRoundStart -= Timer_OnRoundStart;
            GameManager.Instance.Timer.OnRoundEnd -= Timer_OnRoundEnd;
            GameManager.Instance.Timer.OnMatchPause -= Light_OnMatchPause;
            GameManager.Instance.Timer.OnMatchResume -= Light_OnMatchResume;
        }
    }

    private void UpdateAnimation()
    {
        bool isMoving = _moveInput.sqrMagnitude > 0.1f;
        if (!isMoving)
        {
            _animator.SetBool(AnimatorBoolIsMovingUp, false);
            _animator.SetBool(AnimatorBoolIsMovingBottom, false);
            _animator.SetBool(AnimatorBoolIsMovingLeft, false);
            _animator.SetBool(AnimatorBoolIsMovingRight, false);
            return;
        }

        // Reset
        _animator.SetBool(AnimatorBoolIsMovingUp, false);
        _animator.SetBool(AnimatorBoolIsMovingBottom, false);
        _animator.SetBool(AnimatorBoolIsMovingLeft, false);
        _animator.SetBool(AnimatorBoolIsMovingRight, false);

        switch (CurrentDirection)
        {
            case var d when d == Vector2Int.up:
                _animator.SetBool(AnimatorBoolIsMovingUp, true);
                break;

            case var d when d == Vector2Int.down:
                _animator.SetBool(AnimatorBoolIsMovingBottom, true);
                break;

            case var d when d == Vector2Int.left:
                _animator.SetBool(AnimatorBoolIsMovingLeft, true);
                break;

            case var d when d == Vector2Int.right:
                _animator.SetBool(AnimatorBoolIsMovingRight, true);
                break;
        }
    }

}