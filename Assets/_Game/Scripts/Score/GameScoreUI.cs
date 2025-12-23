using System.Linq;
using TMPro;
using UnityEngine;

public class GameScoreUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _p1Score;

    [SerializeField]
    private TextMeshProUGUI _p2Score;

    private void Start()
    {
        GameManager.Instance.Grid.OnNewLightTile += GameScoreUI_OnLightTileChanged;
    }

    private void OnDestroy()
    {
        GameManager.Instance.Grid.OnNewLightTile -= GameScoreUI_OnLightTileChanged;
    }

    private void GameScoreUI_OnLightTileChanged(object sender, System.EventArgs e)
    {
        if (_p1Score != null)
        {
            _p1Score.text = GameManager
                .Instance.PlayerManager.Players.First(p => p.Role == PlayerManager.PlayerRole.Light)
                .Score.ToString();
        }
        if (_p2Score != null)
        {
            _p2Score.text = GameManager
                .Instance.PlayerManager.Players.First(p => p.Role == PlayerManager.PlayerRole.Skull)
                .Score.ToString();
        }
    }
}
