using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static ShadowController;

public class ShadowPlayerController : MonoBehaviour
{
    private const string MoveActionName = "Move";
    private const string SwitchActionName = "GhostSwitch";

    [Header("Shadow Settings")]
    [SerializeField] private GameObject _shadowPrefab;
    [SerializeField] private int _shadowCount = 4;
    [SerializeField] private float _spawnDelay = 1f;
    [SerializeField] private float _maxSwitchDistance = 10f;

    private readonly List<GameObject> _shadows = new();
    public IReadOnlyList<GameObject> Shadows => _shadows;

    private PlayerInput _playerInput;
    private int _activeShadowIndex;
    private bool _canSwitch = true;

    #region Unity Lifecycle
    private void Start()
    {
        _playerInput = GetComponentInParent<PlayerInput>();

        if (_playerInput != null)
        {
            _playerInput.actions[SwitchActionName].performed += OnSwitch;
            _playerInput.actions[MoveActionName].performed += OnMove;
        }

        GameManager.Instance.Timer.OnRoundEnd += HandleRoundEnd;
        StartCoroutine(SpawnShadows());
    }

    private void OnDestroy()
    {
        if (_playerInput != null)
        {
            _playerInput.actions[MoveActionName].performed -= OnMove;
            _playerInput.actions[SwitchActionName].performed -= OnSwitch;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.Timer.OnRoundEnd -= HandleRoundEnd;
    }
    #endregion

    #region Input Handling
    private void OnMove(InputAction.CallbackContext context)
    {
        if (_shadows.Count == 0) return;

        Vector2 moveInput = context.ReadValue<Vector2>();
        var activeGhost = _shadows[_activeShadowIndex];
        activeGhost.GetComponent<ShadowController>().Move(moveInput);
    }

    private void OnSwitch(InputAction.CallbackContext context)
    {
        if (!_canSwitch || _shadows.Count == 0) return;
        StartCoroutine(SwitchGhostCoroutine());
    }

    #endregion

    #region Spawning Logic

    public void SetShadowPrefab(GameObject gameObject)
    {
        _shadowPrefab = gameObject;
    }

    private IEnumerator SpawnShadows()
    {
        for (int i = 0; i < _shadowCount; i++)
        {
            var spawnPosition = new Vector3(-0.5f, -0.5f, 0f);
            var ghost = Instantiate(_shadowPrefab, spawnPosition, Quaternion.identity);
            ghost.transform.SetParent(transform);
            ghost.name = $"Ghost_{i + 1}";

            var controller = ghost.GetComponent<ShadowController>();

            controller.SetShadowType((ShadowType)(i % 4));

            bool isActive = (i == 0);

            if (isActive)
                controller.SetState(new ShadowActiveState());
            else
                controller.SetState(new ShadowExitBaseState());


            _shadows.Add(ghost);
            UpdateAppearance();

            yield return new WaitForSeconds(_spawnDelay);
        }
    }

    private void UpdateAppearance()
    {
        for (int i = 0; i < _shadows.Count; i++)
        {
            var controller = _shadows[i].GetComponent<ShadowController>();
            var appearance = _shadows[i].GetComponent<ShadowAppearanceManager>();

            if (i == _activeShadowIndex)
                appearance.SetActive();
            else
                appearance.SetNormal();
        }
    }

    #endregion

    #region Switching Logic
    private IEnumerator SwitchGhostCoroutine()
    {
        _canSwitch = false;

        var currentGhost = _shadows[_activeShadowIndex];
        var currentController = currentGhost.GetComponent<ShadowController>();

        currentController.SetState(new ShadowScatterState());

        int closestIndex = _activeShadowIndex;
        float closestDistance = _maxSwitchDistance;

        for (int i = 0; i < _shadows.Count; i++)
        {
            if (i == _activeShadowIndex) continue;

            float distance = Vector3.Distance(currentGhost.transform.position, _shadows[i].transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        if (closestIndex != _activeShadowIndex)
        {
            _activeShadowIndex = closestIndex;
            Debug.Log($"Switched to ghost #{_activeShadowIndex + 1}");
        }
        else
        {
            Debug.Log("No ghost in range to switch to");
        }

        var newActiveController = _shadows[_activeShadowIndex].GetComponent<ShadowController>();
        newActiveController.SetState(new ShadowActiveState());

        UpdateAppearance();

        yield return new WaitForSeconds(0.3f);
        _canSwitch = true;
    }


    #endregion

    #region Round Reset

    private void HandleRoundEnd(object sender, System.EventArgs e)
    {
        StopAllCoroutines();
        GameManager.Instance.IsFrightenedShadowState = false;

        foreach (var ghost in _shadows)
        {
            if (ghost != null)
                Destroy(ghost);
        }

        _shadows.Clear();
        _activeShadowIndex = 0;

        StartCoroutine(RespawnAfterDelay(0.25f));
    }

    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(SpawnShadows());
    }

    #endregion
}
