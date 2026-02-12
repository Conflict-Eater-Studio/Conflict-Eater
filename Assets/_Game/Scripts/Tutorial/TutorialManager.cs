using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private GameObject _lightTutorial;
    [SerializeField] private GameObject _skullTutorial;

    void Start()
    {
        if(GameManager.Instance)
        {
            GameManager.Instance.Timer.OnRoundStart += Timer_OnRoundStart;
            GameManager.Instance.Timer.OnRoundEnded += Timer_OnRoundEnded;

            PlayerSpawner.OnPlayersReadyToSpawn += PlayerSpawner_OnPlayersReadyToSpawn;
        }
    }

    private void PlayerSpawner_OnPlayersReadyToSpawn(object sender, System.EventArgs e)
    {
        var p1Role = GameManager.Instance.PlayerSpawner.GetRoleForPlayer(PlayerManager.PlayerIndex.P1);

        if (p1Role == PlayerManager.PlayerRole.Skull)
        {
            var lightRect = _lightTutorial.GetComponent<RectTransform>();
            lightRect.localPosition = new Vector2(-lightRect.localPosition.x, lightRect.localPosition.y);

            var skullRect = _skullTutorial.GetComponent<RectTransform>();
            skullRect.localPosition = new Vector2(-skullRect.localPosition.x, skullRect.localPosition.y);
        }
    }

    private void Timer_OnRoundEnded(object sender, OnRoundEndEventArgs e)
    {
        if( e.CurrentRound == 2)
        {
            this.gameObject.SetActive(true);

            var lightRect = _lightTutorial.GetComponent<RectTransform>();
            lightRect.localPosition = new Vector2(-lightRect.localPosition.x, lightRect.localPosition.y);

            var skullRect= _skullTutorial.GetComponent<RectTransform>();
            skullRect.localPosition = new Vector2(-skullRect.localPosition.x, skullRect.localPosition.y);
        }
    }

    private void Timer_OnRoundStart(object sender, System.EventArgs e)
    {
        this.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.Timer.OnRoundStart -= Timer_OnRoundStart; 
            GameManager.Instance.Timer.OnRoundEnded -= Timer_OnRoundEnded;

            PlayerSpawner.OnPlayersReadyToSpawn -= PlayerSpawner_OnPlayersReadyToSpawn;
        }
    }
}
