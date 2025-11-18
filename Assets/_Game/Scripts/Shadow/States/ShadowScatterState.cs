using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// State for shadows when they enter scatter mode.
/// Shadows move towards their scatter target, trying to avoid reversing direction.
/// </summary>
public class ShadowScatterState : IShadowState
{
    ShadowState IShadowState.State => ShadowState.Scatter;
    #region Constants and Fields
    private static readonly Vector2Int[] Directions =
    {
        new(0, 1),   
        new(-1, 0),  
        new(1, 0),   
        new(0, -1)   
    };

    private const float DecisionInterval = 0.13f;

    private Vector2Int _targetCell;
    private Vector2Int _lastDirection;
    private Vector3Int _currentCell;
    private float _moveTimer;
    #endregion

    #region IShadowState Implementation
    public void Enter(ShadowController shadow)
    {
        //Debug.Log("Enter Scatter State");
        _targetCell = GameManager.Instance.Grid.GetScatterTargetByType(shadow.Type) ?? Vector2Int.zero;
        _lastDirection = Vector2Int.zero;
        shadow.CurrentDirection = Vector2Int.zero;
    }

    public void Exit(ShadowController shadow)
    {

    }

    public void Update(ShadowController shadow)
    {
        _moveTimer += Time.deltaTime;
        if (_moveTimer < DecisionInterval)
            return;

        _moveTimer = 0f;

        var grid = GameManager.Instance.Grid;
        var currentPosition = shadow.transform.position;
        _currentCell = Grid.WorldToCell(currentPosition);

        var nextDirection = ChooseBestDirection(grid, _currentCell);
        if (nextDirection == Vector2Int.zero)
            return;

        _lastDirection = nextDirection;
        shadow.CurrentDirection = nextDirection;
        shadow.Movement.OnMove(nextDirection);
    }
    #endregion

    #region Direction Selection

    /// <summary>
    /// Chooses the best direction to move based on distance to scatter target and walkable tiles.
    /// Prevents moving directly backwards.
    /// </summary>
    private Vector2Int ChooseBestDirection(Grid grid, Vector3Int currentCell)
    {
        var targetWorldPos = Grid.GetCellCenterWorld(new Vector3Int(_targetCell.x, _targetCell.y, 0));

        return Directions
            .Where(dir => dir != -_lastDirection)                 
            .Where(dir => grid.IsWalkable(currentCell + (Vector3Int)dir))
            .OrderBy(dir =>
            {
                var nextWorld = Grid.GetCellCenterWorld(currentCell + (Vector3Int)dir);
                return Vector2.Distance(nextWorld, targetWorldPos);
            })
            .ThenBy(GetDirectionPriority)
            .FirstOrDefault();
    }

    /// <summary>
    /// Returns a fixed priority for directions to break ties.
    /// </summary>
    private static int GetDirectionPriority(Vector2Int direction)
    {
        return direction switch
        {
            { x: 0, y: 1 } => 0,
            { x: -1, y: 0 } => 1,
            { x: 1, y: 0 } => 2,
            { x: 0, y: -1 } => 3,
            _ => int.MaxValue
        };
    }

    #endregion
}
