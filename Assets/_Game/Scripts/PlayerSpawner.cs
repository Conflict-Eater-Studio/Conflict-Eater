using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawner : MonoBehaviour
{
    #region Fields
    [Header("References")]
    [SerializeField] private PlayerInputManager _playerInputManager;
    [SerializeField] private Grid _grid;

    [Header("Prefabs & Positions")]
    [SerializeField] private GameObject _ghostPrefab;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _p1InfoText;
    [SerializeField] private TextMeshProUGUI _p2InfoText;
    #endregion

    #region Unity Lifecycle
    private void OnEnable()
    {
        _playerInputManager.playerJoinedEvent.AddListener(OnPlayerJoined);
    }

    private void OnDisable()
    {
        _playerInputManager.playerJoinedEvent.RemoveListener(OnPlayerJoined);
    }
    #endregion

    #region Player Join Logic
    private void OnPlayerJoined(PlayerInput input)
    {
        if (input.playerIndex > 1) return;

        var renderer = input.GetComponent<Renderer>();
        string childName = input.playerIndex == 0 ? "LightAnchor" : "ShadowAnchor";

        GameObject child = new GameObject(childName);
        child.transform.SetParent(input.transform);
        child.transform.localPosition = Vector3.zero;

        if (input.playerIndex == 0)
        {
            SetupLightPlayer(input, child, renderer);
        }
        else if (input.playerIndex == 1)
        {
            SetupShadowPlayer(input, child);
        }
    }

    private void SetupLightPlayer(PlayerInput input, GameObject child, Renderer renderer)
    {
        Debug.Log("Player 1 joined as LightPlayerController");

        input.name = "LightControllerRoot";
        input.transform.position = _grid.GetSpawnPoint(Grid.SpawnPointType.Light);

        renderer.material.color = Color.yellow;
        input.gameObject.tag = "PlayerLight";

        var lightController = child.AddComponent<LightPlayerController>();
        var collider = child.AddComponent<CircleCollider2D>();
        collider.radius = 0.45f;

        GameManager.Instance.PlayerManager.AddPlayer(
            input.gameObject,
            PlayerManager.PlayerType.Light,
            input
        );
    }

    private void SetupShadowPlayer(PlayerInput input, GameObject child)
    {
        Debug.Log("Player 2 joined as ShadowPlayerController");

        input.name = "ShadowControllerRoot";
        input.transform.position = _grid.GetSpawnPoint(Grid.SpawnPointType.Shadow);

        Destroy(input.GetComponent<Renderer>());
        Destroy(input.GetComponent<CircleCollider2D>());

        input.gameObject.tag = "PlayerShadow";

        var apperenceManager = child.AddComponent<ShadowAppearanceManager>();

        var shadowController = child.AddComponent<ShadowPlayerController>();
        shadowController.SetShadowPrefab(_ghostPrefab);

        GameManager.Instance.PlayerManager.AddPlayer(
            input.gameObject,
            PlayerManager.PlayerType.Shadow,
            input
        );

        GameManager.Instance.Timer.Run();
    }
    #endregion
}
