using System;
using System.Reflection;
using TMPro;
using UnityEngine;

public static class TimerUIExtensions {
    public static void SetText(this TMP_Text text, string value) {
        text.text = value;
    }
}
public class TimerUI : MonoBehaviour
{
    [SerializeField] TMP_Text _matchTimeLeftText;
    [SerializeField] TMP_Text _roundTimeLeftText;

    private float _matchStartTime; 
    private float _roundStartTime;
    private float _matchDurationInMinutes;
    private float _roundDurationInMinutes;
    private void Start() {
        GameManager.Instance.OnMatchStart += TimerUI_OnMatchStart;
        GameManager.Instance.OnRoundEnd += (sender, args) => {
            _roundStartTime = Time.time;
        };
    }
    private void Update() {
        TimeSpan matchTimeLeft = TimeSpan.FromSeconds((_matchStartTime + _matchDurationInMinutes * 60) - Time.time);
        TimeSpan roundTimeLeft = TimeSpan.FromSeconds((_roundStartTime + _roundDurationInMinutes * 60) - Time.time);
        
        _matchTimeLeftText.SetText($"{matchTimeLeft.Minutes:00}:{matchTimeLeft.Seconds:00}");
        _roundTimeLeftText.SetText($"{roundTimeLeft.Minutes:00}:{roundTimeLeft.Seconds:00}");
    }

    private void TimerUI_OnMatchStart(object sender, OnMatchStartEventArgs e) {
        _matchStartTime = e.MatchStartTime;
        _roundStartTime = e.MatchStartTime;
        _matchDurationInMinutes = e.MatchDurationInMinutes;
        _roundDurationInMinutes = e.RoundDurationInMinutes;
    }
}
