using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayersUI : MonoBehaviour
{
    [SerializeField] private Color _lightColor;
    [SerializeField] private Color _shadowColor;
    [SerializeField] private Image _player1Image;
    [SerializeField] private Image _player2Image;
        
    void Start() {
        _player1Image.color = _lightColor;
        _player2Image.color = _shadowColor;
        GameManager.Instance.OnRoundEnd += PlayersUI_OnRoundEnd;
    }
    private void PlayersUI_OnRoundEnd(object sender, EventArgs e) {
        SwapColor();        
    }
    private void SwapColor() {
        (_player1Image.color, _player2Image.color) = (_player2Image.color, _player1Image.color);
    }

}
