using TMPro;
using UnityEngine;

public class CoinCollector : MonoBehaviour
{
    private TextMeshProUGUI _p1Text;
    private TextMeshProUGUI _p2Text;

    private int _p1Score = 0;
    private int _p2Score = 0;

    private PlayerType _activePlayer = PlayerType.P1;

    public enum PlayerType
    {
        P1,
        P2
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Coin"))
        {
            AddScore();
            Destroy(collision.gameObject);
        }
    }

    public void SetInfoText(TextMeshProUGUI p1Text, TextMeshProUGUI p2Text)
    {
        _p1Text = p1Text; 
        _p2Text = p2Text;
    }

    public void AddScore()
    {
        if (_activePlayer == PlayerType.P1)
        {
            _p1Score++;
            if (_p1Text != null) _p1Text.text = _p1Score.ToString();
        }
        else
        {
            _p2Score++;
            if (_p2Text != null) _p2Text.text = _p2Score.ToString();
        }
    }

    public void ToggleActivePlayer()
    {
        _activePlayer = _activePlayer == PlayerType.P1 ? PlayerType.P2 : PlayerType.P1;
    }

    public void SetP1Text(TextMeshProUGUI text) => _p1Text = text;
    public void SetP2Text(TextMeshProUGUI text) => _p2Text = text;

    public int GetP1Score() => _p1Score;
    public int GetP2Score() => _p2Score;
    public PlayerType GetActivePlayer() => _activePlayer;
}
