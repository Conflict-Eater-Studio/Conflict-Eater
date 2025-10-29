using System;
using UnityEngine;

public enum GameState {
    Pause,
    Running
}
public class GameManager : Singleton<GameManager> {

    [SerializeField] private GameState _gameState = GameState.Running;
    [SerializeField] public Match Timer;
    [SerializeField] public RoleSwapper RoleSwapper;
    private void Awake() {
        if (Timer == null) {
            if (TryGetComponent(typeof(Match), out Component compenent)) {
                Timer = compenent as Match;
            }
            else {
                Timer = FindAnyObjectByType<Match>();
            }
        }
    }
}
