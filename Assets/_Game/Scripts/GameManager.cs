using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
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

    [SerializeField]
    private GridManager _gridManager; 

    public bool IsFrightenedShadowState = false;

    public PlayerManager PlayerManager { get; private set; }
    public Grid Grid { get; private set; }
    public PlayerSpawner PlayerSpawner => _playerSpawner;
    public GameObject ParticleSystem
    {
        get { return _particleSystem; }
        set { _particleSystem = value; }
    }
    public GridManager GridManager => _gridManager;

    public void RegisterGrid(Grid newGrid)
    {
        if (Grid != null)
        {
            Grid.OnNewLightTile -= GameManager_OnNewLightTile;
            Grid.OnAllLightTiles -= GameManager_OnAllLightTiles;
        }

        Grid = newGrid;
        Debug.Log("Register grid");

        if (Grid != null)
        {
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

        Timer.OnRoundEnded += OnRoundEnd;
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
        Timer.OnRoundEnded -= OnRoundEnd;
        if (Grid != null)
        {
            Grid.OnNewLightTile -= GameManager_OnNewLightTile;
        }
    }

    public Volume GetGlobalVolume()
    {
        return _globalVolume.GetComponent<Volume>();
    }
}
