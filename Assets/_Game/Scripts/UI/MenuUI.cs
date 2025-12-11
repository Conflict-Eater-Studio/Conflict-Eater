/*
using System;
using UnityEngine;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _pauseButton;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _endRoundButton;
    private void OnEnable() {
        _startButton.onClick.AddListener(GameManager.Instance.Timer.Run);
        _pauseButton.onClick.AddListener(GameManager.Instance.Timer.Pause);
        _resumeButton.onClick.AddListener(GameManager.Instance.Timer.Resume);
        _endRoundButton.onClick.AddListener(GameManager.Instance.Timer.EndRound);       
    }
    private void OnDisable() {
        _startButton.onClick.RemoveListener(GameManager.Instance.Timer.Run);
        _pauseButton.onClick.RemoveListener(GameManager.Instance.Timer.Pause);
        _resumeButton.onClick.RemoveListener(GameManager.Instance.Timer.Resume);
        _endRoundButton.onClick.RemoveListener(GameManager.Instance.Timer.EndRound);       
    }
 
}
*/
