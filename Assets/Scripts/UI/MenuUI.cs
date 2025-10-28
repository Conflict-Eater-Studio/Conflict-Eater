using System;
using UnityEngine;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    private void OnEnable() {
        _startButton.onClick.AddListener(TaskOnClick);
    }
    private void OnDisable() {
        _startButton.onClick.RemoveListener(TaskOnClick);
    }
    void TaskOnClick() {
        GameManager.Instance.Timer.StartTimer();   
    }
}
