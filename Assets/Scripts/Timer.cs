using System;
using UnityEngine;

public class Timer : MonoBehaviour {
    [Tooltip( "Match duration in seconds" )]
    [SerializeField] private float _matchDurationSeconds = 300f;
    [Tooltip( "Round duration in seconds" )]
    [SerializeField] private float _roundDurationSeconds = 10f;
    
    public event EventHandler<OnMatchStartEventArgs> OnMatchStart;
    public event EventHandler OnMatchEnd;
    public event EventHandler OnMatchPause;
    public event EventHandler OnMatchResume;
    public event EventHandler OnRoundEnd;
    
    private float _matchStartTime;
    private float _roundStartTime;
    private float _pauseTime = 0;

    private void Update() {
        if (_pauseTime > 0) return; 
        if(Time.time >= _matchStartTime + _matchDurationSeconds) {
            OnMatchEnd?.Invoke(this, EventArgs.Empty);
            return;
        }
        if (Time.time >= _roundStartTime + _roundDurationSeconds) {
            OnRoundEnd?.Invoke(this, EventArgs.Empty);
            _roundStartTime = Time.time;
        }
   
    }   
    public void StartTimer() {
        _matchStartTime = Time.time;
        _roundStartTime = Time.time;
        OnMatchStart?.Invoke(this, new OnMatchStartEventArgs(_matchDurationSeconds, _roundDurationSeconds)); 
    }
    public void PauseTimer() {
        _pauseTime = Time.time;
        OnMatchPause?.Invoke(this, EventArgs.Empty);
    }
    public void ResumeTimer() {
        float diff = Time.time - _pauseTime;
        _matchStartTime += diff;
        _roundStartTime += diff;
        _pauseTime = 0;
        OnMatchResume?.Invoke(this, EventArgs.Empty);
    }

    public void EndRound() {
        _roundStartTime = Time.time;
        OnRoundEnd?.Invoke(this, EventArgs.Empty);
    }
}
public class OnMatchStartEventArgs : EventArgs {
    public OnMatchStartEventArgs(float matchDuration, float roundDuration) {
        MatchDuration = matchDuration;
        RoundDuration = roundDuration;
    }
    public readonly float MatchDuration;
    public readonly float RoundDuration;
}