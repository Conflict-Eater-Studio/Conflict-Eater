using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Represents the "Eaten" state for shadows (ghosts) after they are eaten by the player.
/// In this state, the shadow returns to its home (ghost house) before respawning.
/// </summary>
public class ShadowEatenState : IShadowState
{
    public ShadowState State => ShadowState.Eaten;

    #region Constants and Fields
    private static readonly Vector2Int[] Directions =
    {
        new(0, 1),
        new(-1, 0),
        new(1, 0),
        new(0, -1),
    };

    private const float DecisionInterval = 0.13f;

    private List<Vector2Int> _homeTargets;
    private int _currentTargetIndex;
    private Vector2Int _lastDirection;
    private Vector3Int _currentCell;
    private float _moveTimer;
    #endregion

    #region IShadowState Implementation
    /// <summary>
    /// Called when the shadow enters the "Eaten" state.
    /// Initializes movement towards home and sets appearance to "dead".
    /// </summary>
    public void Enter(ShadowController shadow)
    {
        GameManager
            .Instance.PlayerManager.Players.First(p => p.Role == PlayerManager.PlayerRole.Light)
            .AddScore(10);
        _homeTargets = GameManager.Instance.Grid.GetShadowHomeTargets();
        _currentTargetIndex = 0;
        _lastDirection = Vector2Int.zero;
        shadow.CurrentDirection = Vector2Int.zero;

        shadow.Movement.Stop();

        var appearance = shadow.GetComponent<ShadowAppearanceManager>();
        appearance.SetDead();
    }

    /// <summary>
    /// Called when exiting the "Eaten" state.
    /// No special logic is needed.
    /// </summary>
    public void Exit(ShadowController shadow) 
    {
        shadow.OnRevive();
    }

    /// <summary>
    /// Called every frame while the shadow is in the "Eaten" state.
    /// Moves the shadow along the path to its home.
    /// </summary>
    public void Update(ShadowController shadow)
    {
        if (_homeTargets == null || _homeTargets.Count == 0)
            return;

        _moveTimer += Time.deltaTime;
        if (_moveTimer < DecisionInterval)
            return;

        _moveTimer = 0f;

        var grid = GameManager.Instance.Grid;
        var currentPosition = shadow.transform.position;
        _currentCell = Grid.WorldToCell(currentPosition);

        Vector2Int targetCell =
            _currentTargetIndex < _homeTargets.Count
                ? _homeTargets[_currentTargetIndex]
                : _homeTargets.Last();

        if ((Vector2Int)_currentCell == targetCell)
        {
            _currentTargetIndex++;

            if (
                _currentTargetIndex >= _homeTargets.Count
                && (Vector2Int)_currentCell == _homeTargets.Last()
            )
            {
                var appearance = shadow.GetComponent<ShadowAppearanceManager>();
                appearance.SetColorAfterEaten();
                appearance.SetColorBeforeFrightened();

                ShadowPlayerController owner =
                    shadow.GetComponentInParent<ShadowPlayerController>();
                if (owner != null)
                    shadow.StartCoroutine(DelayedUpdateAppearance(shadow));

                shadow.SetState(new ShadowScatterState());
                return;
            }

            if (_currentTargetIndex < _homeTargets.Count)
                targetCell = _homeTargets[_currentTargetIndex];
        }

        var nextDirection = ChooseBestDirection(grid, _currentCell, targetCell);
        if (nextDirection == Vector2Int.zero)
            return;

        _lastDirection = nextDirection;
        shadow.CurrentDirection = nextDirection;
        shadow.Movement.OnMove(nextDirection);
    }

    /// <summary>
    /// Delays updating the shadow's appearance to ensure proper timing with the player controller.
    /// </summary>
    private IEnumerator DelayedUpdateAppearance(ShadowController shadow)
    {
        yield return null;

        ShadowPlayerController owner = shadow.GetComponentInParent<ShadowPlayerController>();
        //owner.UpdateAppearance();
    }

    #endregion

    #region Direction Selection
    /// <summary>
    /// Determines the optimal direction toward the target cell, avoiding reversing.
    /// </summary>
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

    /// <summary>
    /// Direction priority for tie-breaking: Up > Left > Right > Down
    /// </summary>
    private static int GetDirectionPriority(Vector2Int direction)
    {
        return direction switch
        {
            { x: 0, y: 1 } => 0,
            { x: -1, y: 0 } => 1,
            { x: 1, y: 0 } => 2,
            { x: 0, y: -1 } => 3,
            _ => int.MaxValue,
        };
    }
    #endregion
}
