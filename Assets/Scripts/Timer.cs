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
    [SerializeField] private float _matchDuration;
    [Tooltip( "Round duration in seconds" )]
    [SerializeField] private float _roundMaxDuration;
    
    public event EventHandler<OnMatchStartEventArgs> OnMatchStart;
    public event EventHandler OnRoundEnd;
    
    private float _matchStartTime;
    private float _roundStartTime;
    
    private void Update() {
        TimeSpan timeLeft = TimeSpan.FromSeconds((_roundStartTime + _roundMaxDuration) - Time.time);
        if (timeLeft.TotalSeconds <= 0) {
            OnRoundEnd?.Invoke(this, EventArgs.Empty);
            _roundStartTime = Time.time;
        }
    }   
    public void Start() {
        _matchStartTime = Time.time;
        _roundStartTime = Time.time;
        OnMatchStart?.Invoke(this, new OnMatchStartEventArgs(_matchStartTime, _matchDuration, _roundMaxDuration)); 
    }
}
