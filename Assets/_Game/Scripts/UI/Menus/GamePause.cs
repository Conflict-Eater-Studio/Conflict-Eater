using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GamePause : MenuBase
{
    [SerializeField]
    private Button _btnResume;

    [SerializeField]
    private Button _btnSettings;

    public void OnBtnExit()
    {
        // NOTE: Remove the GameManager instance to avoid carrying over game state to the main menu
        if (GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.LoadSceneAsync(MenuManager.Scene.MainMenu).completed += (_) =>
            {
                // Open main menu after scene load
                MenuManager.Instance.CloseAllSubMenus();
                MenuManager.Instance.OpenSubMenu(MenuManager.Menu.Main);
            };
        }
    }

    public void OnBtnSettings()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OpenSubMenu(MenuManager.Menu.Settings);
        }
    }

    public void OnBtnResume()
    {
        if (GameManager.Instance?.Timer != null)
        {
            GameManager.Instance.Timer.Resume();
            MenuManager.Instance.CloseLastSubMenu();
        }
    }
}
