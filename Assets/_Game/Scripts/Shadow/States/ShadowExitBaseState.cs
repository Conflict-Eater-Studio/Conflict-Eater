using UnityEngine;

public class ShadowExitBaseState : IShadowState
{
    private enum Phase
    {
        MovingUp,
        MovingSide
    }

    private Phase _phase = Phase.MovingUp;
    private Vector2Int _targetCell = new Vector2Int(-1, 2);
    private Vector2Int _sideDir;
    private bool _directionChosen = false;

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
                if (currentCell.x == _targetCell.x && currentCell.y >= _targetCell.y)
                {
                    _phase = Phase.MovingSide;
                }
                else
                {
                    shadow.CurrentDirection = Vector2Int.up;
                    shadow.Movement.OnMove(Vector2.up);
                }
                break;

            case Phase.MovingSide:
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
                    shadow.SetState(new ShadowScatterState());
                }
                break;
        }
    }
}
