using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public enum SpawnType {
    Discrete,
    Continous,
    Random
}

public class PowerupSpawner : MonoBehaviour {
    [Header("Settings")]
    [SerializeField] private SpawnType _spawnType = SpawnType.Discrete;
    [MinMaxRangeSlider(0, 10f)]
    [SerializeField] private Vector2 _spawnTimeRange = new Vector2(3, 8);
    [SerializeField] private int _powerupCount = 3;

    [Header("Powerups")]
    [SerializeField] private GameObject _powerupPrefab;
    [SerializeField] private Grid _grid;

    private readonly List<Coroutine> _runningCoroutines = new List<Coroutine>();
    private readonly List<PowerupCollision> _powerups = new List<PowerupCollision>();
    private int[] randomIdx;

    private void Start() {

        if (_grid == null) {
            Debug.LogError("Grid not assigned to PowerupSpawner!");
            return;
        }

        GameManager.Instance.Timer.OnRoundStart += PowerupSpawner_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnd += PowerupSpawner_OnRoundEnd;

        

        SpawnPowerups();
    }

    private void OnDestroy() {
        GameManager.Instance.Timer.OnRoundStart -= PowerupSpawner_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnd -= PowerupSpawner_OnRoundEnd;
    }

    // ---------------------------------------------------------------------
    // EVENTS
    // ---------------------------------------------------------------------
    private void PowerupSpawner_OnRoundEnd(object sender, EventArgs e) {
        StopAllRunningCoroutines();
        DeactivateAllPowerups();
    }

    private void PowerupSpawner_OnRoundStart(object sender, EventArgs e) {
        float delay = UnityEngine.Random.Range(_spawnTimeRange.x, _spawnTimeRange.y);
        randomIdx = GenerateRandomPowerupIndices(_powerups.Count, _powerupCount);

        switch (_spawnType) {
            case SpawnType.Discrete:
                RunCoroutine(ActivateAllAfterDelay(delay));
                break;
            case SpawnType.Continous:
                for (int i = 0; i < _powerupCount; i++) {
                    float t = UnityEngine.Random.Range(_spawnTimeRange.x, _spawnTimeRange.y);
                    RunCoroutine(ActivateSingleAfterDelay(randomIdx[i], t));
                }
                break;
            case SpawnType.Random:
                int rnd = UnityEngine.Random.Range(0, _powerups.Count);
                RunCoroutine(ActivateSingleAfterDelay(rnd, delay));
                break;
        }
    }
    private int[] GenerateRandomPowerupIndices(int max, int count) {
        List<int> list = new List<int>();
        
        for (int i = 0; i < max; i++)
            list.Add(i);

        foreach (var i in list) {
            Debug.Log(i);
        }
        for (int i = 0; i < max; i++) {
            int k = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[k]) = (list[k], list[i]);
        }

        int[] result = list.GetRange(0, count).ToArray();
        

        return result;
    }

    private void SpawnPowerups() {
        List<Vector3> spawnPoints = _grid.GetPowerupSpawnPoints();

        for (int i = 0; i < spawnPoints.Count; i++) {
            GameObject powerup = Instantiate(_powerupPrefab, spawnPoints[i], Quaternion.identity);
            PowerupCollision powerupCollision = powerup.GetComponent<PowerupCollision>();
            powerupCollision.Disable();
            _powerups.Add(powerupCollision);
        }
    }
    
    private IEnumerator ActivateAllAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        for (int i = 0; i < _powerupCount; i++) {
            _powerups[randomIdx[i]].Enable();
        }
    }

    private IEnumerator ActivateSingleAfterDelay(int index, float delay) {
        yield return new WaitForSeconds(delay);
        if (_powerups[index] != null)
            _powerups[index].Enable();
    }

    private void DeactivateAllPowerups() {
        foreach (var p in _powerups)
            if (p != null) p.Disable();
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
