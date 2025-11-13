using System;
using UnityEngine;

public enum ShadowType
{
    Blinky, // Red – chases the player directly
    Pinky,  // Pink – tries to ambush the player from the front
    Inky,   // Blue – unpredictable, depends on other ghosts
    Clyde   // Orange – alternates between chasing and retreating
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class ShadowController : MonoBehaviour
{
    [SerializeField] private GameObject _directGameObject;
    [SerializeField] private float _directOffset = 0.37f;

    [Header("Movement Settings")]
    [SerializeField] protected float _speed = 0.5f;
    [SerializeField] private float _centerThreshold = 0.15f;
    [SerializeField] private float _snapSpeedMultiplier = 1.0f;
    [SerializeField] private float _inactiveGhostSpeedMultiplier = 0.7f;

    [Header("AI Settings")]
    [SerializeField] private bool _enableAIMovement = true;
    [SerializeField] private float _directionChangeDelay = 0.5f;

    private Rigidbody2D _rb;
    private Movement _movement;
    private Vector2Int _currentDirection;
    public Vector2Int CurrentDirection
    {
        get => _currentDirection;
        set {
            _currentDirection = value;
            UpdateDirectObjectPosition();
        }
    }

    public Movement Movement => _movement;
    private Grid _grid;

    public bool EnableAI => _enableAIMovement;
    public float DirectionChangeDelay => _directionChangeDelay;
    public float InactivePenalty => _inactiveGhostSpeedMultiplier;

    [Header("Shadow Identity")]
    [SerializeField] private ShadowType _shadowType;
    
    public ShadowType Type => _shadowType;

    private IShadowState _previousState;
    private IShadowState _currentState;
    private IShadowState _nextState;

    public IShadowState PreviousState => _previousState;
    public IShadowState CurrentState => _currentState;
    public IShadowState NextState => _nextState;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _grid = FindFirstObjectByType<Grid>();
        _movement = new Movement(_rb, transform, _grid, _speed, _centerThreshold, _snapSpeedMultiplier);
    }

    void Update()
    {
        _currentState?.Update(this);

        if (_nextState != null)
        {
            SetState(_nextState);
            _nextState = null;
        }
    }

    public void SetState(IShadowState newState)
    {
        if (newState == null)
            return;

        _previousState = _currentState;
        _currentState?.Exit(this);

        _currentState = newState;
        _currentState.Enter(this);
    }
    public void QueueNextState(IShadowState next)
    {
        _nextState = next;
    }
    public void RevertToPreviousState()
    {
        if (_previousState != null)
        {
            SetState(_previousState);
        }
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

        _movement.SpeedMult = speedMult;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerLight")) 
            GameManager.Instance.Timer.EndRound();
    }
    public void SetShadowType(ShadowType type)
    {
        _shadowType = type;
    }

    private void UpdateDirectObjectPosition()
    {
        if (_directGameObject == null)
            return;

        Vector3 localPos = Vector3.zero;

        switch (_currentDirection)
        {
            case var d when d == Vector2Int.up:
                localPos = new Vector3(0f, _directOffset, 0f);
                break;
            case var d when d == Vector2Int.down:
                localPos = new Vector3(0f, -_directOffset, 0f);
                break;
            case var d when d == Vector2Int.left:
                localPos = new Vector3(-_directOffset, 0f, 0f);
                break;
            case var d when d == Vector2Int.right:
                localPos = new Vector3(_directOffset, 0f, 0f);
                break;
        }

        _directGameObject.transform.localPosition = localPos;
    }
}
