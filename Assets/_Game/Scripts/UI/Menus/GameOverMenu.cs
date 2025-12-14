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
        MenuManager.Instance.LoadSceneAsync(MenuManager.Scene.MainMenu).completed += (asyncOp) =>
        {
            MenuManager.Instance.CloseAllSubMenus();
            MenuManager.Instance.OpenSubMenu(MenuManager.Menu.Main);
        };
    }

    public void UpdateText()
    {
        _winnerText.SetText(GameManager.Instance.Score.GetWinnerText());
        _p1ScoreText.SetText("Score: " + GameManager.Instance.Score.P1Score);
        _p2ScoreText.SetText("Score: " + GameManager.Instance.Score.P2Score);
    }
}
