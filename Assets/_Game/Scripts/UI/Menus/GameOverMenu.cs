using System.Linq;
using TMPro;
using UnityEngine;

public class GameOverMenu : MenuBase
{
    [SerializeField]
    private TextMeshProUGUI _winnerText;

    [SerializeField]
    private TextMeshProUGUI _p1ScoreText;

    [SerializeField]
    private TextMeshProUGUI _p2ScoreText;

    public void OnBtnMainMenu()
    {
        Debug.Log("Main Menu Button Clicked");
        MenuManager.Instance.LoadMainMenuAndReset();
    }

    public void UpdateText()
    {
        var winner = GameManager
            .Instance.PlayerManager.Players.OrderByDescending(p => p.Score)
            .First()
            .Index;

        string winnerStr =
            winner == PlayerManager.PlayerIndex.P1 ? "Player 1 Wins!" : "Player 2 Wins!";

        _winnerText.SetText(winnerStr);
        _p1ScoreText.SetText(
            "Score: "
                + GameManager
                    .Instance.PlayerManager.Players.First(p =>
                        p.Index == PlayerManager.PlayerIndex.P1
                    )
                    .Score
        );
        _p2ScoreText.SetText(
            "Score: "
                + GameManager
                    .Instance.PlayerManager.Players.First(p =>
                        p.Index == PlayerManager.PlayerIndex.P2
                    )
                    .Score
        );
    }
}
