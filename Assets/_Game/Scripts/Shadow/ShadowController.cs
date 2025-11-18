using System;
using UnityEngine;

/// <summary>
/// Defines the type of shadow/ghost with their behavior patterns.
/// </summary>
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
    #region Inspector Fields
    [Header("Direct Indicator")]
    [SerializeField] private GameObject _directGameObject;
    [SerializeField] private float _directOffset = 0.37f;

    [Header("Movement Settings")]
    [SerializeField] protected float _speed = 10.5f;
    [SerializeField] private float _centerThreshold = 0.15f;
    [SerializeField] private float _snapSpeedMultiplier = 1.5f;

    [Header("AI Settings")]
    [SerializeField] private bool _enableAIMovement = true;
    [SerializeField] private float _directionChangeDelay = 0.5f;

    [Header("Shadow Identity")]
    [SerializeField] private ShadowType _shadowType;
    #endregion

    #region Properties
    private Rigidbody2D _rb;
    private Movement _movement;
    private Vector2Int _currentDirection;
    private ShadowBehaviorCycle _shadowBehaviorCycle;
    private bool _isShadowActive = false;
    private ShadowPlayerController _owner;

    public bool IsShadowActive
    {
        get { return _isShadowActive; }
        set { _isShadowActive = value; }
    }

    public Vector2Int CurrentDirection{
        get => _currentDirection;
        set
        {
            _currentDirection = value;
            UpdateDirectObjectPosition();
        }
    }
    public Movement Movement => _movement;
    public ShadowBehaviorCycle ShadowBehaviorCycle => _shadowBehaviorCycle;
    private Grid _grid;
    public bool EnableAI => _enableAIMovement;
    public float DirectionChangeDelay => _directionChangeDelay;
    public ShadowType Type => _shadowType;
    #endregion

    #region State Management
    private IShadowState _previousState;
    private IShadowState _currentState;
    private IShadowState _nextState;

    public IShadowState PreviousState => _previousState;
    public IShadowState CurrentState => _currentState;
    public IShadowState NextState => _nextState;

    /// <summary>
    /// Sets a new state for the shadow.
    /// </summary>
    public void SetState(IShadowState newState)
    {
        if (newState == null)
            return;

        _previousState = _currentState;
        _currentState?.Exit(this);

        _currentState = newState;
        _currentState.Enter(this);
    }

    /// <summary>
    /// Queues the next state to transition to.
    /// </summary>
    public void QueueNextState(IShadowState next)
    {
        _nextState = next;
    }

    /// <summary>
    /// Reverts to the previous state if available.
    /// </summary>
    public void RevertToPreviousState()
    {
        if (_previousState != null)
        {
            SetState(_previousState);
        }
    }

    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _grid = FindFirstObjectByType<Grid>();
        _movement = new Movement(_rb, transform, _grid, _speed, _centerThreshold, _snapSpeedMultiplier);
        _shadowBehaviorCycle = GetComponent<ShadowBehaviorCycle>();
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

    public void FixedUpdate()
    {
        _movement.FixedTick();

        float speedMult = 1f;

        _movement.SpeedMult = speedMult;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerLight"))
        {
            if(GameManager.Instance.IsFrightenedShadowState)
            {
                if(_isShadowActive)
                {
                    _owner = GetComponentInParent<ShadowPlayerController>();
                    _owner.StartCoroutine(_owner.SwitchShadowsRandomCoroutine());
                }

                gameObject.transform.position = GameManager.Instance.Grid.GetSpawnPoint(Grid.SpawnPointType.Shadow);
                SetState(new ShadowExitBaseState());
            } else
            {
                GameManager.Instance.Timer.EndRound();
            }
        }
    }

    #endregion

    #region Movement
    /// <summary>
    /// Moves the shadow based on input and sets current direction.
    /// </summary>
    public void Move(Vector2 input)
    {
        Vector2Int newDirection = Vector2Int.zero;

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            newDirection = input.x > 0 ? Vector2Int.right : Vector2Int.left;
        else if (Mathf.Abs(input.y) > 0)
            newDirection = input.y > 0 ? Vector2Int.up : Vector2Int.down;

        if (newDirection != Vector2Int.zero)
            CurrentDirection = newDirection;

        _movement.OnMove(input);
    }

    /// <summary>
    /// Updates the direct indicator's position based on current direction.
    /// </summary>
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

    #endregion

    #region Setters
    /// <summary>
    /// Sets the shadow's color.
    /// </summary>
    public void SetColor(Color color)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = color;
    }

    /// <summary>
    /// Sets the shadow type (Blinky, Pinky, etc.).
    /// </summary>
    public void SetShadowType(ShadowType type)
    {
        _shadowType = type;
    }
    #endregion
}
