using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShadowSwitchUI : MonoBehaviour
{
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

    private bool _isSubscribed = false;
    private PlayerInput _playerInput;
    private const string SwitchClydeActionName = "SwitchClyde";
    private const string SwitchInkyActionName = "SwitchInky";

    private void Start()
    {
        _activeUI.SetActive(true);
    }

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

    private void OnClydeSwitch(InputAction.CallbackContext context)
    {
        StartCoroutine(FlashButton(_l1, 0.3f));
    }

    private void OnInkySwitch(InputAction.CallbackContext context)
    {
        StartCoroutine(FlashButton(_r1, 0.3f));
    }

    private IEnumerator FlashButton(GameObject button, float duration)
    {
        if (button == null) yield break;

        button.SetActive(true);
        yield return new WaitForSeconds(duration);
        button.SetActive(false);
    }

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

    private bool Approximately(Color a, Color b, float eps = 0.01f)
    {
        return Mathf.Abs(a.r - b.r) < eps &&
               Mathf.Abs(a.g - b.g) < eps &&
               Mathf.Abs(a.b - b.b) < eps;
    }
}
