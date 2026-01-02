using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField]
    private List<ShadowIndicatorSlot> _shadowIndicators;
    private List<ShadowIndicatorInitialState> _initialState;

    [Header("UI Controlers")]
    [SerializeField]
    private GameObject _l1;

    [SerializeField]
    private GameObject _r1;

    [Header("DEBUG")]
    [SerializeField]
    private List<ShadowIndicatorDebugInfo> _debugIndicators;
    #endregion

    #region Properties
    private bool _isSubscribed = false;
    private bool _isShadowLinked = false;
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
            if (slot.indicator == null)
                continue;

            _initialState.Add(
                new ShadowIndicatorInitialState
                {
                    slotId = slot.id,
                    indicator = slot.indicator,
                    position = slot.indicator.transform.position,
                }
            );
        }
    }

    /// <summary>
    /// Per-frame check: subscribes to shadow events once the ShadowPlayerController is available.
    /// </summary>
    private void Update()
    {
        //PrintEatenShadows();

        UpdateDebugInfo();

        if (_isSubscribed && _isShadowLinked)
            return;

        PlayerManager player = GameManager.Instance.PlayerManager;
        if (player == null)
            return;

        ShadowPlayerController playerController = player
            .GetPlayerOfType(PlayerManager.PlayerRole.Skull)
            ?.GetComponentInChildren<ShadowPlayerController>();
        if (playerController != null && !_isSubscribed)
        {
            _isSubscribed = true;

            this.playerController = playerController;
            playerController.OnLBSwitchEvent += OnLBSwitch;
            playerController.OnRBSwitchEvent += OnRBSwitch;

            playerController.OnActiveRandomSwitch += PlayerController_OnActiveRandomSwitch;
        }

        if (playerController != null && !_isShadowLinked)
        {
            if(playerController.Shadows != null && playerController.Shadows.Count == 3)
            {
                GetSlotById(0).indicator.LinkedShadow = playerController.Shadows[2]; 
                GetSlotById(1).indicator.LinkedShadow = playerController.Shadows[0]; 
                GetSlotById(2).indicator.LinkedShadow = playerController.Shadows[1]; 

                Debug.Log("Is linked");

                _isShadowLinked = true;
            }
        }
    }

    private void UpdateDebugInfo()
    {
        if (_shadowIndicators == null)
            return;

        if (_debugIndicators == null)
            _debugIndicators = new List<ShadowIndicatorDebugInfo>();

        _debugIndicators.Clear();

        foreach (var slot in _shadowIndicators)
        {
            if (slot == null || slot.indicator == null)
                continue;

            _debugIndicators.Add(
                new ShadowIndicatorDebugInfo
                {
                    slotId = slot.id,
                    currentColor = slot.indicator.GetColor(),
                }
            );
        }
    }

    private void PlayerController_OnActiveRandomSwitch()
    {
        Color activeColor = playerController.ASwitchColor;

        int activeSlotIndex = -1;

        for (int i = 0; i < _shadowIndicators.Count; i++)
        {
            if (_shadowIndicators[i].indicator.GetColor() == activeColor)
            {
                activeSlotIndex = i;
                break;
            }
        }

        if (activeSlotIndex == -1)
        {
            Debug.LogWarning("Active shadow not found in UI indicators");
            return;
        }

        int steps = 1 - activeSlotIndex;

        if (steps == 0)
            return;

        RotateIndicators(steps > 0 ? +1 : -1);
    }


    private Vector3 GetSlotPosition(int slotId)
    {
        ShadowIndicatorInitialState state = _initialState.Find(s => s.slotId == slotId);

        return state != null ? state.position : Vector3.zero;
    }

    #endregion

    #region FlashButtons
    /// <summary>
    /// Called when the player triggers the Clyde switch action.
    /// Starts a coroutine to flash the corresponding UI button.
    /// </summary>
    private void OnLBSwitch()
    {
        RotateIndicators(+1);
        StartCoroutine(FlashButton(_l1, 0.3f));
    }

    /// <summary>
    /// Called when the player triggers the Inky switch action.
    /// Starts a coroutine to flash the corresponding UI button.
    /// </summary>
    private void OnRBSwitch()
    {
        RotateIndicators(-1);
        StartCoroutine(FlashButton(_r1, 0.3f));
    }

    private void RotateIndicators(int direction)
    {
        float duration = 0.25f;

        int count = _shadowIndicators.Count;
        if (count <= 1)
            return;

        Vector3[] positions = new Vector3[count];
        ShadowIndicator[] indicators = new ShadowIndicator[count];

        for (int i = 0; i < count; i++)
        {
            positions[i] = _shadowIndicators[i].indicator.transform.position;
            indicators[i] = _shadowIndicators[i].indicator;
        }

        for (int i = 0; i < count; i++)
        {
            int targetIndex = (i + direction + count) % count;
            indicators[i].transform.DOMove(positions[targetIndex], duration)
                .SetEase(Ease.InOutCubic);
        }

        StartCoroutine(ApplyRotationAfterDelay(indicators, direction, duration));
    }

    private IEnumerator ApplyRotationAfterDelay(
    ShadowIndicator[] indicators,
    int direction,
    float delay
)
    {
        yield return new WaitForSeconds(delay);

        int count = _shadowIndicators.Count;
        ShadowIndicator[] rotated = new ShadowIndicator[count];

        for (int i = 0; i < count; i++)
        {
            int targetIndex = (i + direction + count) % count;
            rotated[targetIndex] = indicators[i];
        }

        for (int i = 0; i < count; i++)
        {
            _shadowIndicators[i].indicator = rotated[i];
        }
    }


    /// <summary>
    /// Coroutine to flash a UI button for a short duration.
    /// </summary>
    /// <param name="button">The button GameObject to flash.</param>
    /// <param name="duration">How long the button stays visible.</param>
    private IEnumerator FlashButton(GameObject button, float duration)
    {
        if (button == null)
            yield break;

        button.SetActive(true);
        yield return new WaitForSeconds(duration);
        button.SetActive(false);
    }
    #endregion

    private ShadowIndicatorSlot GetSlotById(int id)
    {
        return _shadowIndicators.Find(s => s.id == id);
    }

    public void ResetIndicators()
    {
        if (_initialState == null || _initialState.Count == 0)
            return;

        float duration = 0.25f;

        foreach (var state in _initialState)
        {
            ShadowIndicatorSlot slot = GetSlotById(state.slotId);
            if (slot == null)
                continue;

            slot.indicator = state.indicator;

            state.indicator.transform.DOKill();
            state.indicator.transform.DOMove(state.position, duration).SetEase(Ease.OutCubic);
        }
    }

    public void ResetIndicatorsInstant()
    {
        foreach (var state in _initialState)
        {
            ShadowIndicatorSlot slot = GetSlotById(state.slotId);
            if (slot == null)
                continue;

            slot.indicator = state.indicator;
            state.indicator.transform.position = state.position;
        }
    }

    private bool IsShadowEaten(ShadowIndicator indicator)
    {
        if (indicator == null)
            return true;

        // Get the shadow GameObject linked to this indicator
        GameObject shadowGO = indicator.LinkedShadow; // <-- You need a reference in ShadowIndicator
        if (shadowGO == null)
            return true;

        var controller = shadowGO.GetComponent<ShadowController>();
        return controller == null || controller.CurrentState.State == ShadowState.Eaten;
    }

    public void PrintEatenShadows()
    {
        if (_shadowIndicators == null || _shadowIndicators.Count == 0)
        {
            Debug.Log("No shadow indicators available.");
            return;
        }

        Debug.Log("=== Shadow Eaten Status ===");

        foreach (var slot in _shadowIndicators)
        {
            if (slot == null || slot.indicator == null)
                continue;

            string shadowName = slot.indicator.LinkedShadow != null ? slot.indicator.LinkedShadow.name : "NoLinkedShadow";

            bool eaten = IsShadowEaten(slot.indicator);
            Debug.Log($"Slot {slot.id} ({shadowName}) is {(eaten ? "EATEN" : "Alive")}");
        }
    }

}
