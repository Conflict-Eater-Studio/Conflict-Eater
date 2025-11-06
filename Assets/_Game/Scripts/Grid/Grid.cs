using System;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Grid : MonoBehaviour
{
    public enum TilemapType
    {
        Walls,
        Floors,
        Light
    }

    public event EventHandler OnNewLightTile;
    public event EventHandler OnAllLightTiles;

    [Tooltip("Tilemap for walls")]
    [SerializeField] private Tilemap _tilemapWalls;
    [Tooltip("Tilemap for floors")]
    [SerializeField] private Tilemap _tilemapFloors;
    [Tooltip("Tilemap for lighted floors")]
    [SerializeField] private Tilemap _tilemapLight;

    [Tooltip("Tile used to indicate lighted floor")]
    [SerializeField] private TileBase _lightTile;

    [Tooltip("List of floor tile coordinates that are excluded from lighting")]
    [SerializeField] private List<Vector2Int> _lightExclusion = new List<Vector2Int>();

    private int _maxLitTiles = 0;
    private int _litTileCount = 0;
    public int LitTileCount
    {
        get { return _litTileCount; }
    }

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
    }

    // Reset map state on round end
    private void OnRoundEnd(object sender, EventArgs e)
    {
        ResetMapState();
    }

    private void Update()
    {
    }

    private void OnDestroy()
    {
        GameManager.Instance.Timer.OnRoundEnd -= OnRoundEnd;
    }

    /// <summary>
    /// Returns the spawn point for the given player type.
    /// </summary>
    /// <param name="type">Player type</param>
    /// <returns>The spawn point for the given player type.</returns>
    public Vector3 GetSpawnPoint(PlayerManager.PlayerType type)
    {
        switch (type)
        {
            case PlayerManager.PlayerType.Light:
                return _tilemapFloors.cellBounds.min + _tilemapFloors.cellSize / 2f;
            case PlayerManager.PlayerType.Shadow:
                return new Vector3(-1.5f, -1.5f, 0f);
        }
        return Vector3.zero;
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
        ResetPlayer(PlayerManager.PlayerType.Light);
        ResetPlayer(PlayerManager.PlayerType.Shadow);
    }

    // NOTE: Move to player manager?
    /// <summary>
    /// Resets the specified player's position to their spawn point.
    /// Stops player movement.
    /// </summary>
    /// <param name="playerType">Player type to reset</param>
    private void ResetPlayer(PlayerManager.PlayerType playerType)
    {
        var player = GameManager.Instance.PlayerManager.GetPlayerOfType(playerType);
        player.transform.position = GetSpawnPoint(playerType);

        if (playerType == PlayerManager.PlayerType.Light)
        {
            player.GetComponentInChildren<LightPlayerController>().Movement.Stop();
        }
        if (playerType == PlayerManager.PlayerType.Shadow)
        {
            //player.GetComponentInChildren<ShadowPlayerController>().Movement.Stop();
        }
    }

    /// <summary>
    /// Converts a world position to a cell position in the specified tilemap.
    /// </summary>
    /// <param name="worldPosition">World position</param>
    /// <param name="tilemapType">Tilemap type</param>
    /// <returns>Cell position</returns>
    public static Vector3Int WorldToCell(Vector3 worldPosition, TilemapType tilemapType = TilemapType.Floors)
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
}
