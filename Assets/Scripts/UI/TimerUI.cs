using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class Extensions {
    public static void SetText(this TMP_Text text, string value) {
        text.text = value;
    }
}
public class TimerUI : MonoBehaviour
{
    [SerializeField] TMP_Text _matchTimeLeftText;
    [SerializeField] TMP_Text _roundTimeLeftText;
    [SerializeField] Slider _matchSlider;
    [SerializeField] Slider _roundSlider;

    private float _matchStartTime; 
    private float _roundStartTime;
    private float _matchDurationSeconds;
    private float _roundDurationSeconds;

    private float _pauseTime = 0;

    private bool _isEnded;
    private void Awake() {
        GameManager.Instance.Timer.OnMatchStart += TimerUI_OnMatchStart;
        GameManager.Instance.Timer.OnRoundEnd += (sender, args) => {
            _roundStartTime = Time.time;
        };
        GameManager.Instance.Timer.OnMatchEnd += (sender, args) =>
        {
            _isEnded = true;
            Debug.Log("Match EndMatch");
        };
        GameManager.Instance.Timer.OnMatchPause += (sender, args) =>
        {
            _pauseTime = Time.time;
        };
        GameManager.Instance.Timer.OnMatchResume += (sender, args) =>
        {
            float diff = Time.time - _pauseTime;
            _matchStartTime += diff;
            _roundStartTime += diff;
            _pauseTime = 0;
        };
    }
    
 
    private void Update() {
        if (_pauseTime > 0) return; 
        if (_isEnded) return;
        float roundTimeLeft = (_roundStartTime + _roundDurationSeconds) - Time.time;
        float roundTimeLeftPercent = roundTimeLeft / _roundDurationSeconds;
        float matchTimeLeft = (_matchStartTime + _matchDurationSeconds) - Time.time;
        float matchTimeLeftPercent = matchTimeLeft / _matchDurationSeconds;
        if (matchTimeLeft <= 0) return;
        if (roundTimeLeft <= 0) return;
        _roundSlider.value = roundTimeLeftPercent;
        _matchSlider.value = matchTimeLeftPercent;
        
        UpdateTimerDisplay();
    }
    private void UpdateTimerDisplay() {
        TimeSpan matchTimeLeft = TimeSpan.FromSeconds((_matchStartTime + _matchDurationSeconds) - Time.time);
        TimeSpan roundTimeLeft = TimeSpan.FromSeconds((_roundStartTime + _roundDurationSeconds) - Time.time);
        if(matchTimeLeft.Seconds < 0 || roundTimeLeft.Seconds < 0) return;
        _matchTimeLeftText.SetText($"{matchTimeLeft.Minutes:00}:{matchTimeLeft.Seconds:00}");
        _roundTimeLeftText.SetText($"{roundTimeLeft.TotalMinutes:00}:{roundTimeLeft.TotalSeconds:00}");
    }

    private void TimerUI_OnMatchStart(object sender, OnMatchStartEventArgs e) {
        _matchStartTime = Time.time;
        _roundStartTime = Time.time;
        _matchDurationSeconds = e.MatchDuration;
        _roundDurationSeconds = e.RoundDuration;
        UpdateTimerDisplay();
    }
}
