using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MenuBase
{
    [SerializeField]
    private Button _btnPlay;

    [SerializeField]
    private Button _btnTutorial;

    [SerializeField]
    private Button _btnSettings;

    [SerializeField]
    private Button _btnCredits;

    [SerializeField]
    private Button _btnExit;

    public void OnBtnPlay()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.LoadSceneAsync(MenuManager.Scene.Game).completed += (_) =>
            {
                MenuManager.Instance.CloseAllSubMenus();

                // NOTE: Start playing game music, we don't store GUID for now
                AudioManager.Instance.PlaySound(AudioManager.Instance.FMODEvents.Music.Music8Bit);
            };
        }
    }

    public void OnBtnTutorial()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OpenSubMenu(MenuManager.Menu.Controls);
        }
    }

    public void OnBtnSettings()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OpenSubMenu(MenuManager.Menu.Settings);
        }
    }

    public void OnBtnCredits()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OpenSubMenu(MenuManager.Menu.Credits);
        }
    }

    public void OnBtnExit()
    {
        Application.Quit();
    }
}
