using UnityEngine;

public class PowerupSpawner : MonoBehaviour
{
    [Tooltip("Number of powerups to spawn in game")]
    [SerializeField] private int _powerupCount = 1;
    [SerializeField] private GameObject _powerupPrefab;
    [SerializeField] Grid _grid;
    void Start() {
        SpawnPowerup();
    }
    private void SpawnPowerup() {
        Vector2 spawnPoint = _grid.GetSpawnPoint(Grid.SpawnPointType.PowerUpRandom);
        Instantiate(_powerupPrefab, spawnPoint, Quaternion.identity);
    }
    
}
