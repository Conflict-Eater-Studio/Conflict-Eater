using System;
using UnityEngine;

public enum GameState {
    Pause,
    Running
}
public class GameManager : Singleton<GameManager> {

    [SerializeField] private GameState _gameState = GameState.Running;
    [SerializeField] public Timer Timer;
    [SerializeField] public RoleSwapper RoleSwapper;
    private void Awake() {
        if (Timer == null) {
            if (TryGetComponent(typeof(Timer), out Component compenent)) {
                Timer = compenent as Timer;
            }
            else {
                Timer = FindAnyObjectByType<Timer>();
            }
        }
    }
}
