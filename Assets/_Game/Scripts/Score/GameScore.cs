using TMPro;
using UnityEngine;

public class GameScore
{
    public enum PlayerType
    {
        P1,
        P2
    }

    private PlayerType _activePlayer = PlayerType.P1;
    private int _p1Score = 0;
    private int _p2Score = 0;

    public int P1Score
    {
        get { return _p1Score; }
    }
    public int P2Score
    {
        get { return _p2Score; }
    }
    public PlayerType ActivePlayer
    {
        get { return _activePlayer; }
    }

    public void ToggleActivePlayer()
    {
        _activePlayer = _activePlayer == PlayerType.P1 ? PlayerType.P2 : PlayerType.P1;
    }

    public void GameScore_OnNewLightTile(object sender, System.EventArgs e)
    {
        if (_activePlayer == PlayerType.P1)
        {
            _p1Score++;
        }
        else
        {
            _p2Score++;
        }
    }
}
