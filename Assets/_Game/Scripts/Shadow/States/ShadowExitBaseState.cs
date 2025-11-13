using UnityEngine;

/// <summary>
/// State for shadows when they exit their starting base.
/// Handles initial upward movement and then lateral movement until they join normal AI behavior.
/// </summary>
public class ShadowExitBaseState : IShadowState
{
    #region Phase Definition
    private enum Phase
    {
        MovingUp,
        MovingSide
    }
    #endregion

    #region Fields
    private Phase _phase = Phase.MovingUp;
    private Vector2Int _targetCell = new Vector2Int(-1, 2);
    private Vector2Int _sideDir;
    private bool _directionChosen = false;
    #endregion

    #region IShadowState Implementation
    public void Enter(ShadowController shadow)
    {
        _phase = Phase.MovingUp;
        _directionChosen = false;
        shadow.CurrentDirection = Vector2Int.up;
        shadow.Movement.OnMove(Vector2.up);
    }

    public void Exit(ShadowController shadow)
    {
        shadow.Movement.Stop();
    }

    public void Update(ShadowController shadow)
    {
        var grid = GameManager.Instance.Grid;
        var currentCell = Grid.WorldToCell(shadow.transform.position);

        switch (_phase)
        {
            case Phase.MovingUp:
                HandleMovingUpPhase(shadow, currentCell);
                break;

            case Phase.MovingSide:
                HandleMovingSidePhase(shadow, currentCell, grid);
                break;
        }
    }
    #endregion

    #region Phase Handlers

    /// <summary>
    /// Handles vertical movement until reaching the target cell.
    /// </summary>
    private void HandleMovingUpPhase(ShadowController shadow, Vector3Int currentCell)
    {
        if (currentCell.x == _targetCell.x && currentCell.y >= _targetCell.y)
        {
            _phase = Phase.MovingSide;
        }
        else
        {
            shadow.CurrentDirection = Vector2Int.up;
            shadow.Movement.OnMove(Vector2.up);
        }
    }

    /// <summary>
    /// Handles horizontal movement after reaching vertical target.
    /// Chooses random left or right direction and moves until a walkable path is found.
    /// </summary>
    private void HandleMovingSidePhase(ShadowController shadow, Vector3Int currentCell, Grid grid)
    {
        if (!_directionChosen)
        {
            _sideDir = (Random.value < 0.5f) ? Vector2Int.left : Vector2Int.right;
            _directionChosen = true;
        }

        Vector3Int nextCell = new Vector3Int(currentCell.x + _sideDir.x, currentCell.y, 0);

        if (grid.IsWalkable(nextCell))
        {
            shadow.CurrentDirection = _sideDir;
            shadow.Movement.OnMove(_sideDir);
        }
        else
        {
            shadow.ShadowBehaviorCycle.StartBehaviorCycle();
        }
    }

    #endregion
}
