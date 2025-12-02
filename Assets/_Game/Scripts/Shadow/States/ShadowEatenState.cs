using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShadowEatenState : IShadowState
{
    public ShadowState State => ShadowState.Eaten;

    #region Constants and Fields
    private static readonly Vector2Int[] Directions =
    {
        new(0, 1),   // Up
        new(-1, 0),  // Left
        new(1, 0),   // Right
        new(0, -1)   // Down
    };

    private const float DecisionInterval = 0.13f;

    private List<Vector2Int> _homeTargets;
    private int _currentTargetIndex;
    private Vector2Int _lastDirection;
    private Vector3Int _currentCell;
    private float _moveTimer;
    #endregion

    #region IShadowState Implementation
    public void Enter(ShadowController shadow)
    {
        GameManager.Instance.Score.AddScoreToActive(10);
        _homeTargets = GameManager.Instance.Grid.GetShadowHomeTargets();
        _currentTargetIndex = 0;
        _lastDirection = Vector2Int.zero;
        shadow.CurrentDirection = Vector2Int.zero;

        shadow.Movement.Stop();

        var appearance = shadow.GetComponent<ShadowAppearanceManager>();
        appearance.SetDead();
    }

    public void Exit(ShadowController shadow)
    {
        
    }

    public void Update(ShadowController shadow)
    {
        if (_currentTargetIndex >= _homeTargets.Count) {
            var appearance = shadow.GetComponent<ShadowAppearanceManager>();
            appearance.SetNormal();
            shadow.SetState(new ShadowScatterState());
            return;
        }
        
        if (_homeTargets == null || _homeTargets.Count == 0)
            return;

        _moveTimer += Time.deltaTime;
        if (_moveTimer < DecisionInterval)
            return;

        _moveTimer = 0f;

        var grid = GameManager.Instance.Grid;
        var currentPosition = shadow.transform.position;
        _currentCell = Grid.WorldToCell(currentPosition);

        Vector2Int targetCell = _homeTargets[_currentTargetIndex];

        if ((Vector2Int)_currentCell == targetCell)
        {
            _currentTargetIndex++;
            if (_currentTargetIndex >= _homeTargets.Count)
            {
                var appearance = shadow.GetComponent<ShadowAppearanceManager>();
                appearance.SetNormal();
                shadow.SetState(new ShadowScatterState());
                return;
            }
            targetCell = _homeTargets[_currentTargetIndex];
        }

        var nextDirection = ChooseBestDirection(grid, _currentCell, targetCell);
        if (nextDirection == Vector2Int.zero)
            return;

        _lastDirection = nextDirection;
        shadow.CurrentDirection = nextDirection;
        shadow.Movement.OnMove(nextDirection);
    }
    #endregion

    #region Direction Selection
    private Vector2Int ChooseBestDirection(Grid grid, Vector3Int currentCell, Vector2Int targetCell)
    {
        var targetWorldPos = Grid.GetCellCenterWorld(new Vector3Int(targetCell.x, targetCell.y, 0));

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
