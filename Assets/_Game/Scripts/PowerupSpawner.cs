using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerupSpawner : MonoBehaviour {
    [SerializeField] private float _firstPowerupSpawnTime = 5f;
    [SerializeField] private float _spawnDelta = 3f;
    [SerializeField] private int _powerupCount = 3;

    [Header("Powerups")]
    [SerializeField] private GameObject _powerupPrefab;
    [SerializeField] private List<Material> _powerupMaterials;

    private Grid _grid;
    private readonly List<Coroutine> _runningCoroutines = new List<Coroutine>();
    private readonly List<PowerupCollision> _powerups = new List<PowerupCollision>();

    private void Start() {
        _grid = GameManager.Instance.Grid;

        if (_grid == null) {
            Debug.LogError("Grid not assigned to PowerupSpawner!");
            return;
        }

        GameManager.Instance.Timer.OnRoundStart += PowerupSpawner_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnded += PowerupSpawner_OnRoundEnded;
    }

    private void OnDisable() {
        GameManager.Instance.Timer.OnRoundStart -= PowerupSpawner_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnded -= PowerupSpawner_OnRoundEnded;
    }

    private void PowerupSpawner_OnRoundEnded(object sender, EventArgs e) {
        StopAllRunningCoroutines();
        DeactivateAllPowerups();
    }

    private void PowerupSpawner_OnRoundStart(object sender, EventArgs e) {
        _grid = GameManager.Instance.Grid;
        SpawnPowerups();
        //_randomIdxs = GenerateRandomPowerupIndices(_powerups.Count, _powerupCount);
        
        _runningCoroutines.Add(StartCoroutine(ActivateSingleAfterDelay(0, _firstPowerupSpawnTime)));
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
            
            powerupCollision.SetRandomLightPowerup();
            powerupCollision.SetRandomShadowPowerup();

            MeshRenderer mr = powerup.GetComponentInChildren<MeshRenderer>();
            Material[] material = mr.materials;
            Debug.Log(mr.transform.name);
            if (mr == null) {
                Debug.LogError("No mesh renderer found on powerup prefab!");
            }
            switch (powerupCollision.GetLightPowerupType()) {
                case LightPowerupType.EmpathyMode:
                    material[0] = _powerupMaterials[0];
                    break;
                case LightPowerupType.SilentTreatment:
                    material[0] = _powerupMaterials[1];
                    break;
            }
            
            switch (powerupCollision.GetShadowPowerupType()) {
                case ShadowPowerupType.SarcasticSmile:
                    material[2] = _powerupMaterials[2];
                    break;
                case ShadowPowerupType.FarCry:
                    material[2] = _powerupMaterials[3];
                    break;
            }
            
            mr.materials = material;
            powerupCollision.Disable();
            _powerups.Add(powerupCollision);
        }
        for(int i = 0; i < _powerupCount - 1; i++) {
            int index = i;
            _powerups[index].OnPowerupCollected += (sender, args) => {
                _runningCoroutines.Add(StartCoroutine(ActivateSingleAfterDelay(index + 1, _spawnDelta)));
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

    private void DeactivateAllPowerups()
    {
        foreach (var p in _powerups)
        {
            if (p != null)
            {
                Destroy(p.gameObject);
            }
        }

        _powerups.Clear();
    }

    private void StopAllRunningCoroutines() {
        foreach (var c in _runningCoroutines)
            if (c != null)
                StopCoroutine(c);

        _runningCoroutines.Clear();
    }
}
