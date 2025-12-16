using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine.UI;

[System.Serializable]
public class ShadowIndicatorSlot
{
    public int id;
    public ShadowIndicator indicator;
}

[System.Serializable]
public class ShadowIndicatorInitialState
{
    public int slotId;
    public ShadowIndicator indicator;
    public Vector3 position;
}

[System.Serializable]
public class ShadowIndicatorDebugInfo
{
    public int slotId;
    public Color currentColor;
}


/// <summary>
/// Handles the UI for switching between shadows (ghosts).
/// Shows indicators for active and switchable shadows, and flashes buttons when a switch occurs.
/// </summary>
public class ShadowSwitchUI : MonoBehaviour
{
    #region Inspector Fields
    [Header("UI Indicators")]
    [SerializeField] private List<ShadowIndicatorSlot> _shadowIndicators;
    private List<ShadowIndicatorInitialState> _initialState;

    [Header("UI Controlers")]
    [SerializeField] private GameObject _l1;
    [SerializeField] private GameObject _r1;

    [Header("DEBUG")]
    [SerializeField] private List<ShadowIndicatorDebugInfo> _debugIndicators;

    private float _colorTolerance = 0.005f;
    #endregion

    #region Properties
    private bool _isSubscribed = false;
    private PlayerInput _playerInput;
    private const string SwitchClydeActionName = "SwitchClyde";
    private const string SwitchInkyActionName = "SwitchInky";
    private ShadowPlayerController playerController;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        CacheInitialState();
    }

    private void Start()
    {
        GameManager.Instance.Timer.OnRoundEnd += Timer_OnRoundEnd;
    }

    private void Timer_OnRoundEnd(object sender, System.EventArgs e)
    {
        ResetIndicators();
    }

    private void CacheInitialState()
    {
        _initialState = new List<ShadowIndicatorInitialState>();

        foreach (var slot in _shadowIndicators)
        {
            if (slot.indicator == null) continue;

            _initialState.Add(new ShadowIndicatorInitialState
            {
                slotId = slot.id,
                indicator = slot.indicator,
                position = slot.indicator.transform.position
            });
        }
    }

    /// <summary>
    /// Per-frame check: subscribes to shadow events once the ShadowPlayerController is available.
    /// </summary>
    private void Update()
    {
        UpdateDebugInfo();

        if (_isSubscribed) return;

        PlayerManager player = GameManager.Instance.PlayerManager;
        if (player == null) return;

        ShadowPlayerController playerController = player.GetPlayerOfType(PlayerManager.PlayerType.Shadow)
                                                        ?.GetComponentInChildren<ShadowPlayerController>();
        if (playerController != null)
        {
            _isSubscribed = true;

            this.playerController = playerController;
            playerController.OnLBSwitchEvent += OnLBSwitch;
            playerController.OnRBSwitchEvent += OnRBSwitch;

            //playerController.OnActiveRandomSwitch += PlayerController_OnActiveRandomSwitch;
        }
    }

    private void UpdateDebugInfo()
    {
        if (_shadowIndicators == null) return;

        if (_debugIndicators == null)
            _debugIndicators = new List<ShadowIndicatorDebugInfo>();

        _debugIndicators.Clear();

        foreach (var slot in _shadowIndicators)
        {
            if (slot == null || slot.indicator == null) continue;

            _debugIndicators.Add(new ShadowIndicatorDebugInfo
            {
                slotId = slot.id,
                currentColor = slot.indicator.GetColor()
            });
        }
    }

    private void PlayerController_OnActiveRandomSwitch()
    {
        Color rbSwitchColor = playerController.RBSwitchColor;
        Color lbSwitchColor = playerController.LBSwitchColor;
        Color aSwitchColor = playerController.ASwitchColor;

        ShadowIndicatorSlot lbSlot = null;
        ShadowIndicatorSlot aSlot = null;
        ShadowIndicatorSlot rbSlot = null;

        foreach (var slot in _shadowIndicators)
        {
            if (slot?.indicator == null) continue;

            Color indicatorColor = slot.indicator.GetColor();

            if (ColorsEqual(indicatorColor, lbSwitchColor))
            {
                lbSlot = slot;
            }
            else if (ColorsEqual(indicatorColor, aSwitchColor))
            {
                aSlot = slot;
            }
            else if (ColorsEqual(indicatorColor, rbSwitchColor))
            {
                rbSlot = slot;
            }
        }

        if (lbSlot == null || aSlot == null || rbSlot == null)
        {
            Debug.LogWarning("Nie znaleziono wszystkich slotów kolorów!");
            return;
        }

        Debug.Log(
            $"[RandomSwitch] Input colors → " +
            $"LB: {ColorToString(lbSlot.indicator.GetColor())}, " +
            $"A: {ColorToString(aSlot.indicator.GetColor())}, " +
            $"RB: {ColorToString(rbSlot.indicator.GetColor())}"
        );

        ShadowIndicator lb = lbSlot.indicator;
        ShadowIndicator a = aSlot.indicator;
        ShadowIndicator rb = rbSlot.indicator;

        GetSlotById(0).indicator = lb;
        GetSlotById(1).indicator = a;
        GetSlotById(2).indicator = rb;

        SetIndicatorToSlotPosition(0);
        SetIndicatorToSlotPosition(1);
        SetIndicatorToSlotPosition(2);
    }
    private void SetIndicatorToSlotPosition(int slotId)
    {
        ShadowIndicatorSlot slot = GetSlotById(slotId);
        if (slot == null || slot.indicator == null) return;

        Vector3 targetPos = GetSlotPosition(slotId);

        slot.indicator.transform.position = targetPos;
    }

    private Vector3 GetSlotPosition(int slotId)
    {
        ShadowIndicatorInitialState state =
            _initialState.Find(s => s.slotId == slotId);

        return state != null ? state.position : Vector3.zero;
    }

    private string ColorToString(Color c)
    {
        return $"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";
    }

    private bool ColorsEqual(Color a, Color b)
    {
        return Vector3.Distance(
            new Vector3(a.r, a.g, a.b),
            new Vector3(b.r, b.g, b.b)
        ) <= _colorTolerance;
    }

    #endregion

    #region FlashButtons
    /// <summary>
    /// Called when the player triggers the Clyde switch action.
    /// Starts a coroutine to flash the corresponding UI button.
    /// </summary>
    private void OnLBSwitch()
    {
        SwapActiveColor(GetSlotById(0));
        StartCoroutine(FlashButton(_l1, 0.3f));
    }

    /// <summary>
    /// Called when the player triggers the Inky switch action.
    /// Starts a coroutine to flash the corresponding UI button.
    /// </summary>
    private void OnRBSwitch()
    {
        SwapActiveColor(GetSlotById(2));
        StartCoroutine(FlashButton(_r1, 0.3f));
    }

    /// <summary>
    /// Coroutine to flash a UI button for a short duration.
    /// </summary>
    /// <param name="button">The button GameObject to flash.</param>
    /// <param name="duration">How long the button stays visible.</param>
    private IEnumerator FlashButton(GameObject button, float duration)
    {
        if (button == null) yield break;

        button.SetActive(true);
        yield return new WaitForSeconds(duration);
        button.SetActive(false);
    }
    #endregion

    /// <summary>
    /// Swaps the currently active indicator with the given target indicator.
    /// </summary>
    /// <param name="target">The indicator whose color will be applied to the active indicator.</param>
    private void SwapActiveColor(ShadowIndicatorSlot target)
    {
        ShadowIndicatorSlot active = GetSlotById(1);

        if (active == null || target == null) return;

        Vector3 targetPos = new Vector3(target.indicator.transform.position.x, active.indicator.transform.position.y, active.indicator.transform.position.z);
        Vector3 activePos = new Vector3(active.indicator.transform.position.x, target.indicator.transform.position.y, target.indicator.transform.position.z);

        float duration = 0.25f;
        active.indicator.transform.DOMoveX(targetPos.x, duration).SetEase(Ease.InOutCubic);
        target.indicator.transform.DOMoveX(activePos.x, duration).SetEase(Ease.InOutCubic);

        StartCoroutine(SwapAfterDelay(active, target, duration));
    }

    private IEnumerator SwapAfterDelay(ShadowIndicatorSlot active, ShadowIndicatorSlot target, float delay)
    {
        if (active.indicator == null || target.indicator == null)
        {
            yield break;
        }

        yield return new WaitForSeconds(delay);

        ShadowIndicator temp = active.indicator;
        active.indicator = target.indicator;
        target.indicator = temp;
    }

    private ShadowIndicatorSlot GetSlotById(int id)
    {
        return _shadowIndicators.Find(s => s.id == id);
    }

    public void ResetIndicators()
    {
        if (_initialState == null || _initialState.Count == 0) return;

        float duration = 0.25f;

        foreach (var state in _initialState)
        {
            ShadowIndicatorSlot slot = GetSlotById(state.slotId);
            if (slot == null) continue;

            slot.indicator = state.indicator;

            state.indicator.transform.DOKill();
            state.indicator.transform.DOMove(state.position, duration)
                .SetEase(Ease.OutCubic);
        }
    }

    public void ResetIndicatorsInstant()
    {
        foreach (var state in _initialState)
        {
            ShadowIndicatorSlot slot = GetSlotById(state.slotId);
            if (slot == null) continue;

            slot.indicator = state.indicator;
            state.indicator.transform.position = state.position;
        }
    }

}
