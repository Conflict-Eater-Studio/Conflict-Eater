using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Settings : MenuBase
{
    [SerializeField]
    private UnityEngine.UI.Slider _sliderMaster;

    [SerializeField]
    private UnityEngine.UI.Slider _sliderSFX;

    [SerializeField]
    private UnityEngine.UI.Slider _sliderBGM;

    [SerializeField]
    private UnityEngine.UI.Slider _sliderUI;

    private void Start()
    {
        _sliderMaster.value = AudioManager.Instance.MasterVolume;
        _sliderSFX.value = AudioManager.Instance.SFXVolume;
        _sliderBGM.value = AudioManager.Instance.MusicVolume;
        _sliderUI.value = AudioManager.Instance.UIVolume;
    }

    public void OnSliderChange(string type)
    {
        switch (type)
        {
            case "Master":
                AudioManager.Instance.MasterVolume = _sliderMaster.value;
                break;
            case "SFX":
                AudioManager.Instance.SFXVolume = _sliderSFX.value;
                break;
            case "BGM":
                AudioManager.Instance.MusicVolume = _sliderBGM.value;
                break;
            case "UI":
                AudioManager.Instance.UIVolume = _sliderUI.value;
                break;
        }
    }

    public void OnBtnSave()
    {
        AudioManager.Instance.SaveSettings();
    }

    public void OnBtnBack()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.CloseLastSubMenu();
        }
    }
}
