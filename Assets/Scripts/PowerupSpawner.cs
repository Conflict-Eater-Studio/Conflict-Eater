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
    [SerializeField] private Vector2 _spawnWindow;
    [SerializeField] private int _powerupCount = 3;
    [SerializeField] private SpawnType _spawnType = SpawnType.AllAtOnce;

    [SerializeField] private GameObject _powerupPrefab;
    [SerializeField] private Grid _grid;

    private List<Vector3> spawnPoints = new List<Vector3>();
    private List<Coroutine> runningCoroutines = new List<Coroutine>();
    void Start() {
        GameManager.Instance.Timer.OnRoundStart += PowerupSpawner_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnd += PowerupSpawner_OnRoundEnd;
        spawnPoints = _grid.GetPowerupSpawnPoints(_powerupCount);
    }

    private void PowerupSpawner_OnRoundEnd(object sender, EventArgs e) {
        StopAllRunningCoroutines();
    }

    private void PowerupSpawner_OnRoundStart(object sender, EventArgs e) {
        foreach (var spawnPoint in spawnPoints) {
            Debug.Log(spawnPoint);
        }
        float randomTime = UnityEngine.Random.Range(_spawnWindow.x, _spawnWindow.y);
        switch (_spawnType) {
            case SpawnType.AllAtOnce:
                Debug.Log("Spawning powerups at once");
                RunCoroutine(SpawnPowerups(randomTime));
                return;
            case SpawnType.Continous:
                Debug.Log("Spawning powerups continously");
                for (int i = 0; i < _powerupCount; i++) {
                    randomTime = UnityEngine.Random.Range(_spawnWindow.x, _spawnWindow.y);
                    RunCoroutine(SpawnPowerupWithDelay(spawnPoints[i], randomTime));
                }
                return;
        }
    }
    
    private IEnumerator SpawnPowerups(float delay) {
        yield return new WaitForSeconds(delay);
        Debug.Log("Spawning powerups");
        for (int i = 0; i < _powerupCount; i++) {
           SpawnPowerup(spawnPoints[i]);
        }
    }
    private IEnumerator SpawnPowerupWithDelay(Vector2 spawnPoint, float delay) {
        yield return new WaitForSeconds(delay);
        Instantiate(_powerupPrefab, spawnPoint, Quaternion.identity);
    }
    private void SpawnPowerup(Vector2 spawnPoint) {
        Instantiate(_powerupPrefab, spawnPoint, Quaternion.identity);
    }
    private void RunCoroutine(IEnumerator routine) {
        Coroutine c = StartCoroutine(routine);
        runningCoroutines.Add(c);
    }

    private void StopAllRunningCoroutines() {
        foreach (var c in runningCoroutines)
            if (c != null)
                StopCoroutine(c);
        runningCoroutines.Clear();
    }
}
