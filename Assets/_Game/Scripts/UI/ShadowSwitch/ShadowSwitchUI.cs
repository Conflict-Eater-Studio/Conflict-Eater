using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

[System.Serializable]
public class ShadowIndicatorSlot
{
    public int id;
    public ShadowIndicator indicator;
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

    [Header("UI Controlers")]
    [SerializeField] private GameObject _l1;
    [SerializeField] private GameObject _r1;

    [Header("Colors")]
    public Color blinkyColor = Color.red;
    public Color inkyColor = Color.cyan;
    public Color clydeColor = new Color(1f, 0.7f, 0.3f);
    #endregion

    #region Properties
    private bool _isSubscribed = false;
    private PlayerInput _playerInput;
    private const string SwitchClydeActionName = "SwitchClyde";
    private const string SwitchInkyActionName = "SwitchInky";
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Per-frame check: subscribes to shadow events once the ShadowPlayerController is available.
    /// </summary>
    private void Update()
    {
        if (_isSubscribed) return;

        PlayerManager player = GameManager.Instance.PlayerManager;
        if (player == null) return;

        ShadowPlayerController playerController = player.GetPlayerOfType(PlayerManager.PlayerType.Shadow)
                                                        ?.GetComponentInChildren<ShadowPlayerController>();
        if (playerController != null)
        {
            _isSubscribed = true;

            playerController.OnLBSwitchEvent += OnLBSwitch;
            playerController.OnRBSwitchEvent += OnRBSwitch;
        }
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
        yield return new WaitForSeconds(delay);

        ShadowIndicator temp = active.indicator;
        active.indicator = target.indicator;
        target.indicator = temp;
    }

    private ShadowIndicatorSlot GetSlotById(int id)
    {
        return _shadowIndicators.Find(s => s.id == id);
    }
}
