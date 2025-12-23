using System;
using System.Collections.Generic;
using System.Linq;
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

    private Dictionary<PlayerManager.PlayerIndex, PlayerManager.PlayerRole> _playerRoles =
        new Dictionary<PlayerManager.PlayerIndex, PlayerManager.PlayerRole>();
    private Dictionary<PlayerManager.PlayerIndex, bool> _playerReady =
        new Dictionary<PlayerManager.PlayerIndex, bool>();

    private Tween _p1Tween;
    private Tween _p2Tween;

    private PlayerManager.PlayerRole _p1IndicatorState = PlayerManager.PlayerRole.None;
    private PlayerManager.PlayerRole _p2IndicatorState = PlayerManager.PlayerRole.None;

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
        ApplyIndicatorStateInstant(0, PlayerManager.PlayerRole.None);
        ApplyIndicatorStateInstant(1, PlayerManager.PlayerRole.None);
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
        UpdateRoleSelectionVisuals(e.PlayerIndex, e.PlayerRole);
    }

    private void HandleRoleSelectionChanged(object sender, PlayerSpawner.RoleSelectionEventArgs e)
    {
        PlayerManager.PlayerIndex playerIndex = e.PlayerIndex;
        if (
            playerIndex != PlayerManager.PlayerIndex.P1
            && playerIndex != PlayerManager.PlayerIndex.P2
        )
            return;

        // Track which role this player is selecting
        _playerRoles[playerIndex] = e.PlayerRole;

        // Get the appropriate progress bar based on player
        Slider progressBar = null;
        RawImage characterImage = null;

        if (playerIndex == PlayerManager.PlayerIndex.P1)
        {
            progressBar = _p1Progress;
            progressBar.fillRect.GetComponent<Image>().color = PlayerSpawner.RoleColors[
                e.PlayerRole
            ];
            _p1Tween?.Kill();
            characterImage = _p1CharacterRawImage;
        }
        else if (playerIndex == PlayerManager.PlayerIndex.P2)
        {
            progressBar = _p2Progress;
            progressBar.fillRect.GetComponent<Image>().color = PlayerSpawner.RoleColors[
                e.PlayerRole
            ];
            _p2Tween?.Kill();
            characterImage = _p2CharacterRawImage;
        }

        // Update character image texture based on role
        if (characterImage != null)
        {
            characterImage.texture =
                e.PlayerRole == PlayerManager.PlayerRole.Light ? _lightRawImage : _shadowRawImage;
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
        Transform readyText =
            playerIndex == PlayerManager.PlayerIndex.P1
                ? _p1ReadyTextTransform
                : _p2ReadyTextTransform;
        TextMeshProUGUI playerText =
            playerIndex == PlayerManager.PlayerIndex.P1 ? _p1Text : _p2Text;
        bool wasReady = _playerReady.ContainsKey(playerIndex) && _playerReady[playerIndex];

        if (e.IsConfirmed && !wasReady)
        {
            _playerReady[playerIndex] = true;
            if (readyText != null)
            {
                readyText.gameObject.SetActive(true);
                readyText.localScale = Vector3.zero;
                playerText
                    .DOColor(PlayerSpawner.RoleColors[e.PlayerRole], _playerTextTransitionDuration)
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
        PlayerManager.PlayerIndex playerIndex
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
        if (playerIndex == PlayerManager.PlayerIndex.P1)
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

    private void UpdateRoleSelectionVisuals(
        PlayerManager.PlayerIndex playerIndex,
        PlayerManager.PlayerRole selectedRole
    )
    {
        // Show character image when selection starts
        RawImage characterImage =
            playerIndex == PlayerManager.PlayerIndex.P1
                ? _p1CharacterRawImage
                : _p2CharacterRawImage;
        if (characterImage != null && !characterImage.gameObject.activeSelf)
        {
            characterImage.gameObject.SetActive(true);
        }

        // Decide target states for both players based on who started selection and which role
        if (selectedRole == PlayerManager.PlayerRole.Light)
        {
            if (playerIndex == PlayerManager.PlayerIndex.P1)
            {
                SetIndicatorState(PlayerManager.PlayerIndex.P1, PlayerManager.PlayerRole.Light);
                SetIndicatorState(PlayerManager.PlayerIndex.P2, PlayerManager.PlayerRole.Skull);
            }
            else if (playerIndex == PlayerManager.PlayerIndex.P2)
            {
                SetIndicatorState(PlayerManager.PlayerIndex.P2, PlayerManager.PlayerRole.Light);
                SetIndicatorState(PlayerManager.PlayerIndex.P1, PlayerManager.PlayerRole.Skull);
            }
        }
        else
        {
            if (playerIndex == PlayerManager.PlayerIndex.P1)
            {
                SetIndicatorState(PlayerManager.PlayerIndex.P1, PlayerManager.PlayerRole.Skull);
                SetIndicatorState(PlayerManager.PlayerIndex.P2, PlayerManager.PlayerRole.Light);
            }
            else if (playerIndex == PlayerManager.PlayerIndex.P2)
            {
                SetIndicatorState(PlayerManager.PlayerIndex.P2, PlayerManager.PlayerRole.Skull);
                SetIndicatorState(PlayerManager.PlayerIndex.P1, PlayerManager.PlayerRole.Light);
            }
        }
    }

    void HandleRoleSelectionReleased(object sender, PlayerSpawner.RoleSelectionEventArgs e)
    {
        PlayerManager.PlayerIndex playerIndex = e.PlayerIndex;
        if (
            playerIndex != PlayerManager.PlayerIndex.P1
            && playerIndex != PlayerManager.PlayerIndex.P2
        )
            return;

        Transform readyText =
            playerIndex == PlayerManager.PlayerIndex.P1
                ? _p1ReadyTextTransform
                : _p2ReadyTextTransform;
        // Reset the progress bar for the released role
        if (playerIndex == PlayerManager.PlayerIndex.P1 && _p1Progress != null)
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
                        UpdateCharacterImagePosition(
                            _p1Progress,
                            _p1CharacterRawImage,
                            playerIndex
                        );
                    }
                });
        }
        else if (playerIndex == PlayerManager.PlayerIndex.P2 && _p2Progress != null)
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
                        UpdateCharacterImagePosition(
                            _p2Progress,
                            _p2CharacterRawImage,
                            playerIndex
                        );
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
        PlayerManager.PlayerIndex other = (PlayerManager.PlayerIndex)(((int)playerIndex % 2) + 1);
        if (_playerRoles.ContainsKey(other))
        {
            UpdateRoleSelectionVisuals(other, _playerRoles[other]);
        }
        else
        {
            // No one selecting — reset to neutral
            SetIndicatorState(PlayerManager.PlayerIndex.P1, PlayerManager.PlayerRole.None);
            SetIndicatorState(PlayerManager.PlayerIndex.P2, PlayerManager.PlayerRole.None);
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

                // Spawn players before starting the match
                if (GameManager.Instance != null && GameManager.Instance.PlayerSpawner != null)
                {
                    GameManager.Instance.PlayerSpawner.ExecutePlayerSpawn();
                }

                _playerSwapUI.ShowInitialAssignment(
                    displayDuration: 3f,
                    onComplete: () =>
                    {
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

    private void SetIndicatorState(
        PlayerManager.PlayerIndex playerIndex,
        PlayerManager.PlayerRole newState
    )
    {
        if (playerIndex == PlayerManager.PlayerIndex.P1)
        {
            if (_p1IndicatorState == newState)
                return;

            _p1IndicatorState = newState;
            switch (newState)
            {
                case PlayerManager.PlayerRole.Light:
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
                case PlayerManager.PlayerRole.Skull:
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
                case PlayerManager.PlayerRole.None:
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
        else if (playerIndex == PlayerManager.PlayerIndex.P2)
        {
            if (_p2IndicatorState == newState)
                return;

            _p2IndicatorState = newState;
            switch (newState)
            {
                case PlayerManager.PlayerRole.Light:
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
                case PlayerManager.PlayerRole.Skull:
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
                case PlayerManager.PlayerRole.None:
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

    private void ApplyIndicatorStateInstant(int playerIndex, PlayerManager.PlayerRole state)
    {
        Color lightColor = _colorActive;
        Color shadowColor = _colorActive;

        if (state == PlayerManager.PlayerRole.Light)
        {
            lightColor = _colorActive;
            shadowColor = _colorInactive;
        }
        else if (state == PlayerManager.PlayerRole.Skull)
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
