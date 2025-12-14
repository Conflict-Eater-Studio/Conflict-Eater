using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSelectUI : MonoBehaviour
{
    #region Progress Bar Properties
    [Header("Shared Progress Bar")]
    [SerializeField]
    private Slider _p1Progress;

    [SerializeField]
    private Slider _p2Progress;

    [SerializeField]
    private Texture2D _lightRawImage;

    [SerializeField]
    private Texture2D _shadowRawImage;

    [SerializeField]
    private RawImage _p1CharacterRawImage;

    [SerializeField]
    private RawImage _p2CharacterRawImage;

    [SerializeField]
    private float _progressBouceBackDuration = 2f;

    [SerializeField]
    private Ease _progressBouceBackEase = Ease.OutBack;
    #endregion

    #region Player Indicator Properties
    [Header("Player Button indicators")]
    [SerializeField]
    private RawImage _p1R2Image;

    [SerializeField]
    private RawImage _p1L2Image;

    [SerializeField]
    private TextMeshProUGUI _p1ShadowText;

    [SerializeField]
    private TextMeshProUGUI _p1LightText;

    [SerializeField]
    private RawImage _p2R2Image;

    [SerializeField]
    private RawImage _p2L2Image;

    [SerializeField]
    private TextMeshProUGUI _p2ShadowText;

    [SerializeField]
    private TextMeshProUGUI _p2LightText;

    [SerializeField]
    private Color _colorInactive = new Color(0.75f, 0.75f, 0.75f);

    [SerializeField]
    private Color _colorActive = Color.white;

    [SerializeField]
    private float _indicatorTransitionDuration = 0.25f;

    [SerializeField]
    private Ease _indicatorTransitionEase = Ease.InOutSine;
    #endregion

    #region Player Ready Indicators
    [Header("Player Ready Indicators")]
    [SerializeField]
    private Transform _p1ReadyTextTransform;

    [SerializeField]
    private Transform _p2ReadyTextTransform;

    [SerializeField]
    private TextMeshProUGUI _p1Text;

    [SerializeField]
    private TextMeshProUGUI _p2Text;

    [SerializeField]
    private float _playerTextTransitionDuration = 0.5f;

    [SerializeField]
    private Ease _playerTextTransitionEase = Ease.InOutSine;

    [SerializeField]
    private float _playerReadyScaleDuration = 0.5f;

    [SerializeField]
    private Ease _playerReadyScaleEase = Ease.InOutSine;
    #endregion

    [Header("General")]
    [SerializeField]
    private CanvasGroup _canvasGroup;

    [SerializeField]
    private PlayerSwapUI _playerSwapUI;

    private Dictionary<int, PlayerSpawner.PlayerRole> _playerRoles =
        new Dictionary<int, PlayerSpawner.PlayerRole>();
    private Dictionary<int, bool> _playerReady = new Dictionary<int, bool>();

    private Tween _p1Tween;
    private Tween _p2Tween;

    private PlayerSpawner.PlayerRole _p1IndicatorState = PlayerSpawner.PlayerRole.None;
    private PlayerSpawner.PlayerRole _p2IndicatorState = PlayerSpawner.PlayerRole.None;

    private void Start()
    {
        _p1ReadyTextTransform.gameObject.SetActive(false);
        _p2ReadyTextTransform.gameObject.SetActive(false);

        // Hide character images initially
        if (_p1CharacterRawImage != null)
            _p1CharacterRawImage.gameObject.SetActive(false);
        if (_p2CharacterRawImage != null)
            _p2CharacterRawImage.gameObject.SetActive(false);

        // Initialize progress bars
        if (_p1Progress != null)
        {
            _p1Progress.value = 0;
        }

        if (_p2Progress != null)
        {
            _p2Progress.value = 0;
        }

        // Initialize indicator visuals to neutral
        ApplyIndicatorStateInstant(0, PlayerSpawner.PlayerRole.None);
        ApplyIndicatorStateInstant(1, PlayerSpawner.PlayerRole.None);
    }

    private void OnEnable()
    {
        PlayerSpawner.OnRoleSelectionStarted += HandleRoleSelectionStarted;
        PlayerSpawner.OnRoleSelectionChanged += HandleRoleSelectionChanged;
        PlayerSpawner.OnRoleSelectionReleased += HandleRoleSelectionReleased;
        PlayerSpawner.OnPlayersReadyToSpawn += HandlePlayersReadyToSpawn;
    }

    private void OnDisable()
    {
        PlayerSpawner.OnRoleSelectionStarted -= HandleRoleSelectionStarted;
        PlayerSpawner.OnRoleSelectionChanged -= HandleRoleSelectionChanged;
        PlayerSpawner.OnRoleSelectionReleased -= HandleRoleSelectionReleased;
        PlayerSpawner.OnPlayersReadyToSpawn -= HandlePlayersReadyToSpawn;
    }

    private void OnDestroy()
    {
        PlayerSpawner.OnRoleSelectionStarted -= HandleRoleSelectionStarted;
        PlayerSpawner.OnRoleSelectionChanged -= HandleRoleSelectionChanged;
        PlayerSpawner.OnRoleSelectionReleased -= HandleRoleSelectionReleased;
        PlayerSpawner.OnPlayersReadyToSpawn -= HandlePlayersReadyToSpawn;
    }

    private void HandleRoleSelectionStarted(object sender, PlayerSpawner.RoleSelectionEventArgs e)
    {
        UpdateRoleSelectionVisuals(e.PlayerIndex, e.SelectedRole);
    }

    private void HandleRoleSelectionChanged(object sender, PlayerSpawner.RoleSelectionEventArgs e)
    {
        int playerIndex = e.PlayerIndex;
        if (playerIndex < 0 || playerIndex > 1)
            return;

        // Track which role this player is selecting
        _playerRoles[playerIndex] = e.SelectedRole;

        // Get the appropriate progress bar based on player
        Slider progressBar = null;
        RawImage characterImage = null;

        if (playerIndex == 0)
        {
            progressBar = _p1Progress;
            progressBar.fillRect.GetComponent<Image>().color = PlayerSpawner.RoleColors[
                e.SelectedRole
            ];
            _p1Tween?.Kill();
            characterImage = _p1CharacterRawImage;
        }
        else if (playerIndex == 1)
        {
            progressBar = _p2Progress;
            progressBar.fillRect.GetComponent<Image>().color = PlayerSpawner.RoleColors[
                e.SelectedRole
            ];
            _p2Tween?.Kill();
            characterImage = _p2CharacterRawImage;
        }

        // Update character image texture based on role
        if (characterImage != null)
        {
            characterImage.texture =
                e.SelectedRole == PlayerSpawner.PlayerRole.Light ? _lightRawImage : _shadowRawImage;
        }

        // Update progress bar
        if (progressBar != null)
        {
            progressBar.value = e.HoldProgress;

            // Position character image at the edge of the slider fill
            if (characterImage != null)
            {
                UpdateCharacterImagePosition(progressBar, characterImage, playerIndex);
            }
        }

        // Handle ready state
        Transform readyText = playerIndex == 0 ? _p1ReadyTextTransform : _p2ReadyTextTransform;
        TextMeshProUGUI playerText = playerIndex == 0 ? _p1Text : _p2Text;
        bool wasReady = _playerReady.ContainsKey(playerIndex) && _playerReady[playerIndex];

        if (e.IsConfirmed && !wasReady)
        {
            _playerReady[playerIndex] = true;
            if (readyText != null)
            {
                readyText.gameObject.SetActive(true);
                readyText.localScale = Vector3.zero;
                playerText
                    .DOColor(
                        playerIndex == 0
                            ? PlayerSpawner.RoleColors[e.SelectedRole]
                            : PlayerSpawner.RoleColors[e.SelectedRole],
                        _playerTextTransitionDuration
                    )
                    .SetEase(_playerTextTransitionEase);
                readyText
                    .DOScale(Vector3.one, _playerReadyScaleDuration)
                    .SetEase(_playerReadyScaleEase);
            }
        }

        if (!e.IsConfirmed && wasReady)
        {
            _playerReady[playerIndex] = false;
            if (readyText != null)
            {
                readyText.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateCharacterImagePosition(
        Slider slider,
        RawImage characterImage,
        int playerIndex
    )
    {
        if (slider == null || characterImage == null || slider.fillRect == null)
            return;

        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        RectTransform characterRect = characterImage.GetComponent<RectTransform>();

        // Get the fill area width in world space
        float sliderWidth = sliderRect.rect.width;
        float fillWidth = sliderWidth * slider.value;

        float characterHalfWidth = characterRect.rect.width * 0.5f;
        float finalPosition;

        // P1 fills from left to right, P2 fills from right to left
        if (playerIndex == 0)
        {
            float minX = -sliderWidth * sliderRect.pivot.x;
            float edgePosition = minX + fillWidth;
            finalPosition = edgePosition - characterHalfWidth;
        }
        else
        {
            float maxX = sliderWidth * (1 - sliderRect.pivot.x);
            float edgePosition = maxX - fillWidth;
            finalPosition = edgePosition + characterHalfWidth;
        }

        // Set the position and offset Y by half height to center on middle line
        float characterHalfHeight = characterRect.rect.height * 0.5f;
        Vector2 anchoredPos = characterRect.anchoredPosition;
        anchoredPos.x = finalPosition;
        anchoredPos.y = -characterHalfHeight;
        characterRect.anchoredPosition = anchoredPos;
    }

    private void UpdateRoleSelectionVisuals(int playerIndex, PlayerSpawner.PlayerRole selectedRole)
    {
        // Show character image when selection starts
        RawImage characterImage = playerIndex == 0 ? _p1CharacterRawImage : _p2CharacterRawImage;
        if (characterImage != null && !characterImage.gameObject.activeSelf)
        {
            characterImage.gameObject.SetActive(true);
        }

        // Decide target states for both players based on who started selection and which role
        if (selectedRole == PlayerSpawner.PlayerRole.Light)
        {
            if (playerIndex == 0)
            {
                SetIndicatorState(0, PlayerSpawner.PlayerRole.Light);
                SetIndicatorState(1, PlayerSpawner.PlayerRole.Shadow);
            }
            else if (playerIndex == 1)
            {
                SetIndicatorState(1, PlayerSpawner.PlayerRole.Light);
                SetIndicatorState(0, PlayerSpawner.PlayerRole.Shadow);
            }
        }
        else
        {
            if (playerIndex == 0)
            {
                SetIndicatorState(0, PlayerSpawner.PlayerRole.Shadow);
                SetIndicatorState(1, PlayerSpawner.PlayerRole.Light);
            }
            else if (playerIndex == 1)
            {
                SetIndicatorState(1, PlayerSpawner.PlayerRole.Shadow);
                SetIndicatorState(0, PlayerSpawner.PlayerRole.Light);
            }
        }
    }

    void HandleRoleSelectionReleased(object sender, PlayerSpawner.RoleSelectionReleasedEventArgs e)
    {
        int playerIndex = e.PlayerIndex;
        if (playerIndex < 0 || playerIndex > 1)
            return;

        Transform readyText = playerIndex == 0 ? _p1ReadyTextTransform : _p2ReadyTextTransform;

        // Reset the progress bar for the released role
        if (playerIndex == 0 && _p1Progress != null)
        {
            _p1Tween?.Kill();
            _p1Tween = DOTween
                .To(
                    () => _p1Progress.value,
                    x => _p1Progress.value = x,
                    0f,
                    _progressBouceBackDuration * _p1Progress.value // Scale duration based on current value
                )
                .SetEase(_progressBouceBackEase)
                .OnUpdate(() =>
                {
                    if (_p1CharacterRawImage != null && _p1CharacterRawImage.gameObject.activeSelf)
                    {
                        UpdateCharacterImagePosition(_p1Progress, _p1CharacterRawImage, 0);
                    }
                });
        }
        else if (playerIndex == 1 && _p2Progress != null)
        {
            _p2Tween?.Kill();
            _p2Tween = DOTween
                .To(
                    () => _p2Progress.value,
                    x => _p2Progress.value = x,
                    0f,
                    _progressBouceBackDuration * _p2Progress.value // Scale duration based on current value
                )
                .SetEase(_progressBouceBackEase)
                .OnUpdate(() =>
                {
                    if (_p2CharacterRawImage != null && _p2CharacterRawImage.gameObject.activeSelf)
                    {
                        UpdateCharacterImagePosition(_p2Progress, _p2CharacterRawImage, 1);
                    }
                });
        }

        // Clear ready state
        _playerReady[playerIndex] = false;
        if (readyText != null)
        {
            readyText.gameObject.SetActive(false);
        }

        // Clear tracked role
        if (_playerRoles.ContainsKey(playerIndex))
        {
            _playerRoles.Remove(playerIndex);
        }

        // Refresh visuals: if the other player is still selecting, reflect their state;
        // otherwise clear indicator visuals to neutral
        int other = playerIndex == 0 ? 1 : 0;
        if (_playerRoles.ContainsKey(other))
        {
            UpdateRoleSelectionVisuals(other, _playerRoles[other]);
        }
        else
        {
            // No one selecting — reset to neutral
            SetIndicatorState(0, PlayerSpawner.PlayerRole.None);
            SetIndicatorState(1, PlayerSpawner.PlayerRole.None);
        }
    }

    private void HandlePlayersReadyToSpawn(object sender, EventArgs e)
    {
        Sequence s = DOTween.Sequence();
        s.AppendInterval(3f);

        if (_canvasGroup != null)
        {
            // Fade out the entire UI if canvas group is assigned
            s.Append(
                _canvasGroup
                    .DOFade(0f, 0.5f)
                    .OnComplete(() =>
                    {
                        gameObject.SetActive(false);
                    })
            );
        }
        else
        {
            // If not, just deactivate after delay
            s.InsertCallback(
                3f,
                () =>
                {
                    gameObject.SetActive(false);
                }
            );
        }

        s.AppendCallback(() =>
        {
            // Show the initial role assignment for 3 seconds, then start the match
            if (_playerSwapUI != null)
            {
                // Get Player 1's role from PlayerSpawner
                PlayerSpawner spawner = FindFirstObjectByType<PlayerSpawner>();
                PlayerSpawner.PlayerRole p1Role =
                    spawner != null ? spawner.GetPlayer1Role() : PlayerSpawner.PlayerRole.Light;

                _playerSwapUI.ShowInitialAssignment(
                    p1Role,
                    displayDuration: 3f,
                    onComplete: () =>
                    {
                        // Spawn players before starting the match
                        if (
                            GameManager.Instance != null
                            && GameManager.Instance.PlayerSpawner != null
                        )
                        {
                            GameManager.Instance.PlayerSpawner.ExecutePlayerSpawn();
                        }

                        // Start the match countdown after displaying roles
                        if (GameManager.Instance != null && GameManager.Instance.Timer != null)
                        {
                            GameManager.Instance.Timer.StartMatch();
                        }
                    }
                );
            }
        });

        s.Play();
    }

    private void SetIndicatorState(int playerIndex, PlayerSpawner.PlayerRole newState)
    {
        if (playerIndex == 0)
        {
            if (_p1IndicatorState == newState)
                return;

            _p1IndicatorState = newState;
            switch (newState)
            {
                case PlayerSpawner.PlayerRole.Light:
                    _p1L2Image
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1LightText
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1R2Image
                        .DOColor(_colorInactive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1ShadowText
                        .DOColor(_colorInactive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    break;
                case PlayerSpawner.PlayerRole.Shadow:
                    _p1L2Image
                        .DOColor(_colorInactive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1LightText
                        .DOColor(_colorInactive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1R2Image
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1ShadowText
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    break;
                case PlayerSpawner.PlayerRole.None:
                default:
                    _p1L2Image
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1LightText
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1R2Image
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p1ShadowText
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    break;
            }
        }
        else if (playerIndex == 1)
        {
            if (_p2IndicatorState == newState)
                return;

            _p2IndicatorState = newState;
            switch (newState)
            {
                case PlayerSpawner.PlayerRole.Light:
                    _p2L2Image
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2LightText
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2R2Image
                        .DOColor(_colorInactive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2ShadowText
                        .DOColor(_colorInactive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    break;
                case PlayerSpawner.PlayerRole.Shadow:
                    _p2L2Image
                        .DOColor(_colorInactive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2LightText
                        .DOColor(_colorInactive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2R2Image
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2ShadowText
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    break;
                case PlayerSpawner.PlayerRole.None:
                default:
                    _p2L2Image
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2LightText
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2R2Image
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    _p2ShadowText
                        .DOColor(_colorActive, _indicatorTransitionDuration)
                        .SetEase(_indicatorTransitionEase);
                    break;
            }
        }
    }

    private void ApplyIndicatorStateInstant(int playerIndex, PlayerSpawner.PlayerRole state)
    {
        Color lightColor = _colorActive;
        Color shadowColor = _colorActive;

        if (state == PlayerSpawner.PlayerRole.Light)
        {
            lightColor = _colorActive;
            shadowColor = _colorInactive;
        }
        else if (state == PlayerSpawner.PlayerRole.Shadow)
        {
            lightColor = _colorInactive;
            shadowColor = _colorActive;
        }

        if (playerIndex == 0)
        {
            _p1L2Image.color = lightColor;
            _p1LightText.color = lightColor;
            _p1R2Image.color = shadowColor;
            _p1ShadowText.color = shadowColor;
            _p1IndicatorState = state;
        }
        else if (playerIndex == 1)
        {
            _p2L2Image.color = lightColor;
            _p2LightText.color = lightColor;
            _p2R2Image.color = shadowColor;
            _p2ShadowText.color = shadowColor;
            _p2IndicatorState = state;
        }
    }
}
