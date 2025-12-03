using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Handles the UI for switching between shadows (ghosts).
/// Shows indicators for active and switchable shadows, and flashes buttons when a switch occurs.
/// </summary>
public class ShadowSwitchUI : MonoBehaviour
{
    #region Inspector Fields
    [Header("UI Indicators")]
    [SerializeField] private ShadowIndicator _activeUI;
    [SerializeField] private ShadowIndicator _inkyUI;
    [SerializeField] private ShadowIndicator _clydeUI;

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
    /// Initialization: sets the active shadow indicator to visible.
    /// </summary>
    private void Start()
    {
        _activeUI.SetActive(true);
    }

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
            playerController.OnShadowDistanceChanged += HandleShadowDistanceChanged;
            _isSubscribed = true;

            _playerInput = playerController.GetComponentInParent<PlayerInput>();

            if (_playerInput != null)
            {
                _playerInput.actions[SwitchClydeActionName].performed += OnClydeSwitch;
                _playerInput.actions[SwitchInkyActionName].performed += OnInkySwitch;
            }
        }
    }

    #endregion

    #region FlashButtons
    /// <summary>
    /// Called when the player triggers the Clyde switch action.
    /// Starts a coroutine to flash the corresponding UI button.
    /// </summary>
    private void OnClydeSwitch(InputAction.CallbackContext context)
    {
        StartCoroutine(FlashButton(_l1, 0.3f));
    }

    /// <summary>
    /// Called when the player triggers the Inky switch action.
    /// Starts a coroutine to flash the corresponding UI button.
    /// </summary>
    private void OnInkySwitch(InputAction.CallbackContext context)
    {
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

    #region Logic Distance Changed
    /// <summary>
    /// Updates the UI indicators when the shadow's switch availability changes.
    /// </summary>
    /// <param name="shadowColor">Color of the shadow whose state changed.</param>
    /// <param name="canSwitch">Whether switching to this shadow is currently allowed.</param>
    private void HandleShadowDistanceChanged(Color shadowColor, bool canSwitch)
    {
        if (Approximately(shadowColor, inkyColor))
        {
            _inkyUI.SetAvailable(canSwitch);
        }
        
        if (Approximately(shadowColor, clydeColor))
        {
            _clydeUI.SetAvailable(canSwitch);
        }
    }

    /// <summary>
    /// Compares two colors approximately, ignoring small floating-point differences.
    /// </summary>
    /// <param name="a">First color.</param>
    /// <param name="b">Second color.</param>
    /// <param name="eps">Tolerance value for comparison.</param>
    /// <returns>True if colors are approximately equal.</returns>
    private bool Approximately(Color a, Color b, float eps = 0.01f)
    {
        return Mathf.Abs(a.r - b.r) < eps &&
               Mathf.Abs(a.g - b.g) < eps &&
               Mathf.Abs(a.b - b.b) < eps;
    }
    #endregion
}
