using System;
using UnityEngine;
public class OnMatchStartEventArgs : EventArgs {
    public OnMatchStartEventArgs(float matchStartTime, float matchDuration, float roundDuration) {
        MatchStartTime = matchStartTime;
        MatchDuration = matchDuration;
        RoundDuration = roundDuration;
    }
    public readonly float MatchStartTime;
    public readonly float MatchDuration;
    public readonly float RoundDuration;
}
public class Timer : MonoBehaviour {
    [Tooltip( "Match duration in seconds" )]
    [SerializeField] private float _matchDurationSeconds = 300f;
    [Tooltip( "Round duration in seconds" )]
    [SerializeField] private float _roundDurationSeconds = 10f;
    
    public event EventHandler<OnMatchStartEventArgs> OnMatchStart;
    public event EventHandler OnRoundEnd;
    
    private float _matchStartTime;
    private float _roundStartTime;
    
    private void Update() {
        if (Time.time >= _roundStartTime + _roundDurationSeconds) {
            OnRoundEnd?.Invoke(this, EventArgs.Empty);
            _roundStartTime = Time.time;
        }
    }   
    public void StartTimer() {
        _matchStartTime = Time.time;
        _roundStartTime = Time.time;
        OnMatchStart?.Invoke(this, new OnMatchStartEventArgs(_matchStartTime, _matchDurationSeconds, _roundDurationSeconds)); 
    }
}
