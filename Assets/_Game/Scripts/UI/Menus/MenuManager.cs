using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    public enum Menu
    {
        Main,
        Settings,
        Credits,
        Controls,
        Pause,
        GameOver,
    }

    public enum Scene
    {
        MainMenu,
        Game,
    }

    [Serializable]
    public struct MenuData
    {
        public GameObject Canvas;
        public GameObject Selected;
    }

    [SerializeField]
    private EventSystem _eventSystem;

    [Header("Menu GameObjects")]
    [SerializeField]
    private MenuData _mainMenu;

    [SerializeField]
    private MenuData _settingsMenu;

    [SerializeField]
    private MenuData _creditsMenu;

    [SerializeField]
    private MenuData _controlsMenu;

    [SerializeField]
    private MenuData _pauseMenu;

    [SerializeField]
    private MenuData _gameOverMenu;

    private Dictionary<Menu, MenuData> _subMenus;
    private Stack<Tuple<Menu, MenuData>> _openedMenus = new Stack<Tuple<Menu, MenuData>>();

    public static readonly Dictionary<Scene, string> Scenes = new()
    {
        { Scene.MainMenu, "MainMenu" },
        { Scene.Game, "SampleScene" },
    };

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        _subMenus = new Dictionary<Menu, MenuData>
        {
            { Menu.Main, _mainMenu },
            { Menu.Settings, _settingsMenu },
            { Menu.Credits, _creditsMenu },
            { Menu.Controls, _controlsMenu },
            { Menu.Pause, _pauseMenu },
            { Menu.GameOver, _gameOverMenu },
        };
    }

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        OpenSubMenu(Menu.Main);
    }

    public void OpenSubMenu(Menu menu)
    {
        // Check if the wanted menu exists
        if (_subMenus.ContainsKey(menu) == false)
        {
            Debug.LogError($"MenuManager: Menu {menu} not found!");
            return;
        }

        // Save the currently selected object before modifying anything
        GameObject currentlySelected = _eventSystem.currentSelectedGameObject;

        // Update the last menu's saved selected object with what's currently selected
        Tuple<Menu, MenuData> lastMenuTuple = null;
        if (_openedMenus.Count > 0)
        {
            lastMenuTuple = _openedMenus.Pop();
            var updatedMenuData = new MenuData
            {
                Canvas = lastMenuTuple.Item2.Canvas,
                Selected = currentlySelected,
            };
            _openedMenus.Push(new(lastMenuTuple.Item1, updatedMenuData));

            // Hide last menu
            _subMenus[lastMenuTuple.Item1].Canvas.SetActive(false);
        }

        _openedMenus.Push(
            new(
                menu,
                new MenuData
                {
                    Canvas = _subMenus[menu].Canvas,
                    Selected = _subMenus[menu].Selected,
                }
            )
        );

        // Show new menu
        _subMenus[menu].Canvas.SetActive(true);
        _eventSystem.SetSelectedGameObject(_subMenus[menu].Selected);
    }

    public void CloseLastSubMenu()
    {
        if (_openedMenus.Count == 0)
            return;

        Tuple<Menu, MenuData> currentMenu = _openedMenus.Pop();
        _subMenus[currentMenu.Item1].Canvas.SetActive(false);

        Tuple<Menu, MenuData> lastMenu = _openedMenus.Count > 0 ? _openedMenus.Peek() : null;
        if (lastMenu != null)
        {
            _subMenus[lastMenu.Item1].Canvas.SetActive(true);
            // Only set selected object if we have one saved
            if (lastMenu.Item2.Selected != null)
            {
                _eventSystem.SetSelectedGameObject(lastMenu.Item2.Selected);
            }
        }
    }

    public void CloseAllSubMenus()
    {
        while (_openedMenus.Count > 0)
        {
            Tuple<Menu, MenuData> lastMenu = _openedMenus.Pop();
            _subMenus[lastMenu.Item1].Canvas.SetActive(false);
        }
    }

    public bool IsLastMenuOfType(Menu menu)
    {
        if (_openedMenus.Count == 0)
            return false;

        Tuple<Menu, MenuData> currentMenu = _openedMenus.Peek();
        return currentMenu.Item1 == menu;
    }

    public AsyncOperation LoadSceneAsync(Scene scene, LoadSceneMode mode = LoadSceneMode.Single)
    {
        string sceneName = Scenes[scene];
        return SceneManager.LoadSceneAsync(sceneName, mode);
    }

    public AsyncOperation UnloadSceneAsync(Scene scene)
    {
        string sceneName = Scenes[scene];
        return SceneManager.UnloadSceneAsync(sceneName);
    }

    public T GetScript<T>(Menu menu)
    {
        if (_subMenus == null || !_subMenus.TryGetValue(menu, out var data))
        {
            throw new ArgumentException($"MenuManager: Menu {menu} not found!");
        }

        return data.Canvas.GetComponent<T>();
    }
}
