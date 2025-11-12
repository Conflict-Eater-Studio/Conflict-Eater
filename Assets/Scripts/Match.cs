using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class Match : MonoBehaviour {
    [Header("DEBUG UI")]
    [SerializeField] private TMP_Text _matchCountdownText;

    [Header("Timing Settings")]
    [Tooltip("Countdown before match start")]
    [SerializeField] private float _matchCountdown = 5f;
    [SerializeField] private float _roundCountdown = 3f;
    [Tooltip("Match duration in seconds")]
    [SerializeField] private float _matchDurationSeconds = 300f;
    [Tooltip("Round duration in seconds")]
    [SerializeField] private float _roundDurationSeconds = 10f;

    public event EventHandler<OnMatchStartEventArgs> OnMatchStart;
    public event EventHandler OnMatchEnd;
    public event EventHandler OnMatchPause;
    public event EventHandler OnMatchResume;
    public event EventHandler OnRoundEnd;
    public event EventHandler OnRoundStart;

    private float _matchStartTime;
    private float _roundStartTime;
    private float _pauseTime;
    private bool _isGameRunning;

    private void Update() {
        if (!_isGameRunning || _pauseTime > 0) return;

        if (Time.time >= _matchStartTime + _matchDurationSeconds) {
            EndMatch();
            return;
        }

        if (Time.time >= _roundStartTime + _roundDurationSeconds) {
            EndRound();
        }
    }

    // --- MATCH FLOW ---

    public void StartMatch() {
        if (_isGameRunning) return;
        StartCoroutine(StartMatchCountdown(_matchCountdown));
    }

    private IEnumerator StartMatchCountdown(float delay) {
        yield return RunCountdown(delay);
        RunMatch();
    }

    private void RunMatch() {
        _isGameRunning = true;
        _matchStartTime = Time.time;
        _roundStartTime = Time.time;
        OnMatchStart?.Invoke(this, new OnMatchStartEventArgs(_matchDurationSeconds, _roundDurationSeconds));
        OnRoundStart?.Invoke(this, EventArgs.Empty);
    }

    public void EndMatch() {
        _isGameRunning = false;
        _matchCountdownText.text = "";
        OnMatchEnd?.Invoke(this, EventArgs.Empty);
        StopAllCoroutines();
    }

    // --- ROUND FLOW ---
    public void EndRound() {
        StartCoroutine(HandleRoundTransition());
    }
    private IEnumerator HandleRoundTransition() {
        OnRoundEnd?.Invoke(this, EventArgs.Empty);
        Pause();
        
        yield return RunCountdown(_roundCountdown);
        
        OnRoundStart?.Invoke(this, EventArgs.Empty);
        Resume();
        
    }

    // --- PAUSE & RESUME ---

    public void Pause() {
        if (_pauseTime > 0) return;
        _pauseTime = Time.time;
        OnMatchPause?.Invoke(this, EventArgs.Empty);
    }

    public void Resume() {
        if (_pauseTime <= 0) return;
        float diff = Time.time - _pauseTime;
        _matchStartTime += diff;
        _roundStartTime += diff;
        _pauseTime = 0;
        OnMatchResume?.Invoke(this, EventArgs.Empty);
    }

    // --- COUNTDOWN UTILITY ---

    private IEnumerator RunCountdown(float duration) {
        float remaining = duration;
        while (remaining > 0f) {
            _matchCountdownText.SetText($"{Mathf.CeilToInt(remaining)}");
            yield return new WaitForEndOfFrame();
            remaining -= Time.deltaTime;
        }
        _matchCountdownText.text = "";
    }
}

// --- EVENT ARGS CLASS ---

public class OnMatchStartEventArgs : EventArgs {
    public OnMatchStartEventArgs(float matchDuration, float roundDuration) {
        MatchDuration = matchDuration;
        RoundDuration = roundDuration;
    }
    public readonly float MatchDuration;
    public readonly float RoundDuration;
}
