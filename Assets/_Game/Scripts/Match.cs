using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Match : MonoBehaviour
{
    #region Inspector Fields

    [Tooltip("Countdown before match start")]
    [SerializeField]
    private float _matchCountdown = 5f;

    [SerializeField]
    private float _roundCountdown = 3f;

    [Tooltip("Match duration in seconds")]
    [SerializeField]
    private float _matchDurationSeconds = 300f;

    [Tooltip("Round duration in seconds")]
    [SerializeField]
    private float _roundDurationSeconds = 10f;
    
    [Tooltip("Round in single match")]
    [SerializeField][Range(1, 10)]
    private int _rounds = 3;
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

    public int Rounds { get; private set; }
    public float CountdownRemaining { get; private set; }
    public float MatchTime { get; private set; }
    public float RoundTime { get; private set; }
    public bool IsGameRunning { get; private set; }
    public bool IsGamePaused { get; private set; }
    #endregion

    #region Unity Methods

    private void Update()
    {
        if (!IsGameRunning || IsGamePaused)
            return;

        RoundTime += Time.deltaTime;
        if (Rounds <= 0) {
            EndMatch();
        }
        if (RoundTime >= _roundDurationSeconds)
        {
            EndRound();
        }
    }

    #endregion

    #region Match Flow

    public void StartMatch()
    {
        Rounds = _rounds;
        if (IsGameRunning)
            return;
        StartCoroutine(StartMatchCountdown(_matchCountdown));
    }

    private IEnumerator StartMatchCountdown(float delay)
    {
        Pause();
        yield return RunCountdown(delay);
        Resume();
        RunMatch();
    }

    private void RunMatch()
    {
        IsGameRunning = true;
        MatchTime = 0;
        RoundTime = 0;

        OnMatchStart?.Invoke(
            this,
            new OnMatchStartEventArgs(_rounds, _roundDurationSeconds)
        );
        OnRoundStart?.Invoke(this, EventArgs.Empty);
    }

    public void EndMatch()
    {
        IsGameRunning = false;
        OnMatchEnd?.Invoke(this, EventArgs.Empty);
        StopAllCoroutines();
    }

    #endregion

    #region Round Flow

    /// <summary>
    /// Ends the current round. Pauses the timer, resets round time, and fires OnRoundEnd event.
    /// External systems should call StartRoundCountdown() when ready to begin the next round.
    /// </summary>
    public void EndRound()
    {
        RoundTime = 0;
        Rounds--;
        Pause();
        if (Rounds <= 0) {
            EndMatch();

            return;
        }
        OnRoundEnd?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Starts the countdown for the next round. Call this after handling round end logic (e.g., animations).
    /// </summary>
    public void StartRoundCountdown()
    {
        StartCoroutine(HandleRoundCountdown());
    }

    private IEnumerator HandleRoundCountdown()
    {
        yield return RunCountdown(_roundCountdown);
        Resume();
        OnRoundStart?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Pause & Resume

    public void Pause()
    {
        IsGamePaused = true;
        OnMatchPause?.Invoke(this, EventArgs.Empty);
    }

    public void Resume()
    {
        IsGamePaused = false;
        OnMatchResume?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Countdown Utility

    private IEnumerator RunCountdown(float duration)
    {
        CountdownRemaining = duration;
        while (CountdownRemaining > 0f)
        {
            yield return new WaitForEndOfFrame();
            CountdownRemaining -= Time.deltaTime;
        }
    }
    #endregion
}

#region Event Args Class

public class OnMatchStartEventArgs : EventArgs
{
    public OnMatchStartEventArgs(int matchRounds, float roundDuration)
    {
        MatchRounds = matchRounds;
        RoundDuration = roundDuration;
    }

    public readonly int MatchRounds;
    public readonly float RoundDuration;
}

#endregion
