using TMPro;
using UnityEngine;

public class CoinCollector : MonoBehaviour
{
    public enum PlayerType
    {
        P1,
        P2,
    }

    private TextMeshProUGUI _infoText;
    private int _score = 0;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Coin"))
        {
            _score++;
            _infoText.text = $"Score: {_score}";
            Destroy(collision.gameObject);
        }
    }

    public void SetInfoText(TextMeshProUGUI infoText) => _infoText = infoText;
}
