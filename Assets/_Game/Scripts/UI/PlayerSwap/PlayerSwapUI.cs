using System;
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
    [Tooltip("Player 1's bar - always stays at top")]
    [SerializeField]
    private RectTransform _player1Bar;

    [Tooltip("Player 2's bar - always stays at bottom")]
    [SerializeField]
    private RectTransform _player2Bar;

    [Tooltip("Player 1's color indicator")]
    [SerializeField]
    private Image _player1ColorIndicator;

    [Tooltip("Player 2's color indicator")]
    [SerializeField]
    private Image _player2ColorIndicator;

    [Tooltip("Player 1's role text (Light/Shadow)")]
    [SerializeField]
    private TextMeshProUGUI _player1RoleText;

    [Tooltip("Player 2's role text (Light/Shadow)")]
    [SerializeField]
    private TextMeshProUGUI _player2RoleText;

    [SerializeField]
    private CanvasGroup _canvasGroup;

    [SerializeField]
    private Image _flashOverlay;

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

        // Setup flash overlay
        if (_flashOverlay != null)
        {
            _flashOverlay.color = new Color(1, 1, 1, 0);
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
    /// <param name="displayDuration">How long to display the role assignment before fading out</param>
    /// <param name="onComplete">Callback when the entire sequence (fade in, hold, fade out) completes</param>
    public void ShowInitialAssignment(float displayDuration = 3f, Action onComplete = null)
    {
        _animationSequence?.Kill();
        gameObject.SetActive(true);

        // Determine initial roles from PlayerManager
        _player1IsLight = true;
        UpdateBarsToCurrentState();

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
    /// Plays the complete player swap animation sequence:
    /// 1. Role texts fly out to the sides and fade
    /// 2. Role texts swap and fly back in from opposite sides
    /// 3. Pulse bars
    /// 4. Hold then fade out
    /// 5. Trigger round countdown (if during gameplay)
    /// </summary>
    public void PlaySwapAnimation()
    {
        // Kill any existing animation
        _animationSequence?.Kill();

        gameObject.SetActive(true);
        _canvasGroup.alpha = 1f;

        // Swap the role state
        _player1IsLight = !_player1IsLight;

        _animationSequence = DOTween.Sequence();
        _animationSequence.Append(AnimateRoleSwap());
        _animationSequence.AppendInterval(_displayDuration);
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
    /// Animates the role texts swapping with dramatic fly-out and fly-in effect.
    /// Texts fly out to opposite sides, swap content, then fly back in.
    /// </summary>
    private Sequence AnimateRoleSwap()
    {
        Sequence swapSeq = DOTween.Sequence();

        // Store original positions
        Vector2 p1OriginalPos = _player1RoleText.rectTransform.anchoredPosition;
        Vector2 p2OriginalPos = _player2RoleText.rectTransform.anchoredPosition;

        // Player 1 text flies left
        swapSeq.Join(
            _player1RoleText
                .rectTransform.DOAnchorPosX(
                    p1OriginalPos.x - _textFlyoutDistance,
                    _roleSwapDuration * 0.4f
                )
                .SetEase(Ease.InBack)
        );
        swapSeq.Join(_player1RoleText.DOFade(0f, _roleSwapDuration * 0.4f));

        // Player 2 text flies right
        swapSeq.Join(
            _player2RoleText
                .rectTransform.DOAnchorPosX(
                    p2OriginalPos.x + _textFlyoutDistance,
                    _roleSwapDuration * 0.4f
                )
                .SetEase(Ease.InBack)
        );
        swapSeq.Join(_player2RoleText.DOFade(0f, _roleSwapDuration * 0.4f));

        // Swap the text content while invisible
        swapSeq.AppendCallback(() =>
        {
            UpdateRoleTexts();
        });

        // Player 1 text flies in from right
        _player1RoleText.rectTransform.anchoredPosition = new Vector2(
            p1OriginalPos.x + _textFlyoutDistance,
            p1OriginalPos.y
        );
        swapSeq.Append(
            _player1RoleText
                .rectTransform.DOAnchorPos(p1OriginalPos, _roleSwapDuration * 0.6f)
                .SetEase(_roleSwapEase)
        );
        swapSeq.Join(_player1RoleText.DOFade(1f, _roleSwapDuration * 0.6f));

        // Player 2 text flies in from left
        _player2RoleText.rectTransform.anchoredPosition = new Vector2(
            p2OriginalPos.x - _textFlyoutDistance,
            p2OriginalPos.y
        );
        swapSeq.Join(
            _player2RoleText
                .rectTransform.DOAnchorPos(p2OriginalPos, _roleSwapDuration * 0.6f)
                .SetEase(_roleSwapEase)
        );
        swapSeq.Join(_player2RoleText.DOFade(1f, _roleSwapDuration * 0.6f));

        return swapSeq;
    }

    /// <summary>
    /// Updates the bar colors (fixed per player) and role texts (based on current roles).
    /// </summary>
    private void UpdateBarsToCurrentState()
    {
        Debug.Log("Updating PlayerSwapUI bars to current state.");
        Debug.Log("Player 1 Color: " + PlayerColorManager.Player1Color);
        Debug.Log("Player 2 Color: " + PlayerColorManager.Player2Color);
        if (_player1ColorIndicator != null)
            _player1ColorIndicator.color = PlayerColor
                .GetColor(PlayerColorManager.Player1Color)
                .Color;

        if (_player2ColorIndicator != null)
            _player2ColorIndicator.color = PlayerColor
                .GetColor(PlayerColorManager.Player2Color)
                .Color;

        UpdateRoleTexts();
    }

    /// <summary>
    /// Updates role texts based on current _player1IsLight state.
    /// </summary>
    private void UpdateRoleTexts()
    {
        if (_player1RoleText != null)
            _player1RoleText.text = _player1IsLight ? "LIGHT" : "SHADOW";

        if (_player2RoleText != null)
            _player2RoleText.text = _player1IsLight ? "SHADOW" : "LIGHT";
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
