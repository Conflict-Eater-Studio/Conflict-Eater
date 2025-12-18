using FMODUnity;
using TMPro;
using UnityEngine;

public class GameScore
{
    public enum PlayerType
    {
        P1,
        P2,
    }

    private PlayerType _activePlayer = PlayerType.P1;
    private int _p1Score = 0;
    private int _p2Score = 0;
    private int _p1ScoreThisRound = 0;
    private int _p2ScoreThisRound = 0;

    public int P1Score
    {
        get { return _p1Score; }
    }
    public int P2Score
    {
        get { return _p2Score; }
    }
    public int P1ScoreThisRound
    {
        get { return _p1ScoreThisRound; }
    }
    public int P2ScoreThisRound
    {
        get { return _p2ScoreThisRound; }
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
            _p1ScoreThisRound++;
        }
        else
        {
            _p2Score++;
            _p2ScoreThisRound++;
        }
    }

    public void ResetScoreThisRound()
    {
        _p1ScoreThisRound = 0;
        _p2ScoreThisRound = 0;
    }

    public void AddScoreToActive(int score)
    {
        if (_activePlayer == PlayerType.P1)
        {
            _p1Score += score;
            _p1ScoreThisRound += score;
        }
        else
        {
            _p2Score += score;
            _p2ScoreThisRound += score;
        }
    }

    public PlayerType? GetWinnerType()
    {
        if (_p1Score > _p2Score)
            return PlayerType.P1;
        else if (_p2Score > _p1Score)
            return PlayerType.P2;
        else
            return null;
    }

    public string GetWinnerText()
    {
        if (_p1Score > _p2Score)
            return "Player 1 wins!";
        else if (_p2Score > _p1Score)
            return "Player 2 wins!";
        else
            return "It's a draw!";
    }
}
