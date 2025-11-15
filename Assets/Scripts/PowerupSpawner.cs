using System;
using UnityEngine;
using Random = System.Random;

public class PowerupSpawner : MonoBehaviour {
    [Tooltip("Number of powerups to spawn in game")]
    [SerializeField] private Vector2 _spawnWindow;

    [SerializeField] private int _powerupCount = 1;
    [SerializeField] private GameObject _powerupPrefab;
    [SerializeField] Grid _grid;
    void Start() {
        GameManager.Instance.Timer.OnRoundStart += PowerupSpawner_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnd += PowerupSpawner_OnRoundEnd;
    }
    private void PowerupSpawner_OnRoundEnd(object sender, EventArgs e) {
            CancelInvoke(nameof(SpawnPowerup));
    }
    private void PowerupSpawner_OnRoundStart(object sender, EventArgs e) {
           float randomTime = UnityEngine.Random.Range(_spawnWindow.x, _spawnWindow.y);
           Invoke(nameof(SpawnPowerup), randomTime);
    }
    private void SpawnPowerup() {
        Vector2 spawnPoint = _grid.GetSpawnPoint(Grid.SpawnPointType.PowerUpRandom);
        Instantiate(_powerupPrefab, spawnPoint, Quaternion.identity);
    }
    
}
