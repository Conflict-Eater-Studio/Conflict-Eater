using UnityEngine;
using DG.Tweening;
using System.Collections;

/// <summary>
/// Handles player-specific UI for displaying the player's nickname at the start of each round.
/// Manages the animation effect (pulsing) to help the player locate themselves on the map,
/// and swaps nicknames between rounds if needed.
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region Fields
    private GameObject _playerNameObj;
    private string _playerNick = "";
    private string _anotherPlayerNick = "";
    private float _baseScale = 0.05f;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Subscribes to round start and round end events when the object is initialized.
    /// </summary>
    protected virtual void Start()
    {
        GameManager.Instance.Timer.OnRoundStart += Timer_OnRoundStart;
        GameManager.Instance.Timer.OnRoundEnd += Timer_OnRoundEnd;
    }

    /// <summary>
    /// Unsubscribes from round events when the object is destroyed to prevent memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        GameManager.Instance.Timer.OnRoundStart -= Timer_OnRoundStart;

    }
    #endregion

    #region Event Handlers
    /// <summary>
    /// Called when a round ends.
    /// Swaps the current player nickname with another player's nickname.
    /// Updates the UI text accordingly.
    /// </summary>
    private void Timer_OnRoundEnd(object sender, OnRoundEndEventArgs e)
    {
        string playerNickPom = _playerNick;
        string anotherPlayerNickPom = _anotherPlayerNick;

        _playerNick = anotherPlayerNickPom;
        _anotherPlayerNick = playerNickPom;

        _playerNameObj.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = _playerNick;
    }

    /// <summary>
    /// Called when a round starts.
    /// Activates the player name UI and starts the pulsing animation coroutine.
    /// </summary>
    private void Timer_OnRoundStart(object sender, System.EventArgs e)
    {
        if (_playerNameObj == null) return;

        _playerNameObj.SetActive(true);
        StartCoroutine(PulseCoroutine());
    }
    #endregion

    #region Pulsing Animation
    /// <summary>
    /// Coroutine that animates the player's name UI with a pulsing "join join" effect.
    /// The UI scales up and down smoothly several times, then hides at the end.
    /// </summary>
    private IEnumerator PulseCoroutine()
    {
        Transform t = _playerNameObj.transform;

        float totalDuration = 2f;         
        int pulses = 3;                    
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = elapsed / totalDuration;

            float scaleMultiplier = 1f;

            for (int i = 0; i < pulses; i++)
            {
                float start = (float)i / pulses;
                float end = (float)(i + 1) / pulses;

                if (normalized >= start && normalized <= end)
                {
                    float localT = (normalized - start) * pulses; 
                    scaleMultiplier = 1f + Mathf.Sin(localT * Mathf.PI) * 1.5f; 
                    break;
                }
            }

            t.localScale = Vector3.one * _baseScale * scaleMultiplier;
            yield return null;
        }

        t.localScale = Vector3.one * _baseScale;

        _playerNameObj.SetActive(false);
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Sets the current player's nickname and updates the UI text.
    /// </summary>
    public void SetPlayerNick(string nick)
    {
        _playerNick = nick;

        _playerNameObj.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = _playerNick;
    }

    /// <summary>
    /// Assigns the UI object used to display the player's nickname.
    /// Initially disables the object until the round starts.
    /// </summary>
    public void SetPlayerNameObj(GameObject playerNameObj)
    {
        _playerNameObj = playerNameObj;
        _playerNameObj.SetActive(false);
    }

    /// <summary>
    /// Sets the nickname of another player, used for swapping at round end.
    /// </summary>
    public void SetAnotherPlayerNick(string nick)
    {
        _anotherPlayerNick = nick;
    }
    #endregion
}
