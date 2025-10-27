using System;
using UnityEngine;

public class GameManager : Singleton<GameManager> {

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
//TODO: FIX THE ROUND TIMER INSTEAD showing n seconds its show n - 1 seconds