using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePause : MonoBehaviour
{
    public void OnBtnExit()
    {
        // NOTE: Remove the GameManager instance to avoid carrying over game state to the main menu
        if (GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }

        SceneManager.LoadSceneAsync("MainMenu");
    }

    public void OnBtnSettings()
    {
        // WARNING: Replace with scene management system
        SceneManager.LoadSceneAsync("SettingsMenu", LoadSceneMode.Additive);
    }

    // NOTE: Implement resume functionality
    public void OnBtnResume() { }
}
