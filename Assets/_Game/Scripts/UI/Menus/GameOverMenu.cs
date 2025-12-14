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
        _winnerText.SetText(GameManager.Instance.Score.GetWinnerText());
        _p1ScoreText.SetText("Score: " + GameManager.Instance.Score.P1Score);
        _p2ScoreText.SetText("Score: " + GameManager.Instance.Score.P2Score);
    }
}
