using UnityEngine;

public class MenuBase : MonoBehaviour
{
    public void OnBtnPressed()
    {
        AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.UI.Select);
    }

    public void OnBtnReleased()
    {
        AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.UI.Select);
    }

    public void OnBtnHovered()
    {
        AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.UI.Cursor);
    }

    public void OnBtnClicked()
    {
        AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.UI.Select);
    }

    public void OnBtnCancelled()
    {
        AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.UI.Close);
    }
}
