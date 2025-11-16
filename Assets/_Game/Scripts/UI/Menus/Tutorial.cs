using UnityEngine;

public class Tutorial : MonoBehaviour
{
    public void OnBtnBack()
    {
        AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.UI.Select);
        // WARNING: Replace with scene management system
        UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("Tutorial");
    }
}
