using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
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

    public void OnPointerEnter()
    {
        AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.UI.Cursor);
    }

    public void OnBtnClick()
    {
        AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.UI.Select);
    }

    public void OnBtnPlay()
    {
        // WARNING: Replace with scene management system
        SceneManager.LoadSceneAsync("SampleScene").completed += (_) =>
        {
            // WARNING: Playing music this way is temporary. We lose GUID of the music instance after loading the scene.
            // For now it's OK beacuse it's the only instance creted in the game and can be easily found
            AudioManager.Instance.PlaySound(AudioManager.Instance.FMODEvents.Music.Music8Bit);
        };
    }

    public void OnBtnTutorial()
    {
        AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.UI.Select);
        // WARNING: Replace with scene management system
        SceneManager.LoadSceneAsync("Tutorial", LoadSceneMode.Additive);
    }

    public void OnBtnSettings()
    {
        AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.UI.Select);
        // WARNING: Replace with scene management system
        SceneManager.LoadSceneAsync("Settings", LoadSceneMode.Additive);
    }

    public void OnBtnCredits()
    {
        AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.UI.Select);
        // WARNING: Replace with scene management system
        SceneManager.LoadSceneAsync("Credits", LoadSceneMode.Additive);
    }

    public void OnBtnExit()
    {
        Application.Quit();
    }
}
