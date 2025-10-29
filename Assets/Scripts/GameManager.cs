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

        Timer.OnRoundEnd += OnRoundEnd;
    }

    private void OnRoundEnd(object sender, EventArgs e)
    {
        PlayerManager.SwapPlayerGamepads(PlayerManager.PlayerType.Light, PlayerManager.PlayerType.Shadow);
        
        GameObject lightPlayer = PlayerManager.GetPlayerOfType(PlayerManager.PlayerType.Light);
        lightPlayer.GetComponentInChildren<CoinCollector>().ToggleActivePlayer();
    }
}
