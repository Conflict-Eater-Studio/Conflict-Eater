using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private PlayerInputManager playerInputManager;
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private Vector3 playerPosition;
    [SerializeField] private Vector3 ghostPosition;
    [SerializeField] private TextMeshProUGUI p1InfoText;
    [SerializeField] private TextMeshProUGUI p2InfoText;

    // Grid ref
    [SerializeField] private Grid _grid;

    private void OnEnable()
    {
        playerInputManager.playerJoinedEvent.AddListener(OnPlayerJoined);
    }

    private void OnDisable()
    {
        playerInputManager.playerJoinedEvent.RemoveListener(OnPlayerJoined);
    }

    private void OnPlayerJoined(PlayerInput input)
    {
        var renderer = input.GetComponent<Renderer>();

        string childName = input.playerIndex == 0 ? "LightAnchor" : "ShadowAnchor";
        GameObject child = new GameObject(childName);
        child.transform.SetParent(input.transform);
        child.transform.localPosition = Vector3.zero;
        child.AddComponent<CircleCollider2D>();
        child.GetComponent<CircleCollider2D>().radius = 0.45f;

        if (input.playerIndex == 0)
        {
            Debug.Log("Player 1: LightPlayerController");
            input.name = "LightControllerRoot";

            input.transform.position = _grid.GetSpawnPoint(PlayerManager.PlayerType.Light);
            renderer.material.color = Color.yellow;
            input.gameObject.tag = "PlayerLight";
            child.AddComponent<LightPlayerController>();
            child.AddComponent<CoinCollector>();
            child.GetComponent<CoinCollector>().SetInfoText(p1InfoText, p2InfoText);

            GameManager.Instance.PlayerManager.AddPlayer(input.gameObject, PlayerManager.PlayerType.Light, input);
        }
        if (input.playerIndex == 1)
        {
            Debug.Log("Player 2: ShadowPlayerController");
            input.name = "ShadowControllerRoot";

            input.transform.position = _grid.GetSpawnPoint(PlayerManager.PlayerType.Shadow);
            renderer.material.color = Color.darkBlue;
            input.gameObject.tag = "PlayerShadow";

            var ghostController = child.gameObject.AddComponent<ShadowPlayerController>();
            child.GetComponent<CircleCollider2D>().isTrigger = true;
            //ghostController.SetGhostPrefab(ghostPrefab);

            GameManager.Instance.PlayerManager.AddPlayer(input.gameObject, PlayerManager.PlayerType.Shadow, input);

            GameManager.Instance.Timer.Run();
        }
    }
}
