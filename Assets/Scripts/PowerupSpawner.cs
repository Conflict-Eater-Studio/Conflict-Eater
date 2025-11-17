using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpawnType {
    AllAtOnce,
    Continous,
    Random
}
public class PowerupSpawner : MonoBehaviour {
    [SerializeField] private Vector2 _spawnTimeRange;
    [SerializeField] private int _powerupCount = 3;
    [SerializeField] private SpawnType _spawnType = SpawnType.AllAtOnce;
    
    [SerializeField] List<Powerup> _powerups;
    [SerializeField] private GameObject _powerupPrefab;
    [SerializeField] private Grid _grid;

    private List<Vector3> _spawnPoints = new List<Vector3>();
    private readonly List<Coroutine> _runningCoroutines = new List<Coroutine>();
    private List<GameObject> powerups = new List<GameObject>();
    void Start() {
        GameManager.Instance.Timer.OnRoundStart += PowerupSpawner_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnd += PowerupSpawner_OnRoundEnd;
        _spawnPoints = _grid.GetPowerupSpawnPoints(_powerupCount);
    }

    private void PowerupSpawner_OnRoundEnd(object sender, EventArgs e) {
        StopAllRunningCoroutines();
        DestroyAllPowerups();
        powerups.Clear();
    }
    private void DestroyAllPowerups() {
        foreach (var powerup in powerups) {
            Destroy(powerup);
        }
    }

    private void PowerupSpawner_OnRoundStart(object sender, EventArgs e) {
        foreach (var spawnPoint in _spawnPoints) {
            Debug.Log(spawnPoint);
        }
        float randomTime = UnityEngine.Random.Range(_spawnTimeRange.x, _spawnTimeRange.y);
        switch (_spawnType) {
            case SpawnType.AllAtOnce:
                RunCoroutine(SpawnPowerups(randomTime));
                return;
            case SpawnType.Continous:
                for (int i = 0; i < _powerupCount; i++) {
                    randomTime = UnityEngine.Random.Range(_spawnTimeRange.x, _spawnTimeRange.y);
                    RunCoroutine(SpawnPowerupWithDelay(_spawnPoints[i], randomTime));
                }
                return;
        }
    }
    
    private IEnumerator SpawnPowerups(float delay) {
        yield return new WaitForSeconds(delay);
        Debug.Log("Spawning powerups");
        for (int i = 0; i < _powerupCount; i++) {
           SpawnPowerup(_spawnPoints[i]);
        }
    }
    private IEnumerator SpawnPowerupWithDelay(Vector2 spawnPoint, float delay) {
        yield return new WaitForSeconds(delay);
        SpawnPowerup(spawnPoint);
    }
    private void SpawnPowerup(Vector2 spawnPoint) {
        GameObject powerup = Instantiate(_powerupPrefab, spawnPoint, Quaternion.identity);
        powerups.Add(powerup);
    }
    private void RunCoroutine(IEnumerator routine) {
        Coroutine c = StartCoroutine(routine);
        _runningCoroutines.Add(c);
    }

    private void StopAllRunningCoroutines() {
        foreach (var c in _runningCoroutines)
            if (c != null)
                StopCoroutine(c);
        _runningCoroutines.Clear();
    }
}
