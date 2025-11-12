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
    private float _matchTime;
    private float _roundTime;
    private float _matchDurationSeconds;
    private float _roundDurationSeconds;
    private float _matchCountdown;
    private float _roundCountdown;
    private bool _isGameRunning;
    private bool _isGamePaused;
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        if (GameManager.Instance?.Timer == null)
        {
            Debug.LogError("TimerUI: GameManager or Timer not initialized.");
            enabled = false;
            return;
        }
        SubscribeEvents();
        _isGameRunning = false;
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void Update()
    {
        if (!_isGameRunning || _isGamePaused) return;

        _matchTime += Time.deltaTime;
        _roundTime += Time.deltaTime;

        UpdateSliders();
    }
    #endregion

    #region Event Subscription
    private void SubscribeEvents()
    {
        var timer = GameManager.Instance.Timer;
        timer.OnMatchStart += TimerUI_OnMatchStart;
        timer.OnRoundEnd += TimerUI_OnRoundEnd;
        timer.OnMatchEnd += TimerUI_OnMatchEnd;
        timer.OnMatchPause += TimerUI_OnMatchPause;
        timer.OnMatchResume += TimerUI_OnMatchResume;
    }

    private void UnsubscribeEvents()
    {
        if (GameManager.Instance?.Timer == null) return;

        var timer = GameManager.Instance.Timer;
        timer.OnMatchStart -= TimerUI_OnMatchStart;
        timer.OnRoundEnd -= TimerUI_OnRoundEnd;
        timer.OnMatchEnd -= TimerUI_OnMatchEnd;
        timer.OnMatchPause -= TimerUI_OnMatchPause;
        timer.OnMatchResume -= TimerUI_OnMatchResume;
    }
    #endregion

    #region Event Handlers
    private void TimerUI_OnMatchStart(object sender, OnMatchStartEventArgs e) {
        _matchCountdown = e.MatchCountdown;
        _roundCountdown = e.RoundCountdown;
        _matchDurationSeconds = e.MatchDuration;
        _roundDurationSeconds = e.RoundDuration;
        _matchTime = 0f;
        _roundTime = 0f;
        _matchSlider.value = 1f;
        _roundSlider.value = 1f;
        StartCoroutine(StartMatch());
    }
    private IEnumerator StartMatch() {
        yield return StartCoroutine(RunCountdown(_matchCountdown));
        _isGameRunning = true;
    }

    private void TimerUI_OnRoundEnd(object sender, System.EventArgs e) {
        _roundTime = 0f;
        _roundSlider.value = 1f;
        StartCoroutine(RunCountdown(_roundCountdown));
    }

    private void TimerUI_OnMatchEnd(object sender, System.EventArgs e) {
        _matchCountdownText.text = "";
        _isGameRunning = false;
        StopAllCoroutines();
    }
    private void TimerUI_OnMatchPause(object sender, System.EventArgs e) => _isGamePaused = true;
    private void TimerUI_OnMatchResume(object sender, System.EventArgs e) => _isGamePaused = false;
    #endregion

    #region Private Methods
    private void UpdateSliders()
    {
        float roundTimeLeft = 1f - (_roundTime / _roundDurationSeconds);
        float matchTimeLeft = 1f - (_matchTime / _matchDurationSeconds);

        _roundSlider.value = Mathf.Clamp01(roundTimeLeft);
        _matchSlider.value = Mathf.Clamp01(matchTimeLeft);
    }
    private IEnumerator RunCountdown(float duration) {
        float remaining = duration;
        while (remaining > 0f) {
            _matchCountdownText.SetText($"{Mathf.CeilToInt(remaining)}");
            yield return new WaitForEndOfFrame();
            remaining -= Time.deltaTime;
        }
        _matchCountdownText.text = "";
    }

    
    #endregion
}
