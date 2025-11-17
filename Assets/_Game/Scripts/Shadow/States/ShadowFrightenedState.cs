using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// State for shadows when they are frightened (e.g. after Light picks up a power-up).
/// They move randomly, reversing direction once upon entering the state.
/// </summary>
public class ShadowFrightenedState : IShadowState
{
    #region Constants and Fields
    private static readonly Vector2Int[] Directions =
    {
        new(0, 1),  
        new(-1, 0), 
        new(1, 0),   
        new(0, -1)   
    };

    private const float DecisionInterval = 0.15f;

    private Vector2Int _lastDirection;
    private Vector3Int _currentCell;
    private float _moveTimer;

    #endregion

    #region IShadowState Implementation
    public void Enter(ShadowController shadow)
    {
        Debug.Log("Enter Frightened State");

        ShadowAppearanceManager shadowAppearanceManager = shadow.gameObject.GetComponent<ShadowAppearanceManager>();
        shadowAppearanceManager.SetFrightened(true);

        if (shadow.CurrentDirection != Vector2Int.zero)
        {
            shadow.CurrentDirection = -shadow.CurrentDirection;
            _lastDirection = shadow.CurrentDirection;
            shadow.Movement.OnMove(shadow.CurrentDirection);
        }

        _moveTimer = 0f;
    }

    public void Exit(ShadowController shadow)
    {
        ShadowAppearanceManager shadowAppearanceManager = shadow.gameObject.GetComponent<ShadowAppearanceManager>();
        shadowAppearanceManager.SetFrightened(false);
        shadowAppearanceManager.SetNormal();
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

        var nextDirection = ChooseRandomDirection(grid, _currentCell);
        if (nextDirection == Vector2Int.zero)
            return;

        _lastDirection = nextDirection;
        shadow.CurrentDirection = nextDirection;
        shadow.Movement.OnMove(nextDirection);
    }
    #endregion

    #region Direction Logic

    /// <summary>
    /// Chooses a random walkable direction, avoiding immediate reversal if possible.
    /// </summary>
    private Vector2Int ChooseRandomDirection(Grid grid, Vector3Int currentCell)
    {
        var validDirections = Directions
            .Where(dir => grid.IsWalkable(currentCell + (Vector3Int)dir))
            .ToList();

        if (validDirections.Count > 1)
        {
            validDirections.Remove(-_lastDirection);
        }

        if (validDirections.Count == 0)
            return Vector2Int.zero;

        int randomIndex = Random.Range(0, validDirections.Count);
        return validDirections[randomIndex];
    }

    #endregion
}
