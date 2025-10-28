using System;
using System.Collections.Generic;
using UnityEngine;
using static CoinCollector;


public class OnMatchStartEventArgs : EventArgs {
    public OnMatchStartEventArgs(float matchStartTime, float matchDurationInMinutes, float roundDurationInMinutes) {
        MatchStartTime = matchStartTime;
        MatchDurationInMinutes = matchDurationInMinutes;
        RoundDurationInMinutes = roundDurationInMinutes;
    }
    public readonly float MatchStartTime;
    public readonly float MatchDurationInMinutes;
    public readonly float RoundDurationInMinutes;
}

public class GameManager : Singleton<GameManager> {
    
    [SerializeField] private float _matchDurationInMinutes;
    [SerializeField] private float _roundMaxDurationInMinutes;
    
    public event EventHandler<OnMatchStartEventArgs> OnMatchStart;
    public event EventHandler OnRoundEnd;
    
    private float _matchStartTime;
    private float _roundStartTime;

    public PlayerManager PlayerManager { get; private set; }
    public Grid Grid { get; private set; }

    public void RegisterGrid(Grid grid)
    {
        if (Grid == null)
        {
            Grid = grid;
        }
    }

    private void Awake()
    {
        PlayerManager = new PlayerManager();
    }

    private void Update() {
        float roundEndTime = _roundStartTime + _roundMaxDurationInMinutes * 60;
        float timeLeftSeconds = Mathf.Max(0, roundEndTime - Time.time); // nie pozwalamy na ujemne
        TimeSpan timeLeft = TimeSpan.FromSeconds(timeLeftSeconds);
        if (timeLeft.TotalSeconds <= 0) {
            OnRoundEnd?.Invoke(this, EventArgs.Empty);
            _roundStartTime = Time.time;
        }
    }
    public void StartGame() {
        _matchStartTime = Time.time;
        _roundStartTime = Time.time;
        OnMatchStart?.Invoke(this, new OnMatchStartEventArgs(_matchStartTime, _matchDurationInMinutes, _roundMaxDurationInMinutes)); 
    }
}
//TODO: FIX THE ROUND TIMER INSTEAD showing n seconds its show n - 1 seconds