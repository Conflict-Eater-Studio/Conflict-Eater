using System.Linq;
using UnityEngine;

/// <summary>
/// Represents the chase behavior for shadows (ghosts) in a Pac-Man-like game.
/// Each shadow has a unique chase pattern based on its type (Blinky, Pinky, Inky, Clyde).
/// This state calculates target cells and chooses optimal directions to pursue the player.
/// </summary>
public class ShadowChaseState : IShadowState
{
    ShadowState IShadowState.State => ShadowState.Chase;

    #region Constants and Fields
    private static readonly Vector2Int[] Directions =
    {
        new(0, 1),
        new(-1, 0),
        new(1, 0),
        new(0, -1),
    };

    private const float DecisionInterval = 0.13f;
    private float _moveTimer;
    private Vector3Int _currentCell;

    private ShadowType _type;
    #endregion

    #region IShadowState Implementation
    /// <summary>
    /// Called when the shadow enters the chase state.
    /// Initializes movement direction and stores shadow type.
    /// </summary>
    public void Enter(ShadowController shadow)
    {
        _moveTimer = 0f;
        _type = shadow.Type;

        shadow.CurrentDirection = -shadow.CurrentDirection;

        shadow.Movement.OnMove(shadow.CurrentDirection);
    }

    /// <summary>
    /// Called when exiting the chase state.
    /// Currently no special exit behavior is required.
    /// </summary>
    public void Exit(ShadowController shadow) { }

    /// <summary>
    /// Called every frame while the shadow is in the chase state.
    /// Calculates the target cell and chooses the best direction to move toward it.
    /// </summary>
    public void Update(ShadowController shadow)
    {
        _moveTimer += Time.deltaTime;
        if (_moveTimer < DecisionInterval)
            return;

        _moveTimer = 0f;

        var grid = GameManager.Instance.Grid;
        _currentCell = Grid.WorldToCell(shadow.transform.position);

        Vector2Int targetCell = CalculateTargetCell(shadow);

        var nextDirection = ChooseBestDirection(
            grid,
            _currentCell,
            shadow.CurrentDirection,
            targetCell
        );

        if (nextDirection != Vector2Int.zero && nextDirection != shadow.CurrentDirection)
        {
            shadow.CurrentDirection = nextDirection;
            shadow.Movement.OnMove(nextDirection);
        }
    }

    #endregion

    #region Target Cell Calculation
    /// <summary>
    /// Calculates the AI target cell depending on shadow type.
    /// </summary>
    private Vector2Int CalculateTargetCell(ShadowController shadow)
    {
        switch (shadow.Type)
        {
            case ShadowType.Blinky:
                return CalculateTargetCellForBlinky();

            case ShadowType.Pinky:
                return CalculateTargetCellForPinky();

            case ShadowType.Inky:
                return CalculateTargetCellForInky();

            case ShadowType.Clyde:
                return CalculateTargetCellForClyde();

            default:
                return CalculateTargetCellForBlinky();
        }
    }

    private Vector2Int CalculateTargetCellForBlinky()
    {
        // Blinky directly chases the player
        Vector3Int playerPos = Grid.WorldToCell(
            GameManager
                .Instance.PlayerManager.GetPlayerOfType(PlayerManager.PlayerRole.Light)
                .transform.position
        );
        Vector2Int target = new Vector2Int(playerPos.x, playerPos.y);
        return target;
    }

    private Vector2Int CalculateTargetCellForPinky()
    {
        // Pinky tries to ambush the player 4 tiles ahead
        var playerGO = GameManager.Instance.PlayerManager.GetPlayerOfType(
            PlayerManager.PlayerRole.Light
        );
        Vector3Int playerCell = Grid.WorldToCell(playerGO.transform.position);

        Vector2Int playerDirection = playerGO
            .GetComponentInChildren<LightPlayerController>()
            .CurrentDirection;

        Vector2Int target = new Vector2Int(playerCell.x, playerCell.y);

        if (playerDirection == Vector2Int.up)
        {
            target += new Vector2Int(-4, 4);
        }
        else
        {
            target += playerDirection * 4;
        }

        return target;
    }

    private Vector2Int CalculateTargetCellForInky()
    {
        // Inky depends on both the player and Blinky
        var playerGO = GameManager.Instance.PlayerManager.GetPlayerOfType(
            PlayerManager.PlayerRole.Light
        );
        Vector3Int playerCell = Grid.WorldToCell(playerGO.transform.position);

        Vector2Int playerDirection = playerGO
            .GetComponentInChildren<LightPlayerController>()
            .CurrentDirection;

        Vector2Int pointAhead = new Vector2Int(playerCell.x, playerCell.y);
        if (playerDirection == Vector2Int.up)
            pointAhead += new Vector2Int(-2, 2);
        else
            pointAhead += playerDirection * 2;

        var shadowPlayerController = GameManager
            .Instance.PlayerManager.GetPlayerOfType(PlayerManager.PlayerRole.Skull)
            .GetComponentInChildren<ShadowPlayerController>();
        var blinky = shadowPlayerController.Shadows[0];
        if (blinky == null)
            return pointAhead;

        Vector3Int blinkyCell = Grid.WorldToCell(blinky.transform.position);

        Vector2Int vectorToBlinky = new Vector2Int(blinkyCell.x, blinkyCell.y) - pointAhead;

        Vector2Int rotatedVector = -vectorToBlinky;

        Vector2Int target = pointAhead + rotatedVector;
        return target;
    }

    private Vector2Int CalculateTargetCellForClyde()
    {
        // Clyde alternates between chasing and scattering
        var playerGO = GameManager.Instance.PlayerManager.GetPlayerOfType(
            PlayerManager.PlayerRole.Light
        );
        Vector3Int playerCell3D = Grid.WorldToCell(playerGO.transform.position);
        Vector2Int playerCell = new Vector2Int(playerCell3D.x, playerCell3D.y);

        var shadowPlayerController = GameManager
            .Instance.PlayerManager.GetPlayerOfType(PlayerManager.PlayerRole.Skull)
            .GetComponentInChildren<ShadowPlayerController>();
        Vector3Int clydeCell3D = Grid.WorldToCell(
            shadowPlayerController.Shadows[2].gameObject.transform.position
        );
        Vector2Int clydeCell = new Vector2Int(clydeCell3D.x, clydeCell3D.y);

        float distance = Vector2Int.Distance(clydeCell, playerCell);

        if (distance >= 8f)
        {
            return playerCell;
        }
        else
        {
            return GameManager.Instance.Grid.GetScatterTargetByType(_type) ?? Vector2Int.zero;
        }
    }
    #endregion

    #region Direction Selection
    /// <summary>
    /// Chooses the optimal direction to move based on target cell, avoiding reversing.
    /// </summary>
    private Vector2Int ChooseBestDirection(
        Grid grid,
        Vector3Int currentCell,
        Vector2Int currentDirection,
        Vector2Int targetCell
    )
    {
        var targetWorldPos = Grid.GetCellCenterWorld(new Vector3Int(targetCell.x, targetCell.y, 0));

        return Directions
            .Where(dir => dir != -currentDirection)
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
    /// Priority for direction tie-breaking: Up > Left > Right > Down
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
