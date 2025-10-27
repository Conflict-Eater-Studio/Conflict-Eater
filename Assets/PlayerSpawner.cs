using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private PlayerInputManager playerInputManager;
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
            Debug.Log("Gracz 1: PlayerController");
            renderer.material.color = Color.yellow;
        }
        else
        {
            input.gameObject.AddComponent<GhostController>();
            Debug.Log("Gracz 2: EnemyController");
            renderer.material.color = Color.darkBlue;
        }
    }
}
