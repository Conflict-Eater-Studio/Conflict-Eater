using UnityEngine;

[System.Serializable]
public class MenuBase
{
    public static void OnBtnSelect()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.UI.Select);
        }
    }

    public static void OnBtnCancel()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.UI.Cancel);
        }
    }

    public static void OnBtnHover()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.UI.Cursor);
        }
    }
}
