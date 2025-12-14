using System;
using System.Data.Common;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the player swap UI overlay, showing which player has which role.
/// Top bar = Player 1 (fixed position), Bottom bar = Player 2 (fixed position).
/// Role text swaps between bars with dramatic animation when roles change.
/// </summary>
public class PlayerSwapUI : MonoBehaviour
{
    [Header("UI References - Fixed Player Positions")]
    [Tooltip("Light color indicator")]
    [SerializeField]
    private Image _lightColorIndicator;

    [Tooltip("Shadow color indicator")]
    [SerializeField]
    private Image _shadowColorIndicator;

    [Tooltip("Player 1's role text (Light/Shadow)")]
    [SerializeField]
    private TextMeshProUGUI _player1RoleText;

    [Tooltip("Player 2's role text (Light/Shadow)")]
    [SerializeField]
    private TextMeshProUGUI _player2RoleText;

    [SerializeField]
    private CanvasGroup _canvasGroup;

    [Header("Animation Settings")]
    [SerializeField]
    private float _fadeInDuration = 0.3f;

    [SerializeField]
    private float _roleSwapDuration = 0.6f;

    [SerializeField]
    private float _displayDuration = 1.5f;

    [SerializeField]
    private float _fadeOutDuration = 0.5f;

    [SerializeField]
    private float _textFlyoutDistance = 200f;

    [SerializeField]
    private Ease _roleSwapEase = Ease.OutBack;

    private PlayerManager _playerManager;
    private Sequence _animationSequence;

    // Track current role assignment (true = Player1 is Light, false = Player1 is Shadow)
    private bool _player1IsLight = true;

    /// <summary>
    /// Gets whether Player 1 (index 0) currently has the Light role.
    /// </summary>
    public bool IsPlayer1Light => _player1IsLight;

    /// <summary>
    /// Gets the current role of Player 1 (index 0).
    /// </summary>
    public PlayerSpawner.PlayerRole GetPlayer1Role()
    {
        return _player1IsLight ? PlayerSpawner.PlayerRole.Light : PlayerSpawner.PlayerRole.Shadow;
    }

    private void Awake()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Start hidden
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            _playerManager = GameManager.Instance.PlayerManager;
            if (_playerManager != null)
            {
                _playerManager.OnPlayerSwapped += OnPlayerSwapped;
            }
        }
        else
        {
            Debug.LogWarning("PlayerSwapUI: GameManager not found!");
        }
    }

    private void OnDestroy()
    {
        if (_playerManager != null)
        {
            _playerManager.OnPlayerSwapped -= OnPlayerSwapped;
        }

        _animationSequence?.Kill();
    }

    /// <summary>
    /// Called when players swap roles. Triggers the full swap animation sequence.
    /// </summary>
    private void OnPlayerSwapped(object sender, EventArgs e)
    {
        PlaySwapAnimation();
    }

    /// <summary>
    /// Shows the initial player/role assignment with fade in, holds for display duration, then fades out.
    /// Use onComplete callback to trigger match start after the display.
    /// </summary>
    /// <param name="p1Role">The role that Player 1 (index 0) has selected</param>
    /// <param name="displayDuration">How long to display the role assignment before fading out</param>
    /// <param name="onComplete">Callback when the entire sequence (fade in, hold, fade out) completes</param>
    public void ShowInitialAssignment(
        PlayerSpawner.PlayerRole p1Role,
        float displayDuration = 3f,
        Action onComplete = null
    )
    {
        _animationSequence?.Kill();
        gameObject.SetActive(true);

        // Initialize role tracking based on Player 1's actual role
        _player1IsLight = p1Role == PlayerSpawner.PlayerRole.Light;

        _lightColorIndicator.transform.rotation = Quaternion.Euler(
            0f,
            0f,
            _player1IsLight ? 0f : 180f
        );
        _shadowColorIndicator.transform.rotation = Quaternion.Euler(
            0f,
            0f,
            _player1IsLight ? 180f : 0f
        );

        // Create sequence: fade in -> hold -> fade out
        _animationSequence = DOTween.Sequence();
        _animationSequence.Append(_canvasGroup.DOFade(1f, _fadeInDuration));
        _animationSequence.AppendInterval(displayDuration);
        _animationSequence.Append(_canvasGroup.DOFade(0f, _fadeOutDuration));
        _animationSequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// Plays the complete player swap animation sequence
    /// </summary>
    public void PlaySwapAnimation()
    {
        // Kill any existing animation
        _animationSequence?.Kill();

        gameObject.SetActive(true);

        // Swap the role state
        _player1IsLight = !_player1IsLight;

        _animationSequence = DOTween.Sequence();
        _animationSequence.Append(_canvasGroup.DOFade(1f, _fadeInDuration));
        _animationSequence.Append(RotateColorIndicators());
        _animationSequence.AppendInterval(_roleSwapDuration);
        _animationSequence.Append(_canvasGroup.DOFade(0f, _fadeOutDuration));

        // Hide when done and trigger round countdown
        _animationSequence.OnComplete(() =>
        {
            gameObject.SetActive(false);

            // After swap animation, start the round countdown
            if (GameManager.Instance != null && GameManager.Instance.Timer != null)
            {
                GameManager.Instance.Timer.StartRoundCountdown();
            }
        });
    }

    /// <summary>
    /// Animate 180 deg rotation of color indicators.
    /// </summary>
    private Sequence RotateColorIndicators()
    {
        Sequence s = DOTween.Sequence();

        s.Append(
            _lightColorIndicator
                .transform.DORotate(
                    new Vector3(
                        0f,
                        0f,
                        _lightColorIndicator.transform.rotation.eulerAngles.z + 180f
                    ),
                    _roleSwapDuration,
                    RotateMode.FastBeyond360
                )
                .SetEase(_roleSwapEase)
        );

        s.Join(
            _shadowColorIndicator
                .transform.DORotate(
                    new Vector3(
                        0f,
                        0f,
                        _shadowColorIndicator.transform.rotation.eulerAngles.z + 180f
                    ),
                    _roleSwapDuration,
                    RotateMode.FastBeyond360
                )
                .SetEase(_roleSwapEase)
        );

        return s;
    }

    /// <summary>
    /// Immediately hides the swap UI without animation.
    /// </summary>
    public void Hide()
    {
        _animationSequence?.Kill();
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
