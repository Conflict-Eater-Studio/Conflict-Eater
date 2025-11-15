using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TimerUI : MonoBehaviour
{
    #region Serialized Fields
    
    [SerializeField] private Slider _matchSlider;
    [SerializeField] private Slider _roundSlider;
    [SerializeField] private TMP_Text _matchCountdownText;

    #endregion

    #region Private Fields
    private float _matchDurationSeconds;
    private float _roundDurationSeconds;
    
    private float _matchCountdown;
    private float _roundCountdown;
    
    private Match _timer;
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        _timer = GameManager.Instance.Timer;
        if (_timer == null)
        {
            Debug.LogError("TimerUI: Timer not initialized.");
            enabled = false;
            return;
        }
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void Update()
    {        
        if (_timer.CountdownRemaining >= 0f) {
            _matchCountdownText.SetText($"{Mathf.CeilToInt(_timer.CountdownRemaining)}");
        }
        else {
            _matchCountdownText.SetText("");
        }
        
        if (!_timer.IsGameRunning || _timer.IsGamePaused) return;

        UpdateSliders();
    }
    #endregion

    
    #region Event Subscription
    private void SubscribeEvents()
    {
        _timer.OnMatchStart += TimerUI_OnMatchStart;
        _timer.OnRoundEnd += TimerUI_OnRoundEnd;
        _timer.OnMatchEnd += TimerUI_OnMatchEnd;
    }

    private void UnsubscribeEvents()
    {
        if (_timer == null) return;

        _timer.OnMatchStart -= TimerUI_OnMatchStart;
        _timer.OnRoundEnd -= TimerUI_OnRoundEnd;
        _timer.OnMatchEnd -= TimerUI_OnMatchEnd;
    }
    #endregion

    #region Event Handlers
    private void TimerUI_OnMatchStart(object sender, OnMatchStartEventArgs e) {
        _matchDurationSeconds = e.MatchDuration;
        _roundDurationSeconds = e.RoundDuration;
        _matchSlider.value = 1f;
        _roundSlider.value = 1f;
    }

    private void TimerUI_OnRoundEnd(object sender, System.EventArgs e) {
        _roundSlider.value = 1f;
    }

    private void TimerUI_OnMatchEnd(object sender, System.EventArgs e) {
        _matchCountdownText.SetText("");
    }
    
    #endregion

    #region Private Methods
    private void UpdateSliders()
    {
        float roundTimeLeft = 1f - (_timer.RoundTime / _roundDurationSeconds);
        float matchTimeLeft = 1f - (_timer.MatchTime / _matchDurationSeconds);

        _roundSlider.value = Mathf.Clamp01(roundTimeLeft);
        _matchSlider.value = Mathf.Clamp01(matchTimeLeft);
    }


    
    #endregion
}
