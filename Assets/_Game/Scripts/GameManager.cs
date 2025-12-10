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

    [SerializeField] private GameObject _particleSystem;

    public bool IsFrightenedShadowState = false;

    public PlayerManager PlayerManager { get; private set; }
    public Grid Grid { get; private set; }
    public GameScore Score { get; private set; } = new GameScore();
    public GameObject ParticleSystem
    {
        get { return _particleSystem; }
        set { _particleSystem = value; }
    }

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
        // Swap players and update score
        PlayerManager.SwapPlayerGamepads(
            PlayerManager.PlayerType.Light,
            PlayerManager.PlayerType.Shadow
        );
        Score.ToggleActivePlayer();

        // Note: PlayerSwapUI listens to PlayerManager.OnPlayerSwapped and will show the animation.
        // The animation will call Timer.StartRoundCountdown() when complete.
    }

    private void GameManager_OnAllLightTiles(object sender, EventArgs e)
    {
        Timer.EndRound();
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
