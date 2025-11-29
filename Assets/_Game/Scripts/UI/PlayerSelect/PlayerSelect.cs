using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSelect : MonoBehaviour
{
    [SerializeField]
    private Transform _p1ReadyTextTransform;

    [SerializeField]
    private Transform _p2ReadyTextTransform;

    [SerializeField]
    private Slider _p1HoldProgress;

    [SerializeField]
    private Slider _p2HoldProgress;

    [SerializeField]
    private CanvasGroup _canvasGroup;

    private bool _p1Ready = false;
    private bool _p2Ready = false;

    private Tween _p1ProgressTween;
    private Tween _p2ProgressTween;

    private void Start()
    {
        _p1ReadyTextTransform.gameObject.SetActive(false);
        _p2ReadyTextTransform.gameObject.SetActive(false);
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
        if (e.SelectedRole == PlayerSpawner.PlayerRole.Light)
        {
            // Kill any existing tween and update value directly
            _p1ProgressTween?.Kill();
            _p1HoldProgress.value = e.HoldProgress;

            if (e.IsConfirmed && !_p1Ready)
            {
                _p1Ready = true;
                _p1ReadyTextTransform.gameObject.SetActive(true);
                _p1ReadyTextTransform.localScale = Vector3.zero;
                _p1ReadyTextTransform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            }

            if (!e.IsConfirmed)
            {
                _p1Ready = false;
                _p1ReadyTextTransform.gameObject.SetActive(false);
            }
        }
        else if (e.SelectedRole == PlayerSpawner.PlayerRole.Shadow)
        {
            // Kill any existing tween and update value directly
            _p2ProgressTween?.Kill();
            _p2HoldProgress.value = e.HoldProgress;

            if (e.IsConfirmed && !_p2Ready)
            {
                _p2Ready = true;
                _p2ReadyTextTransform.gameObject.SetActive(true);
                _p2ReadyTextTransform.localScale = Vector3.zero;
                _p2ReadyTextTransform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            }

            if (!e.IsConfirmed)
            {
                _p2Ready = false;
                _p2ReadyTextTransform.gameObject.SetActive(false);
            }
        }
    }

    void HandleRoleSelectionReleased(object sender, PlayerSpawner.RoleSelectionReleasedEventArgs e)
    {
        Debug.Log(
            $"Released event received: PlayerIndex={e.PlayerIndex}, ReleasedRole={e.ReleasedRole}"
        );

        if (e.ReleasedRole == PlayerSpawner.PlayerRole.Light)
        {
            Debug.Log("Resetting P1 (Light) slider and ready state");
            _p1ProgressTween?.Kill();
            _p1ProgressTween = DOTween
                .To(() => _p1HoldProgress.value, x => _p1HoldProgress.value = x, 0f, 0.3f)
                .SetEase(Ease.OutBack);
            _p1Ready = false;
            _p1ReadyTextTransform.gameObject.SetActive(false);
        }
        else if (e.ReleasedRole == PlayerSpawner.PlayerRole.Shadow)
        {
            Debug.Log("Resetting P2 (Shadow) slider and ready state");
            _p2ProgressTween?.Kill();
            _p2ProgressTween = DOTween
                .To(() => _p2HoldProgress.value, x => _p2HoldProgress.value = x, 0f, 0.3f)
                .SetEase(Ease.OutBack);
            _p2Ready = false;
            _p2ReadyTextTransform.gameObject.SetActive(false);
        }
    }

    private void HandlePlayersReadyToSpawn(object sender, EventArgs e)
    {
        if (_canvasGroup != null)
        {
            // Fade out and disable
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
    }
}
