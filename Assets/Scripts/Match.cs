using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class Match : MonoBehaviour {
    #region Inspector Fields

    [Header("Timing Settings")]
    [Tooltip("Countdown before match start")]
    [SerializeField] private float _matchCountdown = 5f;
    [SerializeField] private float _roundCountdown = 3f;
    [Tooltip("Match duration in seconds")]
    [SerializeField] private float _matchDurationSeconds = 300f;
    [Tooltip("Round duration in seconds")]
    [SerializeField] private float _roundDurationSeconds = 10f;

    #endregion

    #region Events

    public event EventHandler<OnMatchStartEventArgs> OnMatchStart;
    public event EventHandler OnMatchEnd;
    public event EventHandler OnMatchPause;
    public event EventHandler OnMatchResume;
    public event EventHandler OnRoundEnd;
    public event EventHandler OnRoundStart;

    #endregion

    #region Private Fields

    private float _matchTime;
    private float _rountTime;
    private float _pauseTime;
    private bool _isGameRunning;
    private bool _isGamePaused;
    #endregion

    #region Unity Methods

    private void Update() {
        if (!_isGameRunning || _isGamePaused) return;
        
        _matchTime += Time.deltaTime;
        _rountTime += Time.deltaTime;
        
        if (_matchTime >= _matchDurationSeconds) {
            EndMatch();
            return;
        }

        if (_rountTime >= _roundDurationSeconds) {
            EndRound();
        }
    }

    #endregion

    #region Match Flow

    public void StartMatch() {
        if (_isGameRunning) return;
        StartCoroutine(StartMatchCountdown(_matchCountdown));
    }

    private IEnumerator StartMatchCountdown(float delay) {
        Pause();
        OnMatchStart?.Invoke(this, new OnMatchStartEventArgs(_matchDurationSeconds, _roundDurationSeconds, _matchCountdown, _roundCountdown));
        yield return RunCountdown(delay);
        Resume();
        RunMatch();
    }

    private void RunMatch() {
        _isGameRunning = true;
        _matchTime = 0;
        _rountTime = 0;
        OnRoundStart?.Invoke(this, EventArgs.Empty);
    }

    public void EndMatch() {
        _isGameRunning = false;
        OnMatchEnd?.Invoke(this, EventArgs.Empty);
        StopAllCoroutines();
    }

    #endregion

    #region Round Flow

    public void EndRound() {
        StartCoroutine(HandleRoundTransition());
    }

    private IEnumerator HandleRoundTransition() {
        Pause();
        OnRoundEnd?.Invoke(this, EventArgs.Empty);

        yield return RunCountdown(_roundCountdown);

        Resume();
        OnRoundStart?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Pause & Resume

    public void Pause() {
        _isGamePaused = true;
        OnMatchPause?.Invoke(this, EventArgs.Empty);
    }

    public void Resume() {
        _isGamePaused = false;
        OnMatchResume?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Countdown Utility

    private IEnumerator RunCountdown(float duration) {
        float remaining = duration;
        while (remaining > 0f) {
            yield return new WaitForEndOfFrame();
            remaining -= Time.deltaTime;
        }
    }

    #endregion
}

#region Event Args Class

public class OnMatchStartEventArgs : EventArgs {
    public OnMatchStartEventArgs(float matchDuration, float roundDuration, float matchCountdown, float roundCountdown) {
        MatchDuration = matchDuration;
        RoundDuration = roundDuration;
        MatchCountdown = matchCountdown;
        RoundCountdown = roundCountdown;
        
    }

    public readonly float MatchDuration;
    public readonly float RoundDuration;
    public readonly float MatchCountdown;
    public readonly float RoundCountdown;   
}

#endregion
