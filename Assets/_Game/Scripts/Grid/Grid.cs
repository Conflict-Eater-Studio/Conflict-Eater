using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        Portals,
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

    [Tooltip("Tilemap for portals")]
    [SerializeField]
    private Tilemap _tilemapPortals;

    [Tooltip("Tilemap for active lighted floors")]
    [SerializeField]
    private Tilemap _tilemapActiveLight;

    [Tooltip("Tile used to indicate lighted floor")]
    [SerializeField]
    private TileBase _lightTile;

    [Tooltip("Portal tile")]
    [SerializeField]
    private AnimatedTile _portalTile;

    [Tooltip("Portal prefab")]
    [SerializeField]
    private GameObject _portalPrefab;

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

    [Tooltip("Spawn point for Shadow exit base (cell coordinates)")]
    [SerializeField]
    private Vector2Int _shadowExitBaseCell = new Vector2Int(-2, -2);

    [Tooltip("Portal data (serialized for editor, instantiated at runtime)")]
    [SerializeField]
    private List<PortalData> _portalData = new List<PortalData>();

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

    private List<GridPortal> _portals = new List<GridPortal>();

    private int _maxLitTiles = 0;
    private int _litTileCount = 0;
    public int LitTileCount
    {
        get { return _litTileCount; }
    }

    [SerializeField]
    private float _powerUpSpawnInterval = 20f;

    private void Awake()
    {
        GameManager.Instance.RegisterGrid(this);
        GameManager.Instance.Timer.OnRoundEnd += OnRoundEnd;

        InstantiatePortals();

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

    public List<Vector3> GetPowerupSpawnPoints()
    {
        List<Vector3> spawnPoints = new List<Vector3>();
        for (int i = 0; i < _powerUpSpawnCells.Count; i++)
        {
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
    /// Finds the paired portal position for the given portal ID and cell position.
    /// </summary>
    /// <param name="portalId">ID of portal pair to find</param>
    /// <param name="cellPosition">Cell position of the portal to find its pair for</param>
    /// <returns></returns>
    public Vector3Int? FindPortalPairPosition(uint portalId, Vector2Int cellPosition)
    {
        var pairedPortals = _portals
            .Where(p => p.PortalId == portalId && p.CellPosition != cellPosition)
            .ToList();
        if (pairedPortals.Count > 0)
        {
            return new Vector3Int(
                pairedPortals[0].CellPosition.x,
                pairedPortals[0].CellPosition.y,
                0
            );
        }
        return null;
    }

    /// <summary>
    /// Starts the cooldown on the paired portal (the one at the destination).
    /// </summary>
    /// <param name="portalId">Portal ID to find pair</param>
    /// <param name="cellPosition">Cell position of the portal that initiated the teleport</param>
    public void StartPortalCooldown(uint portalId, Vector2Int cellPosition)
    {
        var pairedPortals = _portals
            .Where(p => p.PortalId == portalId && p.CellPosition != cellPosition)
            .ToList();

        foreach (var portal in pairedPortals)
        {
            portal.StartCooldown();
        }
    }

    /// <summary>
    /// Gets the portal tilemap for animation control.
    /// </summary>
    /// <returns>The portal tilemap</returns>
    public Tilemap GetPortalTilemap()
    {
        return _tilemapPortals;
    }

    /// <summary>
    /// Instantiates portal GameObjects from serialized PortalData at runtime.
    /// </summary>
    private void InstantiatePortals()
    {
        if (_portalPrefab == null)
            return;

        foreach (PortalData data in _portalData)
        {
            Vector3Int cellPos = new Vector3Int(data.CellPosition.x, data.CellPosition.y, 0);
            Vector3 worldPos = _tilemapPortals.GetCellCenterWorld(cellPos);

            // Change the portal tile color
            _tilemapPortals.SetColor(cellPos, data.PortalColor);

            GameObject portalObj = Instantiate(
                _portalPrefab,
                worldPos,
                Quaternion.identity,
                _tilemapPortals.transform
            );
            GridPortal portal = portalObj.GetComponent<GridPortal>();

            if (portal != null)
            {
                portal.PortalId = data.PortalId;
                portal.CellPosition = data.CellPosition;
                portal.PortalColor = data.PortalColor;
                portal.CooldownDuration = data.PortalCooldown;
                _portals.Add(portal);
            }
        }
    }

    /// <summary>
    /// Adds portal data (used by editor scripts).
    /// </summary>
    /// <param name="data">Portal data to add</param>
    public void AddPortalData(PortalData data)
    {
        // Only 2 portals with the same ID are allowed
        if (_portalData.FindAll(p => p.PortalId == data.PortalId).Count < 2)
        {
            _portalData.Add(data);
        }
    }

    /// <summary>
    /// Removes portal data by cell position (used by editor scripts).
    /// </summary>
    /// <param name="cellPosition">Cell position of portal to remove</param>
    /// <returns>True if portal was removed, false otherwise</returns>
    public bool RemovePortalData(Vector2Int cellPosition)
    {
        PortalData portal = _portalData.Find(p => p.CellPosition == cellPosition);
        if (portal != null)
        {
            _portalData.Remove(portal);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets all portal data (used by editor scripts).
    /// </summary>
    public List<PortalData> GetPortalData()
    {
        return _portalData;
    }

    /// <summary>
    /// Checks if the given portal ID is available (i.e. less than 2 portals with the same ID exist).
    /// </summary>
    /// <param name="portalId">Wanted ID</param>
    /// <returns>True if the ID is available, false otherwise.</returns>
    public bool IsIdAvailable(uint portalId)
    {
        return _portalData.FindAll(p => p.PortalId == portalId).Count < 2;
    }

    /// <summary>
    /// Returns the next available portal ID.
    /// </summary>
    /// <returns>Available portal ID</returns>
    public uint GetNextAvailablePortalId()
    {
        if (_portalData.Count == 0)
            return 1;

        _portalData.Sort((a, b) => a.PortalId.CompareTo(b.PortalId));
        uint nextId = _portalData.Last().PortalId;
        return IsIdAvailable(nextId) ? nextId : nextId + 1;
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
        _tilemapActiveLight.ClearAllTiles();
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
        ResetPlayer(PlayerManager.PlayerRole.Light, SpawnPointType.LightRandom);
        ResetPlayer(PlayerManager.PlayerRole.Skull, SpawnPointType.Shadow);

        PowerUp[] powerUps = FindObjectsByType<PowerUp>(FindObjectsSortMode.None);
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
    private void ResetPlayer(PlayerManager.PlayerRole playerType, SpawnPointType spawnPointType)
    {
        var player = GameManager.Instance.PlayerManager.GetPlayerOfType(playerType);
        player.transform.position = GetSpawnPoint(spawnPointType);

        if (playerType == PlayerManager.PlayerRole.Light)
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
            case TilemapType.Portals:
                tilemap = GameManager.Instance.Grid._tilemapPortals;
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
            case TilemapType.Portals:
                tilemap = GameManager.Instance.Grid._tilemapPortals;
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

    public Vector2Int GetShadowExitBaseCell()
    {
        return _shadowExitBaseCell;
    }

}
