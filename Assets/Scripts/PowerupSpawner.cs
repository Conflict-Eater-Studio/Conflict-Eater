using UnityEngine;

public class PowerupSpawner : MonoBehaviour
{
    
    [SerializeField] Grid _grid;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        PrimitiveType[] powerupTypes = {PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Capsule};    
        Vector2 spawnPoint = _grid.GetSpawnPoint(Grid.SpawnPointType.PowerUp);
        var powerup = GameObject.CreatePrimitive(powerupTypes[Random.Range(0, powerupTypes.Length)]);
        Instantiate(powerup, spawnPoint, Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
