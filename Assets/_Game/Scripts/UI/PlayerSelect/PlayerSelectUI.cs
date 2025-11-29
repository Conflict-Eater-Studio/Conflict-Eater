using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSelectUI : MonoBehaviour
{
    [Header("Player 1 UI")]
    [SerializeField]
    private Image _p1ColorIndicator;

    [SerializeField]
    private Slider _p1LightProgress;

    [SerializeField]
    private Slider _p1ShadowProgress;

    [SerializeField]
    private Transform _p1ReadyTextTransform;

    [Header("Player 2 UI")]
    [SerializeField]
    private Image _p2ColorIndicator;

    [SerializeField]
    private Slider _p2LightProgress;

    [SerializeField]
    private Slider _p2ShadowProgress;

    [SerializeField]
    private Transform _p2ReadyTextTransform;

    [Header("General")]
    [SerializeField]
    private CanvasGroup _canvasGroup;

    [SerializeField]
    private PlayerSwapUI _playerSwapUI;

    private Dictionary<int, PlayerSpawner.PlayerRole> _playerRoles =
        new Dictionary<int, PlayerSpawner.PlayerRole>();
    private Dictionary<int, bool> _playerReady = new Dictionary<int, bool>();

    private Tween _p1LightTween;
    private Tween _p1ShadowTween;
    private Tween _p2LightTween;
    private Tween _p2ShadowTween;

    private void Start()
    {
        _p1ReadyTextTransform.gameObject.SetActive(false);
        _p2ReadyTextTransform.gameObject.SetActive(false);

        // Initialize progress bars
        if (_p1LightProgress != null)
            _p1LightProgress.value = 0;
        if (_p1ShadowProgress != null)
            _p1ShadowProgress.value = 0;
        if (_p2LightProgress != null)
            _p2LightProgress.value = 0;
        if (_p2ShadowProgress != null)
            _p2ShadowProgress.value = 0;
    }

    private void OnEnable()
    {
        PlayerSpawner.OnRoleSelectionChanged += HandleRoleSelectionChanged;
        PlayerSpawner.OnRoleSelectionReleased += HandleRoleSelectionReleased;
        PlayerSpawner.OnColorChanged += HandleColorChanged;
        PlayerSpawner.OnPlayersReadyToSpawn += HandlePlayersReadyToSpawn;
    }

    private void OnDisable()
    {
        PlayerSpawner.OnRoleSelectionChanged -= HandleRoleSelectionChanged;
        PlayerSpawner.OnRoleSelectionReleased -= HandleRoleSelectionReleased;
        PlayerSpawner.OnColorChanged -= HandleColorChanged;
        PlayerSpawner.OnPlayersReadyToSpawn -= HandlePlayersReadyToSpawn;
    }

    private void OnDestroy()
    {
        PlayerSpawner.OnRoleSelectionChanged -= HandleRoleSelectionChanged;
        PlayerSpawner.OnRoleSelectionReleased -= HandleRoleSelectionReleased;
        PlayerSpawner.OnColorChanged -= HandleColorChanged;
        PlayerSpawner.OnPlayersReadyToSpawn -= HandlePlayersReadyToSpawn;
    }

    private void HandleColorChanged(object sender, PlayerSpawner.ColorChangedEventArgs e)
    {
        Color newColor = PlayerColor.GetColor(e.NewColor).Color;

        // Update color indicator for the player
        if (e.PlayerIndex == 0)
        {
            if (_p1ColorIndicator != null)
                _p1ColorIndicator.color = newColor;

            // Update P1's progress bar fill colors
            if (_p1LightProgress != null && _p1LightProgress.fillRect != null)
                _p1LightProgress.fillRect.GetComponent<Image>().color = newColor;

            if (_p1ShadowProgress != null && _p1ShadowProgress.fillRect != null)
                _p1ShadowProgress.fillRect.GetComponent<Image>().color = newColor;
        }
        else if (e.PlayerIndex == 1)
        {
            if (_p2ColorIndicator != null)
                _p2ColorIndicator.color = newColor;

            // Update P2's progress bar fill colors
            if (_p2LightProgress != null && _p2LightProgress.fillRect != null)
                _p2LightProgress.fillRect.GetComponent<Image>().color = newColor;

            if (_p2ShadowProgress != null && _p2ShadowProgress.fillRect != null)
                _p2ShadowProgress.fillRect.GetComponent<Image>().color = newColor;
        }
    }

    private void HandleRoleSelectionChanged(object sender, PlayerSpawner.RoleSelectionEventArgs e)
    {
        int playerIndex = e.PlayerIndex;
        if (playerIndex < 0 || playerIndex > 1)
            return;

        // Track which role this player is selecting
        _playerRoles[playerIndex] = e.SelectedRole;

        // Get the appropriate progress bar
        Slider progressBar = null;
        Tween progressTween = null;

        if (playerIndex == 0)
        {
            if (e.SelectedRole == PlayerSpawner.PlayerRole.Light)
            {
                progressBar = _p1LightProgress;
                _p1LightTween?.Kill();
                progressTween = _p1LightTween;

                // Reset shadow progress
                if (_p1ShadowProgress != null)
                    _p1ShadowProgress.value = 0;
            }
            else
            {
                progressBar = _p1ShadowProgress;
                _p1ShadowTween?.Kill();
                progressTween = _p1ShadowTween;

                // Reset light progress
                if (_p1LightProgress != null)
                    _p1LightProgress.value = 0;
            }
        }
        else
        {
            if (e.SelectedRole == PlayerSpawner.PlayerRole.Light)
            {
                progressBar = _p2LightProgress;
                _p2LightTween?.Kill();
                progressTween = _p2LightTween;

                // Reset shadow progress
                if (_p2ShadowProgress != null)
                    _p2ShadowProgress.value = 0;
            }
            else
            {
                progressBar = _p2ShadowProgress;
                _p2ShadowTween?.Kill();
                progressTween = _p2ShadowTween;

                // Reset light progress
                if (_p2LightProgress != null)
                    _p2LightProgress.value = 0;
            }
        }

        // Update progress bar
        if (progressBar != null)
        {
            progressBar.value = e.HoldProgress;
        }

        // Handle ready state
        Transform readyText = playerIndex == 0 ? _p1ReadyTextTransform : _p2ReadyTextTransform;
        bool wasReady = _playerReady.ContainsKey(playerIndex) && _playerReady[playerIndex];

        if (e.IsConfirmed && !wasReady)
        {
            _playerReady[playerIndex] = true;
            if (readyText != null)
            {
                readyText.gameObject.SetActive(true);
                readyText.localScale = Vector3.zero;
                readyText.DOScale(Vector3.one, 0.5f).SetEase(Ease.InOutSine);
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

    void HandleRoleSelectionReleased(object sender, PlayerSpawner.RoleSelectionReleasedEventArgs e)
    {
        int playerIndex = e.PlayerIndex;
        if (playerIndex < 0 || playerIndex > 1)
            return;

        // Get appropriate progress bars and tweens
        Slider lightProgress = playerIndex == 0 ? _p1LightProgress : _p2LightProgress;
        Slider shadowProgress = playerIndex == 0 ? _p1ShadowProgress : _p2ShadowProgress;
        Transform readyText = playerIndex == 0 ? _p1ReadyTextTransform : _p2ReadyTextTransform;

        // Reset the progress bar for the released role
        if (e.ReleasedRole == PlayerSpawner.PlayerRole.Light && lightProgress != null)
        {
            if (playerIndex == 0)
            {
                _p1LightTween?.Kill();
                _p1LightTween = DOTween
                    .To(() => lightProgress.value, x => lightProgress.value = x, 0f, 0.3f)
                    .SetEase(Ease.OutBack);
            }
            else
            {
                _p2LightTween?.Kill();
                _p2LightTween = DOTween
                    .To(() => lightProgress.value, x => lightProgress.value = x, 0f, 0.3f)
                    .SetEase(Ease.OutBack);
            }
        }
        else if (e.ReleasedRole == PlayerSpawner.PlayerRole.Shadow && shadowProgress != null)
        {
            if (playerIndex == 0)
            {
                _p1ShadowTween?.Kill();
                _p1ShadowTween = DOTween
                    .To(() => shadowProgress.value, x => shadowProgress.value = x, 0f, 0.3f)
                    .SetEase(Ease.OutBack);
            }
            else
            {
                _p2ShadowTween?.Kill();
                _p2ShadowTween = DOTween
                    .To(() => shadowProgress.value, x => shadowProgress.value = x, 0f, 0.3f)
                    .SetEase(Ease.OutBack);
            }
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
    }

    private void HandlePlayersReadyToSpawn(object sender, EventArgs e)
    {
        // Fade out the player select UI
        if (_canvasGroup != null)
        {
            _canvasGroup
                .DOFade(0f, 0.5f)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
        }
        else
        {
            gameObject.SetActive(false);
        }

        // Show the initial role assignment for 3 seconds, then start the match
        if (_playerSwapUI != null)
        {
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
    }
}
