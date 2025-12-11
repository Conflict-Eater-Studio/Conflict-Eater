using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSelectUI : MonoBehaviour
{
    [Header("Shared Progress Bar")]
    [SerializeField]
    private Slider _p1Progress;

    [SerializeField]
    private Slider _p2Progress;

    [Header("Player Ready Indicators")]
    [SerializeField]
    private Transform _p1ReadyTextTransform;

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

    private Tween _p1Tween;
    private Tween _p2Tween;

    private void Start()
    {
        _p1ReadyTextTransform.gameObject.SetActive(false);
        _p2ReadyTextTransform.gameObject.SetActive(false);

        // Initialize progress bars
        if (_p1Progress != null)
        {
            _p1Progress.value = 0;
        }

        if (_p2Progress != null)
        {
            _p2Progress.value = 0;
        }
    }

    private void OnEnable()
    {
        PlayerSpawner.OnRoleSelectionChanged += HandleRoleSelectionChanged;
        PlayerSpawner.OnRoleSelectionReleased += HandleRoleSelectionReleased;
        PlayerSpawner.OnPlayersReadyToSpawn += HandlePlayersReadyToSpawn;
    }

    private void OnDisable()
    {
        PlayerSpawner.OnRoleSelectionChanged -= HandleRoleSelectionChanged;
        PlayerSpawner.OnRoleSelectionReleased -= HandleRoleSelectionReleased;
        PlayerSpawner.OnPlayersReadyToSpawn -= HandlePlayersReadyToSpawn;
    }

    private void OnDestroy()
    {
        PlayerSpawner.OnRoleSelectionChanged -= HandleRoleSelectionChanged;
        PlayerSpawner.OnRoleSelectionReleased -= HandleRoleSelectionReleased;
        PlayerSpawner.OnPlayersReadyToSpawn -= HandlePlayersReadyToSpawn;
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

        if (playerIndex == 0)
        {
            progressBar = _p1Progress;
            progressBar.fillRect.GetComponent<Image>().color = PlayerSpawner.RoleColors[
                e.SelectedRole
            ];
            _p1Tween?.Kill();
        }
        else if (playerIndex == 1)
        {
            progressBar = _p2Progress;
            progressBar.fillRect.GetComponent<Image>().color = PlayerSpawner.RoleColors[
                e.SelectedRole
            ];
            _p2Tween?.Kill();
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

        Transform readyText = playerIndex == 0 ? _p1ReadyTextTransform : _p2ReadyTextTransform;

        // Reset the progress bar for the released role
        if (playerIndex == 0 && _p1Progress != null)
        {
            _p1Tween?.Kill();
            _p1Tween = DOTween
                .To(() => _p1Progress.value, x => _p1Progress.value = x, 0f, 0.3f)
                .SetEase(Ease.OutBack);
        }
        else if (playerIndex == 1 && _p2Progress != null)
        {
            _p2Tween?.Kill();
            _p2Tween = DOTween
                .To(() => _p2Progress.value, x => _p2Progress.value = x, 0f, 0.3f)
                .SetEase(Ease.OutBack);
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
            // Get Player 1's role from PlayerSpawner
            PlayerSpawner spawner = FindFirstObjectByType<PlayerSpawner>();
            PlayerSpawner.PlayerRole p1Role =
                spawner != null ? spawner.GetPlayer1Role() : PlayerSpawner.PlayerRole.Light;

            _playerSwapUI.ShowInitialAssignment(
                p1Role,
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
