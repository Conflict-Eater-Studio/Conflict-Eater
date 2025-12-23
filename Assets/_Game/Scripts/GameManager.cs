using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    [SerializeField]
    private GameObject _globalVolume;

    [SerializeField]
    public Match Timer;

    [SerializeField]
    private GameObject _particleSystem;

    [SerializeField]
    private PlayerSpawner _playerSpawner;

    public bool IsFrightenedShadowState = false;

    public PlayerManager PlayerManager { get; private set; }
    public Grid Grid { get; private set; }
    public PlayerSpawner PlayerSpawner => _playerSpawner;
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
            Grid.OnNewLightTile += GameManager_OnNewLightTile;
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

        _globalVolume.SetActive(true);
    }

    private void OnMatchEnd(object sender, EventArgs e)
    {
        MenuManager.Instance.OpenSubMenu(MenuManager.Menu.GameOver);
        MenuManager.Instance.GetScript<GameOverMenu>(MenuManager.Menu.GameOver).UpdateText();
    }

    private void GameManager_OnNewLightTile(object sender, EventArgs e)
    {
        PlayerManager.Players.First(p => p.Role == PlayerManager.PlayerRole.Light).AddScore(1);
    }

    private void OnRoundEnd(object sender, EventArgs e)
    {
        // Swap players and update score
        PlayerManager.SwapPlayerRoles(
            PlayerManager.PlayerRole.Light,
            PlayerManager.PlayerRole.Skull
        );
        foreach (var p in PlayerManager.Players)
        {
            p.StartNewRound();
        }
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
            Grid.OnNewLightTile -= GameManager_OnNewLightTile;
        }
    }
}
