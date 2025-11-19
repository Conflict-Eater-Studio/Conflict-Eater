using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameScore;

public class GameManager : Singleton<GameManager>
{
    [SerializeField]
    public Match Timer;

    [SerializeField]
    private GameObject _endGamePanel;

    [SerializeField]
    private TextMeshProUGUI _winnerText;

    public bool IsFrightenedShadowState = false;

    public PlayerManager PlayerManager { get; private set; }
    public Grid Grid { get; private set; }
    public GameScore Score { get; private set; } = new GameScore();

    public void RegisterGrid(Grid grid)
    {
        if (Grid == null)
        {
            Grid = grid;
            Grid.OnNewLightTile += Score.GameScore_OnNewLightTile;
            Grid.OnAllLightTiles += GameManager_OnAllLightTiles;
        }
    }

    public void BtnMainMenu()
    {
        // Remove the GameManager before going to main menu
        Destroy(gameObject);
        if (AudioManager.Instance.ActiveInstances.Count > 0)
        {
            AudioManager.Instance.StopEventInstance(
                AudioManager.Instance.ActiveInstances.First().Key,
                FMOD.Studio.STOP_MODE.ALLOWFADEOUT
            );
        }
        SceneManager.LoadSceneAsync("MainMenu");
    }

    private void Awake()
    {
        PlayerManager = new PlayerManager();

        Timer.OnRoundEnd += OnRoundEnd;
        Timer.OnMatchEnd += OnMatchEnd;

        _endGamePanel.GetComponentInChildren<Button>().onClick.AddListener(BtnMainMenu);
    }

    private void OnMatchEnd(object sender, EventArgs e)
    {
        _endGamePanel.SetActive(true);
        _winnerText.SetText(Score.GetWinnerText());
    }

    private void OnRoundEnd(object sender, EventArgs e)
    {
        PlayerManager.SwapPlayerGamepads(
            PlayerManager.PlayerType.Light,
            PlayerManager.PlayerType.Shadow
        );
        Score.ToggleActivePlayer();
    }

    private void GameManager_OnAllLightTiles(object sender, EventArgs e)
    {
        Timer.EndRound();
        // WARNING: Allows light player to continue as light after lighting all tiles
        // Maybe Timer should allow for EndRound() without triggering OnRoundEnd to avoid this double swap
        PlayerManager.SwapPlayerGamepads(
            PlayerManager.PlayerType.Light,
            PlayerManager.PlayerType.Shadow
        );
        Score.ToggleActivePlayer();
    }

    private void OnDestroy()
    {
        Timer.OnRoundEnd -= OnRoundEnd;
        if (Grid != null)
        {
            Grid.OnNewLightTile -= Score.GameScore_OnNewLightTile;
        }
    }
}
