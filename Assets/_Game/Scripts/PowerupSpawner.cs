using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class PowerupSpawner : MonoBehaviour {
    [SerializeField] private float _spawnDelta = 3f;
    [SerializeField] private int _powerupCount = 3;

    [Header("Powerups")]
    [SerializeField] private GameObject _powerupPrefab;
    
    private Grid _grid;
    private readonly List<Coroutine> _runningCoroutines = new List<Coroutine>();
    private readonly List<PowerupCollision> _powerups = new List<PowerupCollision>();
    private int[] _randomIdxs;

    private void Start() {
        _grid = GameManager.Instance.Grid;

        if (_grid == null) {
            Debug.LogError("Grid not assigned to PowerupSpawner!");
            return;
        }

        GameManager.Instance.Timer.OnRoundStart += PowerupSpawner_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnd += PowerupSpawner_OnRoundEnd;
    }

    private void OnDisable() {
        GameManager.Instance.Timer.OnRoundStart -= PowerupSpawner_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnd -= PowerupSpawner_OnRoundEnd;
    }

    private void PowerupSpawner_OnRoundEnd(object sender, EventArgs e) {
        StopAllRunningCoroutines();
        DeactivateAllPowerups();
    }

    private void PowerupSpawner_OnRoundStart(object sender, EventArgs e) {
        if (GameManager.Instance.Timer.CurrentRound % 2 != 0) {
            _grid = GameManager.Instance.Grid;
            SpawnPowerups();
        };
        
        _randomIdxs = GenerateRandomPowerupIndices(_powerups.Count, _powerupCount);
        
        _runningCoroutines.Add(StartCoroutine(ActivateSingleAfterDelay(_randomIdxs[0], _spawnDelta)));
    
      
    }
    private int[] GenerateRandomPowerupIndices(int max, int count) {
        List<int> list = new List<int>();
        
        for (int i = 0; i < max; i++)
            list.Add(i);

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
            powerupCollision.OnPowerupCollected += (sender, args) => {
                _runningCoroutines.Add(StartCoroutine(ActivateSingleAfterDelay(_randomIdxs[(i + 1) % _powerupCount], _spawnDelta)));
            };
        }
    }
    private IEnumerator ActivateSingleAfterDelay(int index, float delay) {
        yield return new WaitForSeconds(delay);
        
        if (_powerups[index]) {
            PowerupCollision pc = _powerups[index];
            pc.Enable();
        }
    }
    
    private void DeactivateAllPowerups() {
        foreach (var p in _powerups)
            if (p != null) p.Disable();
    }
    
    private void StopAllRunningCoroutines() {
        foreach (var c in _runningCoroutines)
            if (c != null)
                StopCoroutine(c);

        _runningCoroutines.Clear();
    }
}
