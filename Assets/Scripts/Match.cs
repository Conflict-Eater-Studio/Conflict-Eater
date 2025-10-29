using System;
using UnityEngine;

public class Match : MonoBehaviour {
    [Tooltip( "Match duration in seconds" )]
    [SerializeField] private float _matchDurationSeconds = 300f;
    [Tooltip( "Round duration in seconds" )]
    [SerializeField] private float _roundDurationSeconds = 10f;
    
    public event EventHandler<OnMatchStartEventArgs> OnMatchStart;
    public event EventHandler OnMatchEnd;
    public event EventHandler OnMatchPause;
    public event EventHandler OnMatchResume;
    public event EventHandler OnRoundEnd;
    
    private float _matchStartTime = 0;
    private float _roundStartTime = 0;
    private float _pauseTime = 0;

    private bool _isGameStart = false;

    private void Update() {
        if (_pauseTime > 0) return;
        if (!_isGameStart) return; 
        if(Time.time >= _matchStartTime + _matchDurationSeconds) {
            End();             
            return;
        }
        if (Time.time >= _roundStartTime + _roundDurationSeconds) {
            EndRound();
        }
   
    }   
    public void Run() {
        _isGameStart = true;
        _matchStartTime = Time.time;
        _roundStartTime = Time.time;
        OnMatchStart?.Invoke(this, new OnMatchStartEventArgs(_matchDurationSeconds, _roundDurationSeconds)); 
    }
    public void Pause() {
        _pauseTime = Time.time;
        OnMatchPause?.Invoke(this, EventArgs.Empty);
    }
    public void Resume() {
        float diff = Time.time - _pauseTime;
        _matchStartTime += diff;
        _roundStartTime += diff;
        _pauseTime = 0;
        OnMatchResume?.Invoke(this, EventArgs.Empty);
    }
    public void End() {
        OnMatchEnd?.Invoke(this, EventArgs.Empty);
        _isGameStart = false;
    }
    public void EndRound() {
        Debug.Log("round end");
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