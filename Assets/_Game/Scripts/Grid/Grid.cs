using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Grid : MonoBehaviour
{
    public enum SpawnPointType
    {
        Player,
        Ghost
    }

    // Tilemap
    [SerializeField] private Tilemap _tilemapWalls;
    [SerializeField] private Tilemap _tilemapFloors;

    [SerializeField] private GameObject _coin;
    private List<GameObject> _coins = new List<GameObject>();
    [SerializeField] private List<Vector2Int> _coinExclusion = new List<Vector2Int>();

    void Start()
    {
        SpawnCoins();
    }

    void Update()
    {
    }

    public Vector3 GetSpawnPoint(SpawnPointType type)
    {
        switch (type)
        {
            case SpawnPointType.Player:
                return _tilemapFloors.cellBounds.min + _tilemapFloors.cellSize / 2f;
            case SpawnPointType.Ghost:
                return new Vector3(0.5f, 0.5f, 0f);
        }
        return Vector3.zero;
    }

    public bool IsWalkable(Vector3Int cellPosition)
    {
        if (!_tilemapWalls.HasTile(cellPosition) && _tilemapFloors.HasTile(cellPosition))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void SpawnCoins()
    {
        BoundsInt bounds = _tilemapFloors.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                if (IsWalkable(cellPosition) && !_coinExclusion.Contains(new Vector2Int(x, y)))
                {
                    Vector3 worldPosition = _tilemapFloors.CellToWorld(cellPosition) + _tilemapFloors.cellSize / 2f;
                    GameObject coinInstance = Instantiate(_coin, worldPosition, Quaternion.identity);
                    _coins.Add(coinInstance);
                }
            }
        }
    }

    public void ResetMapState()
    {
        foreach (GameObject coin in _coins)
        {
            Destroy(coin);
        }
        _coins.Clear();
        SpawnCoins();

        // Reset player (temp by CoinCollector)
        var player = FindFirstObjectByType<CoinCollector>().gameObject;
        player.transform.position = GetSpawnPoint(SpawnPointType.Player);
        player.GetComponent<PlayerController>().Movement.Stop();
    }
}
