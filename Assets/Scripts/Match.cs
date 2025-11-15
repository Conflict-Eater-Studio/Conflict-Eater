using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class Match : MonoBehaviour {
    #region Inspector Fields

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

    public float CountdownRemaining { get; private set; }
    public float MatchTime { get; private set; }
    public float RoundTime { get; private set; }
    public bool IsGameRunning { get; private set; }
    public bool IsGamePaused { get; private set; }
    #endregion

    #region Unity Methods

    private void Update() {
        if (!IsGameRunning || IsGamePaused) return;
        
        MatchTime += Time.deltaTime;
        RoundTime += Time.deltaTime;
        
        if (MatchTime >= _matchDurationSeconds) {
            EndMatch();
            return;
        }

        if (RoundTime >= _roundDurationSeconds) {
            EndRound();
        }
    }

    #endregion

    #region Match Flow

    public void StartMatch() {
        if (IsGameRunning) return;
        StartCoroutine(StartMatchCountdown(_matchCountdown));
    }

    private IEnumerator StartMatchCountdown(float delay) {
        Pause();
        yield return RunCountdown(delay);
        Resume();
        RunMatch();
    }

    private void RunMatch() {
        IsGameRunning = true;
        MatchTime = 0;
        RoundTime = 0;

        OnMatchStart?.Invoke(this, new OnMatchStartEventArgs(_matchDurationSeconds, _roundDurationSeconds));
        OnRoundStart?.Invoke(this, EventArgs.Empty);
    }

    public void EndMatch() {
        IsGameRunning = false;
        OnMatchEnd?.Invoke(this, EventArgs.Empty);
        StopAllCoroutines();
    }

    #endregion

    #region Round Flow

    public void EndRound() {
        RoundTime = 0;
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
        IsGamePaused = true;
        OnMatchPause?.Invoke(this, EventArgs.Empty);
    }

    public void Resume() {
        IsGamePaused = false;
        OnMatchResume?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Countdown Utility

    private IEnumerator RunCountdown(float duration) {
        CountdownRemaining = duration;
        while (CountdownRemaining > 0f) {
            yield return new WaitForEndOfFrame();
            CountdownRemaining -= Time.deltaTime;
        }
    }
    #endregion
}

#region Event Args Class

public class OnMatchStartEventArgs : EventArgs {
    public OnMatchStartEventArgs(float matchDuration, float roundDuration) {
        MatchDuration = matchDuration;
        RoundDuration = roundDuration;
    }

    public readonly float MatchDuration;
    public readonly float RoundDuration;
}

#endregion
