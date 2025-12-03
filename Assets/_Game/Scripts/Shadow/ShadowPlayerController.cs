using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the player's control over multiple shadows/ghosts.
/// Manages input, spawning, switching between shadows, round events, and appearance updates.
/// </summary>
public class ShadowPlayerController : MonoBehaviour
{
    #region Inspector Fields
    [Header("Shadow Settings")]
    [SerializeField] private GameObject _shadowPrefab;
    [SerializeField] private int _shadowCount = 3;
    [SerializeField] private float _spawnDelay = 1f;

    [SerializeField] private bool _blinky = false;
    [SerializeField] private bool _pinky = false;
    [SerializeField] private bool _inky = false;
    [SerializeField] private bool _clyde = false;

    [SerializeField] private ShadowState _blintyState;
    [SerializeField] private ShadowState _pinkyState;
    [SerializeField] private ShadowState _inkyState;
    [SerializeField] private ShadowState _clydeState;

    [Header("Switch Distance Settings")]
    [SerializeField] private float maxSwitchDistanceToPlayer = 15f;

    [Header("Switch Particle")]
    [SerializeField] private GameObject _switchParticlePrefab;

    #endregion

    #region Events
    public delegate void ShadowDistanceChangedEvent(Color color, bool canSwitch);
    public event ShadowDistanceChangedEvent OnShadowDistanceChanged;
    #endregion

    #region Private Fields

    private const string MoveActionName = "Move";
    private const string SwitchClydeActionName = "SwitchClyde";
    private const string SwitchInkyActionName = "SwitchInky";
    private const string PauseActionName = "Pause";

    private List<ShadowType> shadowTypes = new List<ShadowType>();
    private readonly List<GameObject> _shadows = new();
    private PlayerInput _playerInput;
    private int _activeShadowIndex;
    private int _previousActiveIndex = -1;
    private bool _canSwitch = true;
    private bool _isRoundStarted = false;

    private Color activeColor = Color.red;
    private Color inactiveColor1 = new Color(1f, 0.5f, 0f);
    private Color inactiveColor2 = Color.cyan;

    private Dictionary<ShadowController, bool> _previousCanSwitchStates = new();
    private readonly List<GameObject> _activeParticles = new();
    #endregion

    #region Public Fields
    public IReadOnlyList<GameObject> Shadows => _shadows;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initializes player input, subscribes to timer events, and starts spawning shadows.
    /// </summary>
    private void Start()
    {
        _playerInput = GetComponentInParent<PlayerInput>();

        if (_playerInput != null)
        {
            _playerInput.actions[MoveActionName].performed += OnMove;
            _playerInput.actions[PauseActionName].performed += OnPause;

            _playerInput.actions[SwitchClydeActionName].performed += OnClydeSwitch;
            _playerInput.actions[SwitchInkyActionName].performed += OnInkySwitch;
        }


        _switchParticlePrefab = GameManager.Instance.ParticleSystem;
        GameManager.Instance.Timer.OnRoundStart += Timer_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnd += HandleRoundEnd;

        GameManager.Instance.Timer.OnMatchPause += Shadow_OnMatchPause;
        GameManager.Instance.Timer.OnMatchResume += Shadow_OnMatchResume;

        shadowTypes.Add(ShadowType.Blinky);
        shadowTypes.Add(ShadowType.Inky);
        shadowTypes.Add(ShadowType.Clyde);

        StartCoroutine(SpawnFirstShadow());
    }

    /// <summary>
    /// Updates shadow states and checks distances for switch logic.
    /// </summary>
    private void Update()
    {
        if (_shadows.Count < 3) return;

        _blinky = _shadows[0].GetComponent<ShadowController>().IsShadowActive;
        _inky = _shadows[1].GetComponent<ShadowController>().IsShadowActive;
        _clyde = _shadows[2].GetComponent<ShadowController>().IsShadowActive;

        _blintyState = _shadows[0].GetComponent<ShadowController>().CurrentState.State;
        _inkyState = _shadows[1].GetComponent<ShadowController>().CurrentState.State;
        _clydeState = _shadows[2].GetComponent<ShadowController>().CurrentState.State;

        CheckShadowDistance();
    }

    /// <summary>
    /// Handles cleanup and unsubscribes from events.
    /// </summary>
    private void OnDestroy()
    {
        if (_playerInput != null)
        {
            _playerInput.actions[MoveActionName].performed -= OnMove;

            _playerInput.actions[SwitchClydeActionName].performed -= OnClydeSwitch;
            _playerInput.actions[SwitchInkyActionName].performed -= OnInkySwitch;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Timer.OnRoundStart -= Timer_OnRoundStart;
            GameManager.Instance.Timer.OnRoundEnd -= HandleRoundEnd;

            GameManager.Instance.Timer.OnMatchPause -= Shadow_OnMatchPause;
            GameManager.Instance.Timer.OnMatchResume -= Shadow_OnMatchResume;
        }
    }
    #endregion

    #region Input Handling
    /// <summary>
    /// Processes movement input and moves the currently active shadow.
    /// </summary>
    private void OnMove(InputAction.CallbackContext context)
    {
        if (!_isRoundStarted) return;
        if (_shadows.Count == 0) return;

        Vector2 moveInput = context.ReadValue<Vector2>();
        var activeGhost = _shadows[_activeShadowIndex];
        activeGhost.GetComponent<ShadowController>().Move(moveInput);
    }

    /// <summary>
    /// Switches to Clyde if possible.
    /// </summary>
    private void OnClydeSwitch(InputAction.CallbackContext context)
    {
        if (!_canSwitch || _shadows.Count == 0)
            return;

        Color clydeColor = _shadows[_activeShadowIndex].GetComponent<ShadowAppearanceManager>().clydeColor;
        SwitchToGhostByColor(clydeColor);
    }

    /// <summary>
    /// Switches to Inky if possible.
    /// </summary>
    private void OnInkySwitch(InputAction.CallbackContext context)
    {
        if (!_canSwitch || _shadows.Count == 0)
            return;

        Color inkyColor = _shadows[_activeShadowIndex].GetComponent<ShadowAppearanceManager>().inkyColor;
        SwitchToGhostByColor(inkyColor);
    }
    #endregion

    #region Shadow Spawning
    public void SetShadowPrefab(GameObject gameObject)
    {
        _shadowPrefab = gameObject;
    }

    /// <summary>
    /// Spawns the first shadow and sets it active.
    /// </summary>
    private IEnumerator SpawnFirstShadow()
    {
        var spawnPosition = new Vector3(-0.5f, -0.5f, 0f);
        var ghost = Instantiate(_shadowPrefab, spawnPosition, Quaternion.identity);
        ghost.transform.SetParent(transform);
        ghost.name = "Ghost_1";

        var controller = ghost.GetComponent<ShadowController>();
        controller.Owner = this;
        controller.SetShadowType(shadowTypes[0]);
        controller.IsShadowActive = true;

        ShadowAppearanceManager appearanceManager = controller.GetComponent<ShadowAppearanceManager>();
        activeColor = appearanceManager.activeColor;
        inactiveColor1 = appearanceManager.clydeColor;
        inactiveColor2 = appearanceManager.inkyColor;

        appearanceManager.colorBeforeFrightened = activeColor;
        appearanceManager.SetCustomColor(activeColor);

        _shadows.Add(ghost);
        _activeShadowIndex = 0;
        UpdateAppearance();

        yield return new WaitUntil(() => _isRoundStarted);

        controller.SetState(new ShadowExitBaseState());

        StartCoroutine(SpawnRemainingShadows());
    }

    /// <summary>
    /// Spawns remaining shadows after the first with delay.
    /// </summary>
    private IEnumerator SpawnRemainingShadows()
    {
        for (int i = 1; i < _shadowCount; i++)
        {
            var spawnPosition = new Vector3(-0.5f, -0.5f, 0f);
            var ghost = Instantiate(_shadowPrefab, spawnPosition, Quaternion.identity);
            ghost.transform.SetParent(transform);
            ghost.name = $"Ghost_{i + 1}";

            var controller = ghost.GetComponent<ShadowController>();
            controller.SetShadowType(shadowTypes[(i % _shadowCount)]);
            controller.IsShadowActive = false;
            controller.SetState(new ShadowExitBaseState());

            ShadowAppearanceManager appearanceManager = controller.GetComponent<ShadowAppearanceManager>();
            ShadowType type = controller.Type;

            if (type == ShadowType.Inky)
            {
                appearanceManager.colorBeforeFrightened = appearanceManager.inkyColor;
                appearanceManager.SetCustomColor(appearanceManager.inkyColor);
            }

            if (type == ShadowType.Clyde)
            {
                appearanceManager.colorBeforeFrightened = appearanceManager.clydeColor;
                appearanceManager.SetCustomColor(appearanceManager.clydeColor);
            }

            _shadows.Add(ghost);

            UpdateAppearance();

            yield return new WaitForSeconds(_spawnDelay);
        }
    }

    #endregion

    #region Pause Handling
    /// <summary>
    /// Handles the pause input from the player. Toggles game pause state and loads/unloads the pause menu scene.
    /// </summary>
    private void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

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

    /// <summary>
    /// Called when the match is resumed. Unlocks movement for all shadow objects.
    /// </summary>
    private void Shadow_OnMatchResume(object sender, System.EventArgs e)
    {
        _shadows.ForEach(shadow =>
        {
            shadow.GetComponent<ShadowController>().Movement.IsLocked = false;
        });
    }

    /// <summary>
    /// Called when the match is paused. Locks movement for all shadow objects.
    /// </summary>
    private void Shadow_OnMatchPause(object sender, System.EventArgs e)
    {
        _shadows.ForEach(shadow =>
        {
            shadow.GetComponent<ShadowController>().Movement.IsLocked = true;
        });
    }

    /// <summary>
    /// Called when a new round starts. Flags the round as started, enabling shadow input and behavior.
    /// </summary>
    private void Timer_OnRoundStart(object sender, System.EventArgs e)
    {
        _isRoundStarted = true;
    }
    #endregion

    #region Distance Checking
    /// <summary>
    /// Checks distance between active shadow and others to enable switching.
    /// </summary>
    private void CheckShadowDistance()
    {
        if (_shadows.Count == 0) return;

        var activeController = GetActiveGhostController();
        if (activeController == null) return;

        Vector3 activePos = activeController.transform.position;

        for (int i = 0; i < _shadows.Count; i++)
        {
            var shadow = _shadows[i];
            var controller = shadow.GetComponent<ShadowController>();

            if (controller == activeController)
                continue;

            if (controller.CurrentState.State == ShadowState.Eaten)
                continue;

            float distance = Vector3.Distance(activePos, shadow.transform.position);
            bool canSwitchNow = distance <= maxSwitchDistanceToPlayer;

            Color color = controller.GetComponent<ShadowAppearanceManager>().GetCurrentColor();

            bool previousCanSwitch;
            _previousCanSwitchStates.TryGetValue(controller, out previousCanSwitch);

            if (previousCanSwitch != canSwitchNow)
            {
                controller.CanBeSwitchedTo = canSwitchNow;
                OnShadowDistanceChanged?.Invoke(color, canSwitchNow);
            }

            _previousCanSwitchStates[controller] = canSwitchNow;
        }
    }
    #endregion

    #region Appearance
    /// <summary>
    /// Updates appearance for active and inactive shadows.
    /// </summary>
    public void UpdateAppearance()
    {
        if (_shadows.Count != 3) return;

        var activeAppearance = _shadows[_activeShadowIndex].GetComponent<ShadowAppearanceManager>();
        activeAppearance.SetCustomColor(activeColor);
        activeAppearance.colorBeforeFrightened = activeColor;

        if (_previousActiveIndex != -1 && _previousActiveIndex != _activeShadowIndex)
        {
            List<Color> existingColors = new List<Color>();
            for (int i = 0; i < _shadows.Count; i++)
            {
                if (i == _activeShadowIndex) continue;
                var appearance = _shadows[i].GetComponent<ShadowAppearanceManager>();
                existingColors.Add(appearance.GetCurrentColor());
            }

            Color missingColor = inactiveColor1;
            if (existingColors.Contains(inactiveColor1))
                missingColor = inactiveColor2;

            var previousAppearance = _shadows[_previousActiveIndex].GetComponent<ShadowAppearanceManager>();
            previousAppearance.SetCustomColor(missingColor);
            previousAppearance.colorBeforeFrightened = missingColor;
        }

        _previousActiveIndex = _activeShadowIndex;
    }

    #endregion

    #region Switching Logic
    /// <summary>
    /// Switches control to a shadow with the specified color if possible.
    /// </summary>
    private void SwitchToGhostByColor(Color targetColor)
    {
        if (!_canSwitch || _shadows.Count == 0) return;

        int targetIndex = -1;

        for (int i = 0; i < _shadows.Count; i++)
        {
            var controller = _shadows[i].GetComponent<ShadowController>();
            var appearance = controller.GetComponent<ShadowAppearanceManager>();

            if (appearance.GetCurrentColor() == targetColor &&
                controller.CurrentState.State != ShadowState.Eaten &&
                controller.CanBeSwitchedTo)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex == -1)
        {
            return;
        }

        StartCoroutine(SwitchToIndexCoroutine(targetIndex));
    }

    /// <summary>
    /// Coroutine that handles switching control to a shadow at a given index.
    /// Updates states, activates the new shadow, and triggers switch particles.
    /// </summary>
    private IEnumerator SwitchToIndexCoroutine(int newIndex)
    {
        _canSwitch = false;

        bool isFrightened = GameManager.Instance.IsFrightenedShadowState;

        var old = _shadows[_activeShadowIndex].GetComponent<ShadowController>();
        old.IsShadowActive = false;

        old.SetState(isFrightened ? new ShadowFrightenedState()
                                  : new ShadowScatterState());

        _previousActiveIndex = _activeShadowIndex;
        _activeShadowIndex = newIndex;

        var newController = _shadows[_activeShadowIndex].GetComponent<ShadowController>();
        newController.IsShadowActive = true;
        newController.SetState(new ShadowActiveState());

        StartCoroutine(PlaySwitchParticle(old, newController, activeColor));

        yield return new WaitForSeconds(0.3f);
        _canSwitch = true;
    }

    /// <summary>
    /// Plays a particle effect traveling from the old shadow to the new shadow when switching.
    /// </summary>
    /// <param name="from">The shadow being switched from.</param>
    /// <param name="to">The shadow being switched to.</param>
    /// <param name="color">The color to apply to the particle effect.</param>
    private IEnumerator PlaySwitchParticle(ShadowController from, ShadowController to, Color color)
    {
        GameObject psObj = Instantiate(_switchParticlePrefab, from.transform.position, Quaternion.identity);
        ParticleSystem ps = psObj.GetComponent<ParticleSystem>();

        Vector3 startPos = from.transform.position;
        Vector3 endPos = to.transform.position;

        Vector3 direction = (endPos - startPos).normalized;
        psObj.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        _activeParticles.Add(psObj);

        float duration = 0.5f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            psObj.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        UpdateAppearance();
        yield return new WaitForSeconds(1f);
        _activeParticles.Remove(psObj);
        Destroy(psObj);
    }

    /// <summary>
    /// Randomly switches control to another shadow. Ensures it is not the current active shadow
    /// and that the shadow is not in the 'Eaten' state.
    /// </summary>
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

        ShadowAppearanceManager shadowAppearanceManager = newController.GetComponent<ShadowAppearanceManager>();
        shadowAppearanceManager.SetActive();

        yield return new WaitForSeconds(0.3f);
        _canSwitch = true;
    }

    #endregion

    #region Round Reset
    /// <summary>
    /// Handles the end of the round by resetting the shadow system:
    /// stops coroutines, clears active particles, destroys existing shadows,
    /// resets indices and flags, and starts respawning shadows after a short delay.
    /// </summary>
    private void HandleRoundEnd(object sender, System.EventArgs e)
    {
        StopAllCoroutines();
        _previousActiveIndex = -1;
        _canSwitch = true;
        _isRoundStarted = false;
        GameManager.Instance.IsFrightenedShadowState = false;

        foreach (var p in _activeParticles)
        {
            if (p != null)
                Destroy(p);
        }
        _activeParticles.Clear();

        foreach (var ghost in _shadows)
        {
            if (ghost != null)
                Destroy(ghost);
        }

        _shadows.Clear();
        _activeShadowIndex = 0;

        StartCoroutine(RespawnAfterDelay(0.25f));
    }

    /// <summary>
    /// Coroutine that waits for a specified delay and then starts spawning the first shadow.
    /// </summary>
    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(SpawnFirstShadow());
    }

    #endregion

    #region Getters
    /// <summary>
    /// Returns the currently active shadow's controller.
    /// </summary>
    public ShadowController GetActiveGhostController() {
        var activeGhost = _shadows[_activeShadowIndex];
        return activeGhost.GetComponent<ShadowController>();
    }
    #endregion
}
