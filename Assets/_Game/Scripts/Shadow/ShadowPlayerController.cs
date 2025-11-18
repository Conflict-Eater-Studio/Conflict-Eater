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

    [SerializeField] private bool _blinky = false;
    [SerializeField] private bool _pinky = false;
    [SerializeField] private bool _inky = false;
    [SerializeField] private bool _clyde = false;

    [SerializeField] private ShadowState _blintyState;
    [SerializeField] private ShadowState _pinkyState;
    [SerializeField] private ShadowState _inkyState;
    [SerializeField] private ShadowState _clydeState;


    private readonly List<GameObject> _shadows = new();
    public IReadOnlyList<GameObject> Shadows => _shadows;

    private PlayerInput _playerInput;
    private int _activeShadowIndex;
    private bool _canSwitch = true;
    private bool _isRoundStarted = false;

    #region Unity Lifecycle
    private void Start()
    {
        _playerInput = GetComponentInParent<PlayerInput>();

        if (_playerInput != null)
        {
            _playerInput.actions[SwitchActionName].performed += OnSwitch;
            _playerInput.actions[MoveActionName].performed += OnMove;
        }

        GameManager.Instance.Timer.OnRoundStart += Timer_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnd += HandleRoundEnd;
        StartCoroutine(SpawnFirstShadow());
    }

    private void Timer_OnRoundStart(object sender, System.EventArgs e)
    {
        _isRoundStarted = true;
    }

    private void Update()
    {
        if (_shadows.Count < 4) return;

        _blinky = _shadows[0].GetComponent<ShadowController>().IsShadowActive;
        _pinky = _shadows[1].GetComponent<ShadowController>().IsShadowActive;
        _inky = _shadows[2].GetComponent<ShadowController>().IsShadowActive;
        _clyde = _shadows[3].GetComponent<ShadowController>().IsShadowActive;

        _blintyState = _shadows[0].GetComponent<ShadowController>().CurrentState.State;
        _pinkyState = _shadows[1].GetComponent<ShadowController>().CurrentState.State;
        _inkyState = _shadows[2].GetComponent<ShadowController>().CurrentState.State;
        _clydeState = _shadows[3].GetComponent<ShadowController>().CurrentState.State;

        UpdateNextShadowMarker();
    }

    private void UpdateNextShadowMarker()
    {
        int nextIndex = GetClosestShadowIndex();

        if (nextIndex == -1 ||
            Vector3.Distance(
                _shadows[_activeShadowIndex].transform.position,
                _shadows[nextIndex].transform.position) > _maxSwitchDistance)
        {
            for (int i = 0; i < _shadows.Count; i++)
            {
                var appearance = _shadows[i].GetComponent<ShadowAppearanceManager>();
                appearance.ActiveNextShadowMarker(false);
            }
            return;
        }

        for (int i = 0; i < _shadows.Count; i++)
        {
            var appearance = _shadows[i].GetComponent<ShadowAppearanceManager>();
            appearance.ActiveNextShadowMarker(i == nextIndex);
        }
    }

    private void OnDestroy()
    {
        if (_playerInput != null)
        {
            _playerInput.actions[MoveActionName].performed -= OnMove;
            _playerInput.actions[SwitchActionName].performed -= OnSwitch;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Timer.OnRoundEnd -= HandleRoundEnd;
            GameManager.Instance.Timer.OnRoundStart += Timer_OnRoundStart;
        }
    }
    #endregion

    #region Input Handling
    private void OnMove(InputAction.CallbackContext context)
    {
        if (!_isRoundStarted) return;
        if (_shadows.Count == 0) return;

        Vector2 moveInput = context.ReadValue<Vector2>();
        var activeGhost = _shadows[_activeShadowIndex];
        activeGhost.GetComponent<ShadowController>().Move(moveInput);
    }

    private void OnSwitch(InputAction.CallbackContext context)
    {
        if (!_canSwitch || _shadows.Count == 0)
            return;

        StartCoroutine(SwitchShadowsCoroutine());
    }

    #endregion

    #region Spawning Logic

    public void SetShadowPrefab(GameObject gameObject)
    {
        _shadowPrefab = gameObject;
    }

    private IEnumerator SpawnFirstShadow()
    {
        var spawnPosition = new Vector3(-0.5f, -0.5f, 0f);
        var ghost = Instantiate(_shadowPrefab, spawnPosition, Quaternion.identity);
        ghost.transform.SetParent(transform);
        ghost.name = "Ghost_1";

        var controller = ghost.GetComponent<ShadowController>();
        controller.SetShadowType(ShadowType.Blinky);
        controller.IsShadowActive = true;

        _shadows.Add(ghost);
        _activeShadowIndex = 0;
        UpdateAppearance();

        yield return new WaitUntil(() => _isRoundStarted);

        controller.SetState(new ShadowExitBaseState());

        StartCoroutine(SpawnRemainingShadows());
    }

    private IEnumerator SpawnRemainingShadows()
    {
        for (int i = 1; i < _shadowCount; i++)
        {
            var spawnPosition = new Vector3(-0.5f, -0.5f, 0f);
            var ghost = Instantiate(_shadowPrefab, spawnPosition, Quaternion.identity);
            ghost.transform.SetParent(transform);
            ghost.name = $"Ghost_{i + 1}";

            var controller = ghost.GetComponent<ShadowController>();
            controller.SetShadowType((ShadowType)(i % 4));
            controller.IsShadowActive = false;
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

            if (GameManager.Instance.IsFrightenedShadowState && !controller.IsShadowActive)
            {
                appearance.SetFrightened(true);
            }
            else if (i == _activeShadowIndex)
            {
                appearance.SetActive();
            }
            else
            {
                appearance.SetNormal();
            }
        }
    }


    #endregion

    #region Switching Logic
    private IEnumerator SwitchShadowsCoroutine()
    {
        _canSwitch = false;

        bool isFrightened = GameManager.Instance.IsFrightenedShadowState;

        var oldController = _shadows[_activeShadowIndex].GetComponent<ShadowController>();
        oldController.IsShadowActive = false;

        if (isFrightened)
            oldController.SetState(new ShadowFrightenedState());
        else
            oldController.SetState(new ShadowScatterState());

        int closestIndex = _activeShadowIndex;
        float closestDistance = _maxSwitchDistance;

        for (int i = 0; i < _shadows.Count; i++)
        {
            if (i == _activeShadowIndex) continue;

            var controller = _shadows[i].GetComponent<ShadowController>();
            if (controller.CurrentState.State == ShadowState.Eaten)
                continue;

            float dist = Vector3.Distance(
                _shadows[_activeShadowIndex].transform.position,
                _shadows[i].transform.position);

            if (dist < closestDistance)
            {
                closestIndex = i;
                closestDistance = dist;
            }
        }

        _activeShadowIndex = closestIndex;

        var newController = _shadows[_activeShadowIndex].GetComponent<ShadowController>();
        newController.IsShadowActive = true;
        newController.SetState(new ShadowActiveState());

        UpdateAppearance();

        yield return new WaitForSeconds(0.3f);
        _canSwitch = true;
    }

    public IEnumerator SwitchShadowsRandomCoroutine()
    {
        _canSwitch = false;

        bool isFrightened = GameManager.Instance.IsFrightenedShadowState;

        var oldController = _shadows[_activeShadowIndex].GetComponent<ShadowController>();
        oldController.IsShadowActive = false;

        if (isFrightened)
            oldController.SetState(new ShadowFrightenedState());
        else
            oldController.SetState(new ShadowScatterState());

        int randomIndex = _activeShadowIndex;
        if (_shadows.Count > 1)
        {
            int attempts = 0;
            do
            {
                randomIndex = Random.Range(0, _shadows.Count);
                attempts++;
                if (attempts > 10) break;
            } while (randomIndex == _activeShadowIndex
                     || _shadows[randomIndex].GetComponent<ShadowController>().CurrentState.State == ShadowState.Eaten);
        }

        _activeShadowIndex = randomIndex;

        var newController = _shadows[_activeShadowIndex].GetComponent<ShadowController>();
        newController.IsShadowActive = true;
        newController.SetState(new ShadowActiveState());

        UpdateAppearance();

        yield return new WaitForSeconds(0.3f);
        _canSwitch = true;
    }

    private int GetClosestShadowIndex()
    {
        float closest = Mathf.Infinity;
        int index = -1;

        for (int i = 0; i < _shadows.Count; i++)
        {
            if (i == _activeShadowIndex) continue;

            var controller = _shadows[i].GetComponent<ShadowController>();
            if (controller.CurrentState.State == ShadowState.Eaten)
                continue;

            float d = Vector3.Distance(
                _shadows[_activeShadowIndex].transform.position,
                _shadows[i].transform.position);

            if (d < closest)
            {
                closest = d;
                index = i;
            }
        }

        return index;
    }


    #endregion

    
    #region Round Reset

    private void HandleRoundEnd(object sender, System.EventArgs e)
    {
        StopAllCoroutines();
        _canSwitch = true;
        _isRoundStarted = false;
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
        StartCoroutine(SpawnFirstShadow());
    }

    #endregion

    public ShadowController GetActiveGhostController() {
        var activeGhost = _shadows[_activeShadowIndex];
        return activeGhost.GetComponent<ShadowController>();
    }
}
