using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShadowPlayerController : MonoBehaviour
{
    [SerializeField] private GameObject ghostPrefab; 
    [SerializeField] private int totalGhosts = 4;     
    [SerializeField] private float spawnDelay = 1f;

    private List<GameObject> ghosts = new List<GameObject>();
    private int activeGhostIndex = 0;
    private bool canSwitch = true;

    private Color inactiveColor = new Color(0.5f, 0.5f, 1f, 0.5f); 
    private Color activeColor = Color.darkBlue;

    [SerializeField] private float maxSwitchDistance = 10f;

    private const string MoveActionName = "Move";

    private Rigidbody2D _rb;
    private Vector2 _moveInput;
    private Movement _movement;
    private PlayerInput _playerInput;
    public Movement Movement => _movement;

    [Tooltip("Movement speed in units per second")]
    [SerializeField] protected float _speed = 5f;
    [Tooltip("How close to .5 before applying queued dir")]
    [SerializeField] private float _centerThreshold = 0.15f;
    [Tooltip("How fast to snap to center when switching axis (multiplier of normal speed)")]
    [SerializeField] private float _snapSpeedMultiplier = 1.5f;

    private Grid _grid;

    private void Start()
    {
        _rb = GetComponentInParent<Rigidbody2D>();
        _grid = FindFirstObjectByType<Grid>();
        _movement = new Movement(_rb, transform, _grid, _speed, _centerThreshold, _snapSpeedMultiplier);

        _playerInput = GetComponentInParent<PlayerInput>();
        if (_playerInput)
        {
            InputAction moveAction = _playerInput.actions[MoveActionName];
            moveAction.performed += OnMove;

            _playerInput.actions["GhostSwitch"].performed += OnSwitch;
        }

        SetGhostColors();

        //StartCoroutine(SpawnRemainingGhosts());
    }

    public virtual void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _moveInput = context.ReadValue<Vector2>();
            _movement.OnMove(_moveInput);
        }
    }

    protected virtual void FixedUpdate()
    {
        _movement.FixedTick();
    }

    public void SetGhostPrefab(GameObject prefab)
    {
        ghostPrefab = prefab;
    }

    private IEnumerator SpawnRemainingGhosts()
    {
        for (int i = 1; i < totalGhosts; i++)
        {
            yield return new WaitForSeconds(spawnDelay);

            GameObject ghost = Instantiate(ghostPrefab, Vector3.zero, Quaternion.identity);
            ghost.name = $"Ghost_{i + 1}";

            ghost.GetComponent<LightPlayerController>().enabled = false;
            Destroy(ghost.GetComponent<CoinCollector>());

            ghosts.Add(ghost);

            SetGhostColors();
        }
    }

    public void OnSwitch(InputAction.CallbackContext context)
    {
        if (context.performed && canSwitch && ghosts.Count > 0)
        {
            //StartCoroutine(SwitchGhost());
        }
    }

    private IEnumerator SwitchGhost()
    {
        canSwitch = false;

        GameObject currentGhost = ghosts[activeGhostIndex];
        currentGhost.GetComponent<LightPlayerController>().enabled = false;

        int closestIndex = activeGhostIndex;
        float closestDistance = maxSwitchDistance;

        for (int i = 0; i < ghosts.Count; i++)
        {
            if (i == activeGhostIndex) continue;

            float distance = Vector3.Distance(currentGhost.transform.position, ghosts[i].transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        if (closestIndex != activeGhostIndex)
        {
            activeGhostIndex = closestIndex;
            ghosts[activeGhostIndex].GetComponent<LightPlayerController>().enabled = true;

            Debug.Log($"Switched to ghost #{activeGhostIndex + 1}");
        }
        else
        {
            ghosts[activeGhostIndex].GetComponent<LightPlayerController>().enabled = true;
            Debug.Log("No ghost in range to switch to");
        }

        SetGhostColors();

        yield return new WaitForSeconds(0.3f);
        canSwitch = true;
    }

    private void SetGhostColors()
    {
        for (int i = 0; i < ghosts.Count; i++)
        {
            SpriteRenderer sr = ghosts[i].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = (i == activeGhostIndex) ? activeColor : inactiveColor;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerLight"))
        {
            GameManager.Instance.Grid.ResetMapState();
            GameManager.Instance.PlayerManager.SwapPlayerGamepads(PlayerManager.PlayerType.Light, PlayerManager.PlayerType.Shadow);

            collision.GetComponentInChildren<CoinCollector>().ToggleActivePlayer();
        }
    }

    private void OnDestroy()
    {
        if (_playerInput != null)
        {
            var moveAction = _playerInput.actions[MoveActionName];
            moveAction.performed -= OnMove;
        }
    }


}
