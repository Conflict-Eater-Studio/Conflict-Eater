using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Grid : MonoBehaviour
{
    public enum SpawnPointType
    {
        Light,
        Shadow,
        PowerUp,
        PowerUpRandom,
        LightRandom,
    }

    public enum TilemapType
    {
        Walls,
        Floors,
        Light,
    }

    public event EventHandler OnNewLightTile;
    public event EventHandler OnAllLightTiles;

    [Tooltip("Tilemap for walls")]
    [SerializeField]
    private Tilemap _tilemapWalls;

    [Tooltip("Tilemap for floors")]
    [SerializeField]
    private Tilemap _tilemapFloors;

    [Tooltip("Tilemap for lighted floors")]
    [SerializeField]
    private Tilemap _tilemapLight;

    [Tooltip("Tilemap for inactive lighted floors")]
    [SerializeField]
    private Tilemap _tilemapInactiveLight;

    [Tooltip("Tile used to indicate lighted floor")]
    [SerializeField]
    private TileBase _lightTile;

    [Tooltip("Tile used to indicate inactive lighted floor")]
    [SerializeField]
    private TileBase _lightInactiveTile;

    [Tooltip("List of floor tile coordinates that are excluded from lighting")]
    [SerializeField]
    private List<Vector2Int> _lightExclusion = new List<Vector2Int>();

    [Tooltip("Spawn points for Light player (cell coordinates)")]
    [SerializeField]
    private List<Vector2Int> _lightSpawnCells = new List<Vector2Int>();

    [Tooltip("Spawn point for Shadow player (cell coordinates)")]
    [SerializeField]
    private Vector2Int _shadowSpawnCell = new Vector2Int(-2, -2);

    [Tooltip("Spawn points for Power-Up (cell coordinates)")]
    [SerializeField]
    private List<Vector2Int> _powerUpSpawnCells = new List<Vector2Int>();

    [Tooltip("Cells that shadows cannot step on")]
    [SerializeField]
    private List<Vector2Int> _shadowBlockedCells = new List<Vector2Int>();

    [Header("Shadow Home Targets")]
    [SerializeField]
    private List<Vector2Int> _homeTargets = new List<Vector2Int>();

    [System.Serializable]
    public class ShadowScatterTarget
    {
        public ShadowType shadowType;
        public Vector2Int targetCell;
    }

    [Tooltip("Target points for ghosts when in Scatter state (cell coordinates)")]
    [SerializeField]
    private List<ShadowScatterTarget> _scatterTargets = new List<ShadowScatterTarget>();

    private int _maxLitTiles = 0;
    private int _litTileCount = 0;
    public int LitTileCount
    {
        get { return _litTileCount; }
    }

    [SerializeField] private float _powerUpSpawnInterval = 20f;
    private Coroutine _powerUpSpawnRoutine;
    private int _lastSpawnIndex = -1;

    private void Awake()
    {
        GameManager.Instance.RegisterGrid(this);
        GameManager.Instance.Timer.OnRoundEnd += OnRoundEnd;

        // Calculate max lit tiles
        BoundsInt bounds = _tilemapFloors.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                if (IsWalkable(cellPos) && !IsLightTileExcluded(cellPos))
                {
                    _maxLitTiles++;
                }
            }
        }

        InitializeInactiveLightTiles();
    }

    private void InitializeInactiveLightTiles()
    {
        BoundsInt bounds = _tilemapFloors.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);

                if (IsWalkable(cellPos) && !IsLightTileExcluded(cellPos))
                {
                    _tilemapInactiveLight.SetTile(cellPos, _lightInactiveTile);
                }
            }
        }
    }


    // Reset map state on round end
    private void OnRoundEnd(object sender, EventArgs e)
    {
        ResetMapState();
    }

    private void Update() { }

    private void OnDestroy()
    {
        GameManager.Instance.Timer.OnRoundEnd -= OnRoundEnd;
    }

    /// <summary>
    /// Returns the spawn point for the given spawn point type.
    /// </summary>
    /// <param name="type">Spawn point type</param>
    /// <returns>The spawn point for the given spawn point type.</returns>
    public Vector2 GetSpawnPoint(SpawnPointType type)
    {
        switch (type)
        {
            case SpawnPointType.Light:
                if (_lightSpawnCells.Count > 0)
                {
                    Vector2Int spawnCell = _lightSpawnCells[0];
                    return _tilemapFloors.GetCellCenterWorld(
                        new Vector3Int(spawnCell.x, spawnCell.y, 0)
                    );
                }
                break;
            case SpawnPointType.Shadow:
                return _tilemapFloors.GetCellCenterWorld(
                    new Vector3Int(_shadowSpawnCell.x, _shadowSpawnCell.y, 0)
                );
            case SpawnPointType.PowerUp:
                if (_powerUpSpawnCells.Count > 0)
                {
                    Vector2Int spawnCell = _powerUpSpawnCells[0];
                    return _tilemapFloors.GetCellCenterWorld(
                        new Vector3Int(spawnCell.x, spawnCell.y, 0)
                    );
                }
                break;
            case SpawnPointType.PowerUpRandom:
                if (_powerUpSpawnCells.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, _powerUpSpawnCells.Count);
                    Vector2Int spawnCell = _powerUpSpawnCells[randomIndex];
                    return _tilemapFloors.GetCellCenterWorld(
                        new Vector3Int(spawnCell.x, spawnCell.y, 0)
                    );
                }
                break;
            case SpawnPointType.LightRandom:
                if (_lightSpawnCells.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, _lightSpawnCells.Count);
                    Vector2Int spawnCell = _lightSpawnCells[randomIndex];
                    return _tilemapFloors.GetCellCenterWorld(
                        new Vector3Int(spawnCell.x, spawnCell.y, 0)
                    );
                }
                break;
        }
        return Vector3.zero;
    }

    public List<Vector3> GetPowerupSpawnPoints() {
        List<Vector3> spawnPoints = new List<Vector3>();
        for (int i = 0; i < _powerUpSpawnCells.Count; i++) {
            Vector2Int spawnCell = _powerUpSpawnCells[i];
            Vector3Int worldCoordinates = new Vector3Int(spawnCell.x, spawnCell.y, 0);
            spawnPoints.Add(_tilemapFloors.GetCellCenterWorld(worldCoordinates));
        }

        return spawnPoints;
    }

    public List<Vector2Int> GetShadowHomeTargets()
    {
        return _homeTargets;
    }


    /// <summary>
    /// Sets the spawn point for the given player type.
    /// </summary>
    /// <param name="type">Player type</param>
    /// <param name="cellPosition">Cell position for spawn point</param>
    [Obsolete("Spawn points are now arrays. Use the editor to manage spawn points.")]
    public void SetSpawnPoint(PlayerManager.PlayerType type, Vector3Int cellPosition)
    {
        Vector2Int coord = new Vector2Int(cellPosition.x, cellPosition.y);
        switch (type)
        {
            case PlayerManager.PlayerType.Light:
                if (_lightSpawnCells.Count == 0)
                {
                    _lightSpawnCells.Add(coord);
                }
                else
                {
                    _lightSpawnCells[0] = coord;
                }
                break;
            case PlayerManager.PlayerType.Shadow:
                _shadowSpawnCell = coord;
                break;
        }
    }

    /// <summary>
    /// Checks if the tile at the given cell coord is walkable.
    /// (i.e. tile exists on the floor tilemap and does not exist on the wall tilemap)
    /// </summary>
    /// <param name="cellPosition">Cell position</param>
    /// <returns>True if tile is walkable, false otherwise.</returns>
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

    public bool IsWalkableForShadow(Vector3Int cellPosition)
    {
        if (IsShadowBlocked(cellPosition))
            return false;

        return IsWalkable(cellPosition);
    }

    public bool IsShadowBlocked(Vector3Int cellPosition)
    {
        return _shadowBlockedCells.Contains(new Vector2Int(cellPosition.x, cellPosition.y));
    }

    /// <summary>
    /// Sets the floor tile at the given cell coord to active.
    /// Increments lit tile count and invokes OnNewLightTile event.
    /// When all lightable tiles are lit, invokes OnAllLightTiles event.
    /// </summary>
    /// <param name="cellPosition">The coord of the tile to be activated</param>
    public void SetTileToLight(Vector3Int cellPosition)
    {
        if (IsLightTile(cellPosition) || IsLightTileExcluded(cellPosition))
        {
            return;
        }
        _tilemapLight.SetTile(cellPosition, _lightTile);

        _litTileCount++;
        OnNewLightTile?.Invoke(this, EventArgs.Empty);

        if (_maxLitTiles == _litTileCount)
        {
            OnAllLightTiles?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Resets all floor tiles to inactive.
    /// </summary>
    public void ClearLightTiles()
    {
        _tilemapLight.ClearAllTiles();
        _litTileCount = 0;
    }

    /// <summary>
    /// Returns true if the floor tile at the given cell coord is active.
    /// </summary>
    /// <param name="cellPosition">The coord of the tile to be checked</param>
    /// <returns></returns>
    public bool IsLightTile(Vector3Int cellPosition)
    {
        return _tilemapLight.GetTile(cellPosition) == _lightTile;
    }

    /// <summary>
    /// Returns true if the floor tile at the given cell coord is excluded from lighting.
    /// </summary>
    /// <param name="cellPosition">The coord of the tile to be checked</param>
    /// <returns>True if the tile is excluded from lighting, false otherwise.</returns>
    public bool IsLightTileExcluded(Vector3Int cellPosition)
    {
        return _lightExclusion.Contains(new Vector2Int(cellPosition.x, cellPosition.y));
    }

    /// <summary>
    /// Resets the map state by clearing all light tiles and resetting player positions.
    /// </summary>
    public void ResetMapState()
    {
        ClearLightTiles();
        ResetPlayer(PlayerManager.PlayerType.Light, SpawnPointType.LightRandom);
        ResetPlayer(PlayerManager.PlayerType.Shadow, SpawnPointType.Shadow);

        PowerUp[] powerUps = FindObjectsOfType<PowerUp>();
        foreach (var powerUp in powerUps)
        {
            Destroy(powerUp.gameObject);
        }
    }

    // NOTE: Move to player manager?
    /// <summary>
    /// Resets the specified player's position to their spawn point.
    /// Stops player movement.
    /// </summary>
    /// <param name="playerType">Player type to reset</param>
    /// <param name="spawnPointType">Spawn point type to reset</param>
    private void ResetPlayer(PlayerManager.PlayerType playerType, SpawnPointType spawnPointType)
    {
        var player = GameManager.Instance.PlayerManager.GetPlayerOfType(playerType);
        player.transform.position = GetSpawnPoint(spawnPointType);

        if (playerType == PlayerManager.PlayerType.Light)
        {
            player.GetComponentInChildren<LightPlayerController>().Movement.Stop();
        }
    }

    /// <summary>
    /// Converts a world position to a cell position in the specified tilemap.
    /// </summary>
    /// <param name="worldPosition">World position</param>
    /// <param name="tilemapType">Tilemap type</param>
    /// <returns>Cell position</returns>
    public static Vector3Int WorldToCell(
        Vector3 worldPosition,
        TilemapType tilemapType = TilemapType.Floors
    )
    {
        Tilemap tilemap = null;
        switch (tilemapType)
        {
            case TilemapType.Walls:
                tilemap = GameManager.Instance.Grid._tilemapWalls;
                break;
            case TilemapType.Floors:
                tilemap = GameManager.Instance.Grid._tilemapFloors;
                break;
            case TilemapType.Light:
                tilemap = GameManager.Instance.Grid._tilemapLight;
                break;
        }
        return tilemap.WorldToCell(worldPosition);
    }

    public static Vector3 GetCellCenterWorld(
        Vector3Int cellPosition,
        TilemapType tilemapType = TilemapType.Floors
    )
    {
        Tilemap tilemap = null;
        switch (tilemapType)
        {
            case TilemapType.Walls:
                tilemap = GameManager.Instance.Grid._tilemapWalls;
                break;
            case TilemapType.Floors:
                tilemap = GameManager.Instance.Grid._tilemapFloors;
                break;
            case TilemapType.Light:
                tilemap = GameManager.Instance.Grid._tilemapLight;
                break;
        }
        return tilemap.GetCellCenterWorld(cellPosition);
    }

    /// <summary>
    /// Returns the first scatter target position for the specified shadow type.
    /// </summary>
    /// <param name="type">The ShadowType to search for.</param>
    /// <returns>The target cell position (Vector2Int) if found, or null if not found.</returns>
    public Vector2Int? GetScatterTargetByType(ShadowType type)
    {
        foreach (var target in _scatterTargets)
        {
            if (target.shadowType == type)
            {
                return target.targetCell;
            }
        }
        return null;
    }
}
