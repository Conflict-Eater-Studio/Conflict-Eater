using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private PlayerInputManager playerInputManager;
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private Vector3 playerPosition;
    [SerializeField] private Vector3 ghostPosition;
    [SerializeField] private TextMeshProUGUI infoText;
    
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

        if (input.playerIndex == 0)
        {
            Debug.Log("Player 1: PlayerController");
            input.transform.position = _grid.GetSpawnPoint(Grid.SpawnPointType.Player);
            renderer.material.color = Color.yellow;
            input.gameObject.tag = "PlayerLight";
            input.GetComponent<CoinCollector>().SetInfoText(infoText);
        }
        if (input.playerIndex == 1)
        {
            input.name = "Ghost_1";
            input.transform.position = _grid.GetSpawnPoint(Grid.SpawnPointType.Ghost);
            input.gameObject.tag = "PlayerShadow";
            Destroy(input.GetComponent<CoinCollector>());

            var ghostController = input.gameObject.AddComponent<GhostController>();
            ghostController.SetGhostPrefab(ghostPrefab);

            Debug.Log("Player 2: EnemyController");
        }
    }
}
