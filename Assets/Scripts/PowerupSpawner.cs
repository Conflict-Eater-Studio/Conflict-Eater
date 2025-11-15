using System;
using UnityEngine;
using Random = System.Random;

public class PowerupSpawner : MonoBehaviour
{
    [Tooltip("Number of powerups to spawn in game")]
    [SerializeField] private float _minimumRoundTimePassed = 10f;
    [SerializeField] private float _maximumRoundTimePassed = 20f;

    [SerializeField] private int _powerupCount = 1;
    [SerializeField] private GameObject _powerupPrefab;
    [SerializeField] Grid _grid;
    void Start() {
        GameManager.Instance.Timer.OnRoundStart += PowerupSpawner_OnRoundStart;
    }
    private void PowerupSpawner_OnRoundStart(object sender, EventArgs e) {
           float randomTime = UnityEngine.Random.Range(_minimumRoundTimePassed, _maximumRoundTimePassed);
            Debug.Log(randomTime);
           Invoke("SpawnPowerup", randomTime);
    }
    private void SpawnPowerup() {
        Vector2 spawnPoint = _grid.GetSpawnPoint(Grid.SpawnPointType.PowerUpRandom);
        Instantiate(_powerupPrefab, spawnPoint, Quaternion.identity);
    }
    
}
