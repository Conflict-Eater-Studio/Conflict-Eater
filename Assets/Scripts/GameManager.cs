using System;
using System.Collections.Generic;
using UnityEngine;
using static CoinCollector;

public enum GameState
{
    Pause,
    Running
}

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private GameState _gameState = GameState.Running;
    [SerializeField] public Match Timer;
    [SerializeField] public RoleSwapper RoleSwapper;

    [SerializeField] private float _matchDurationInMinutes;
    [SerializeField] private float _roundMaxDurationInMinutes;

    public event EventHandler<OnMatchStartEventArgs> OnMatchStart;
    public event EventHandler OnRoundEnd;

    private float _matchStartTime;
    private float _roundStartTime;

    public PlayerManager PlayerManager { get; private set; }
    public Grid Grid { get; private set; }

    [SerializeField] public Timer Timer;
    [SerializeField] public RoleSwapper RoleSwapper;

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

        if (Timer == null)
        {
            if (TryGetComponent(typeof(Timer), out Component compenent))
            {
                Timer = compenent as Timer;
            }
            else
            {
                Timer = FindAnyObjectByType<Timer>();
            }
        }
    }

    private void Update()
    {
        float roundEndTime = _roundStartTime + _roundMaxDurationInMinutes * 60;
        float timeLeftSeconds = Mathf.Max(0, roundEndTime - Time.time); // nie pozwalamy na ujemne
        TimeSpan timeLeft = TimeSpan.FromSeconds(timeLeftSeconds);
        if (timeLeft.TotalSeconds <= 0)
        {
            OnRoundEnd?.Invoke(this, EventArgs.Empty);
            _roundStartTime = Time.time;

        }
    }
}
