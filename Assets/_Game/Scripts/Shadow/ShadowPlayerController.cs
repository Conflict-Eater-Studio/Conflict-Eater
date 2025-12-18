using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
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
    [SerializeField] private Material _laserMaterial;
    #endregion

    #region Private Fields
    private const string MoveActionName = "Move";
    private const string SwitchClydeActionName = "SwitchClyde";
    private const string SwitchInkyActionName = "SwitchInky";
    private const string PauseActionName = "Pause";
    private const string ShowMyPlayerActionName = "ShowMyPlayer";

    private List<ShadowType> shadowTypes = new List<ShadowType>();
    private readonly List<GameObject> _shadows = new();
    private PlayerInput _playerInput;
    private int _activeShadowIndex;
    private bool _canSwitch = true;
    private bool _isRoundStarted = false;

    [SerializeField] private Color _rbSwitchColor;
    [SerializeField] private Color _lbSwitchColor;
    [SerializeField] private Color _aSwitchColor;

    public Color RBSwitchColor => _rbSwitchColor;
    public Color LBSwitchColor => _lbSwitchColor;
    public Color ASwitchColor => _aSwitchColor;
    #endregion

    #region Events
    public event System.Action OnLBSwitchEvent;
    public event System.Action OnRBSwitchEvent;
    public event System.Action OnActiveRandomSwitch;
    #endregion

    #region Public Fields
    public IReadOnlyList<GameObject> Shadows => _shadows;
    #endregion

    #region VFX Cleanup
    private readonly List<GameObject> _roundVfxObjects = new();
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

            _playerInput.actions[SwitchClydeActionName].performed += OnLBSwitch;
            _playerInput.actions[SwitchInkyActionName].performed += OnRBSwitch;

            _playerInput.actions[ShowMyPlayerActionName].performed += OnShowMyPlayer;
        }

        GameManager.Instance.Timer.OnRoundStart += Timer_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnd += HandleRoundEnd;

        GameManager.Instance.Timer.OnMatchPause += Shadow_OnMatchPause;
        GameManager.Instance.Timer.OnMatchResume += Shadow_OnMatchResume;

        shadowTypes.Add(ShadowType.Blinky);
        shadowTypes.Add(ShadowType.Inky);
        shadowTypes.Add(ShadowType.Clyde);

        StartCoroutine(SpawnFirstShadow());
    }

    /// Handles cleanup and unsubscribes from events.
    /// </summary>
    private void OnDestroy()
    {
        if (_playerInput != null)
        {
            _playerInput.actions[MoveActionName].performed -= OnMove;

            _playerInput.actions[SwitchClydeActionName].performed -= OnLBSwitch;
            _playerInput.actions[SwitchInkyActionName].performed -= OnRBSwitch;

            _playerInput.actions[ShowMyPlayerActionName].performed -= OnShowMyPlayer;
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
        if (!_isRoundStarted)
            return;
        if (_shadows.Count == 0)
            return;

        Vector2 moveInput = context.ReadValue<Vector2>();
        var activeGhost = _shadows[_activeShadowIndex];
        activeGhost.GetComponent<ShadowController>().Move(moveInput);
    }

    /// <summary>
    /// Switches to Clyde if possible.
    /// </summary>
    private void OnLBSwitch(InputAction.CallbackContext context)
    {
        if (!_canSwitch || _shadows.Count == 0)
            return;

        OnLBSwitchEvent?.Invoke();

        SwitchToGhostByColor(_lbSwitchColor);
        Color pomA = _aSwitchColor;
        Color pomLB = _lbSwitchColor;

        _aSwitchColor = pomLB;
        _lbSwitchColor = pomA;

    }

    /// <summary>
    /// Switches to Inky if possible.
    /// </summary>
    private void OnRBSwitch(InputAction.CallbackContext context)
    {
        if (!_canSwitch || _shadows.Count == 0)
            return;

        OnRBSwitchEvent?.Invoke();

        SwitchToGhostByColor(_rbSwitchColor);

        Color pomA = _aSwitchColor;
        Color pomRB = _rbSwitchColor;

        _aSwitchColor = pomRB;
        _rbSwitchColor = pomA;
    }

    private void OnShowMyPlayer(InputAction.CallbackContext context)
    {
        ShadowAppearanceManager shadowAppearanceManager = _shadows[_activeShadowIndex].GetComponent<ShadowAppearanceManager>();

        shadowAppearanceManager.FlashLight();
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
        _lbSwitchColor = appearanceManager.clydeColor;
        _rbSwitchColor = appearanceManager.inkyColor;
        _aSwitchColor = appearanceManager.blinkyColor;

        appearanceManager.colorBeforeFrightened = appearanceManager.blinkyColor;
        appearanceManager.SetCustomColor(appearanceManager.blinkyColor);

        _shadows.Add(ghost);
        _activeShadowIndex = 0;

        yield return new WaitUntil(() => _isRoundStarted);

        controller.SetState(new ShadowActiveState());

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

            ShadowAppearanceManager appearanceManager =
                controller.GetComponent<ShadowAppearanceManager>();
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

            yield return new WaitForSeconds(_spawnDelay);
        }
    }

    #endregion

    #region Pause Handling
    /// <summary>
    /// Handles the pause input from the player. Toggles game pause state and loads/unloads the pause menu scene.
    /// </summary>
    public virtual void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (GameManager.Instance?.Timer != null)
        {
            if (
                GameManager.Instance.Timer.IsGamePaused
                && MenuManager.Instance.IsLastMenuOfType(MenuManager.Menu.Pause)
            )
            {
                GameManager.Instance.Timer.Resume();
                MenuManager.Instance.CloseLastSubMenu();
            }
            else if (
                !GameManager.Instance.Timer.IsGamePaused
                && !MenuManager.Instance.IsLastMenuOfType(MenuManager.Menu.Pause)
            )
            {
                GameManager.Instance.Timer.Pause();
                MenuManager.Instance.OpenSubMenu(MenuManager.Menu.Pause);
            }
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

    #region Switching Logic
    /// <summary>
    /// Switches control to a shadow with the specified color if possible.
    /// </summary>
    private void SwitchToGhostByColor(Color targetColor)
    {
        if (!_canSwitch || _shadows.Count == 0)
            return;

        int targetIndex = -1;

        for (int i = 0; i < _shadows.Count; i++)
        {
            var controller = _shadows[i].GetComponent<ShadowController>();
            var appearance = controller.GetComponent<ShadowAppearanceManager>();

            if (appearance.GetCurrentColor() == targetColor &&
                controller.CurrentState.State != ShadowState.Eaten)
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

        AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.SFX.EnemySwap);

        bool isFrightened = GameManager.Instance.IsFrightenedShadowState;

        var old = _shadows[_activeShadowIndex].GetComponent<ShadowController>();
        old.IsShadowActive = false;

        old.SetState(isFrightened ? new ShadowFrightenedState() : new ShadowScatterState());

        _activeShadowIndex = newIndex;

        var newController = _shadows[_activeShadowIndex].GetComponent<ShadowController>();
        newController.IsShadowActive = true;
        newController.SetState(new ShadowActiveState());

        StartCoroutine(PlaySwitchLaser(old, newController));

        yield return new WaitForSeconds(0.3f);
        _canSwitch = true;
    }

    private IEnumerator PlaySwitchLaser(ShadowController from, ShadowController to)
    {
        GameObject laserObj = new GameObject("SwitchLaser");
        _roundVfxObjects.Add(laserObj);
        laserObj.transform.position = from.transform.position;
        LineRenderer lr = laserObj.AddComponent<LineRenderer>();
        lr.sortingLayerName = "Player";

        lr.positionCount = 2;
        lr.startWidth = 0.04f;
        lr.endWidth = 0.04f;
        lr.numCapVertices = 0;
        lr.numCornerVertices = 0;

        lr.material = _laserMaterial;
        lr.material.color = Color.softRed;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.softRed, 0f), new GradientColorKey(Color.softRed, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        lr.colorGradient = gradient;

        Vector3 startPos = from.transform.position;
        Vector3 endPos = to.transform.position;

        GameObject lightObj = new GameObject("Laser2DLight");
        _roundVfxObjects.Add(lightObj);
        lightObj.transform.position = startPos;
        Light2D light2D = lightObj.AddComponent<Light2D>();
        light2D.lightType = Light2D.LightType.Point;
        light2D.color = Color.softRed;
        light2D.intensity = 12f;
        light2D.pointLightOuterRadius = 1f;

        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 wiggle = new Vector3(
                Mathf.Sin(t * 20f) * 0.02f,
                Mathf.Cos(t * 25f) * 0.02f,
                0f
            );

            lr.SetPosition(1, Vector3.Lerp(startPos, endPos, t) + wiggle);
            lr.SetPosition(0, Vector3.Lerp(startPos, endPos, t * 0.5f));

            if (light2D != null)
                light2D.transform.position = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        lr.SetPosition(0, endPos);
        lr.SetPosition(1, endPos);

        yield return new WaitForSeconds(0.05f);

        CleanupVfxObject(laserObj);
        CleanupVfxObject(lightObj);
    }

    private void CleanupVfxObject(GameObject obj)
    {
        if (obj == null)
            return;

        _roundVfxObjects.Remove(obj);
        Destroy(obj);
    }


    /// <summary>
    /// Randomly switches control to another shadow. Ensures it is not the current active shadow
    /// and that the shadow is not in the 'Eaten' state.
    /// </summary>
    public IEnumerator SwitchShadowsRandomCoroutine()
    {
        _canSwitch = false;

        AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.SFX.EnemySwap);

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
                if (attempts > 10)
                    break;
            } while (
                randomIndex == _activeShadowIndex
                || _shadows[randomIndex].GetComponent<ShadowController>().CurrentState.State
                    == ShadowState.Eaten
            );
        }

        _activeShadowIndex = randomIndex;

        var newController = _shadows[_activeShadowIndex].GetComponent<ShadowController>();
        newController.IsShadowActive = true;
        newController.SetState(new ShadowActiveState());

        ShadowAppearanceManager shadowAppearanceManager =
            newController.GetComponent<ShadowAppearanceManager>();
        shadowAppearanceManager.SetActive();

        if(shadowAppearanceManager.GetCurrentColor() == _rbSwitchColor)
        {
            Color pom = _aSwitchColor;

            _aSwitchColor = shadowAppearanceManager.GetCurrentColor();
            _rbSwitchColor = pom;
        } else if(shadowAppearanceManager.GetCurrentColor() == _lbSwitchColor)
        {
            Color pom = _aSwitchColor;

            _aSwitchColor = shadowAppearanceManager.GetCurrentColor();
            _lbSwitchColor = pom;
        }

        OnActiveRandomSwitch?.Invoke();

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
        _canSwitch = true;
        _isRoundStarted = false;
        GameManager.Instance.IsFrightenedShadowState = false;

        foreach (var vfx in _roundVfxObjects)
        {
            if (vfx != null)
                Destroy(vfx);
        }

        _roundVfxObjects.Clear();

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
    public ShadowController GetActiveGhostController()
    {
        var activeGhost = _shadows[_activeShadowIndex];
        return activeGhost.GetComponent<ShadowController>();
    }
    #endregion

    #region Setters
    public void SetLaserMaterial(Material material)
    {
        _laserMaterial = material;
    }
    #endregion
}
