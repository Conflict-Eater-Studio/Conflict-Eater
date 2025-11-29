using System;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawner : MonoBehaviour
{
    #region Events
    public class RoleSelectionEventArgs : EventArgs
    {
        public int PlayerIndex;
        public PlayerRole SelectedRole;
        public float HoldProgress;
        public bool IsConfirmed;
    }

    public class RoleSelectionReleasedEventArgs : EventArgs
    {
        public int PlayerIndex;
        public PlayerRole ReleasedRole;
    }

    public static event EventHandler<RoleSelectionEventArgs> OnRoleSelectionChanged;
    public static event EventHandler<RoleSelectionReleasedEventArgs> OnRoleSelectionReleased;
    public static event EventHandler OnPlayersReadyToSpawn;
    #endregion

    #region Fields
    [Header("References")]
    [SerializeField]
    private PlayerInputManager _playerInputManager;

    [SerializeField]
    private Grid _grid;

    [Header("Prefabs & Positions")]
    [SerializeField]
    private GameObject _ghostPrefab;

    [Header("Role Selection Settings")]
    [SerializeField]
    private float _holdDuration = 2f;

    [Tooltip("Time to stay on 'Ready' state before spawning players")]
    [SerializeField]
    private float _stayOnReadyTime = 1f;

    // Role selection state
    public enum PlayerRole
    {
        None,
        Light,
        Shadow,
    }

    private class JoinRequest
    {
        public Gamepad Gamepad;
        public PlayerRole SelectedRole;
        public float HoldTime;
        public bool IsConfirmed;
    }

    // Track join requests by role (Light/Shadow)
    private Dictionary<PlayerRole, JoinRequest> _roleRequests =
        new Dictionary<PlayerRole, JoinRequest>();
    private bool _playersSpawned = false;
    #endregion

    #region Unity Lifecycle
    private void OnEnable()
    {
        if (_playerInputManager != null)
        {
            // Disable automatic joining, handle manually
            _playerInputManager.DisableJoining();
        }
    }

    private void Update()
    {
        if (_playersSpawned)
            return;

        DetectGamepadInput();
        UpdateRoleSelection();
    }
    #endregion

    #region Player Join Logic
    private void DetectGamepadInput()
    {
        if (_playersSpawned)
            return;

        var gamepads = Gamepad.all;
        foreach (var gamepad in gamepads)
        {
            float leftTrigger = gamepad.leftTrigger.ReadValue();
            float rightTrigger = gamepad.rightTrigger.ReadValue();

            // Check if this gamepad is already claiming a role
            bool gamepadAlreadyClaimed = false;
            foreach (var existingRequest in _roleRequests.Values)
            {
                if (existingRequest.Gamepad == gamepad)
                {
                    gamepadAlreadyClaimed = true;
                    break;
                }
            }

            // Gamepad already has a role, skip it
            if (gamepadAlreadyClaimed)
                continue;

            // Check if Light role is being claimed
            if (leftTrigger > 0.5f && !_roleRequests.ContainsKey(PlayerRole.Light))
            {
                _roleRequests[PlayerRole.Light] = new JoinRequest
                {
                    Gamepad = gamepad,
                    SelectedRole = PlayerRole.Light,
                    HoldTime = 0f,
                    IsConfirmed = false,
                };
            }

            // Check if Shadow role is being claimed
            if (rightTrigger > 0.5f && !_roleRequests.ContainsKey(PlayerRole.Shadow))
            {
                _roleRequests[PlayerRole.Shadow] = new JoinRequest
                {
                    Gamepad = gamepad,
                    SelectedRole = PlayerRole.Shadow,
                    HoldTime = 0f,
                    IsConfirmed = false,
                };
            }
        }
    }

    private void UpdateRoleSelection()
    {
        // Update hold times for each role request
        var rolesToRemove = new List<PlayerRole>();

        foreach (var kvp in _roleRequests)
        {
            PlayerRole role = kvp.Key;
            var request = kvp.Value;

            if (request.IsConfirmed)
                continue;

            var gamepad = request.Gamepad;
            if (gamepad == null)
            {
                rolesToRemove.Add(role);
                continue;
            }

            float leftTrigger = gamepad.leftTrigger.ReadValue();
            float rightTrigger = gamepad.rightTrigger.ReadValue();

            // Check if the player is still holding their claimed role's trigger
            bool isStillHoldingRole = false;

            if (role == PlayerRole.Light && leftTrigger > 0.5f)
            {
                isStillHoldingRole = true;
            }
            else if (role == PlayerRole.Shadow && rightTrigger > 0.5f)
            {
                isStillHoldingRole = true;
            }

            if (isStillHoldingRole)
            {
                request.HoldTime += Time.deltaTime;

                if (request.HoldTime >= _holdDuration && !request.IsConfirmed)
                {
                    request.IsConfirmed = true;
                    RaiseRoleSelectionEvent(role, request);
                }
                else if (!request.IsConfirmed)
                {
                    RaiseRoleSelectionEvent(role, request);
                }
            }
            else
            {
                // Trigger was released - fire event and mark for removal
                RaiseRoleSelectionReleasedEvent(role, role);
                rolesToRemove.Add(role);
            }
        }

        // Remove released roles
        foreach (var role in rolesToRemove)
        {
            _roleRequests.Remove(role);
        }

        // Check if both roles are claimed and confirmed
        if (_roleRequests.Count == 2)
        {
            bool lightConfirmed =
                _roleRequests.ContainsKey(PlayerRole.Light)
                && _roleRequests[PlayerRole.Light].IsConfirmed;
            bool shadowConfirmed =
                _roleRequests.ContainsKey(PlayerRole.Shadow)
                && _roleRequests[PlayerRole.Shadow].IsConfirmed;

            if (lightConfirmed && shadowConfirmed)
            {
                SpawnPlayers();
            }
        }
    }

    private void RaiseRoleSelectionEvent(PlayerRole role, JoinRequest request)
    {
        // Map role to player index for UI (Light=0, Shadow=1)
        int playerIndex = role == PlayerRole.Light ? 0 : 1;

        OnRoleSelectionChanged?.Invoke(
            this,
            new RoleSelectionEventArgs
            {
                PlayerIndex = playerIndex,
                SelectedRole = request.SelectedRole,
                HoldProgress = request.HoldTime / _holdDuration,
                IsConfirmed = request.IsConfirmed,
            }
        );
    }

    private void RaiseRoleSelectionReleasedEvent(PlayerRole role, PlayerRole releasedRole)
    {
        // Map role to player index for UI (Light=0, Shadow=1)
        int playerIndex = role == PlayerRole.Light ? 0 : 1;

        OnRoleSelectionReleased?.Invoke(
            this,
            new RoleSelectionReleasedEventArgs
            {
                PlayerIndex = playerIndex,
                ReleasedRole = releasedRole,
            }
        );
    }

    private void SpawnPlayers()
    {
        _playersSpawned = true;
        StartCoroutine(SpawnPlayersCoroutine());
    }

    private System.Collections.IEnumerator SpawnPlayersCoroutine()
    {
        // Wait before spawning players (stay on "Ready" state for a moment)
        yield return new WaitForSeconds(_stayOnReadyTime);

        OnPlayersReadyToSpawn?.Invoke(this, EventArgs.Empty);

        // Enabled joining and spawn players
        _playerInputManager.EnableJoining();

        int playerIndex = 0;
        foreach (var kvp in _roleRequests)
        {
            PlayerRole role = kvp.Key;
            var request = kvp.Value;

            // Join the player using PlayerInputManager
            var input = _playerInputManager.JoinPlayer(playerIndex, -1, null, request.Gamepad);

            var renderer = input.GetComponent<Renderer>();
            string childName = role == PlayerRole.Light ? "LightAnchor" : "ShadowAnchor";

            GameObject child = new GameObject(childName);
            child.transform.SetParent(input.transform);
            child.transform.localPosition = Vector3.zero;

            if (role == PlayerRole.Light)
            {
                SetupLightPlayer(input, child, renderer);
            }
            else if (role == PlayerRole.Shadow)
            {
                SetupShadowPlayer(input, child);
            }

            playerIndex++;
        }
    }

    private void SetupLightPlayer(PlayerInput input, GameObject child, Renderer renderer)
    {
        Debug.Log("Player 1 joined as LightPlayerController");

        input.name = "LightControllerRoot";
        input.transform.position = _grid.GetSpawnPoint(Grid.SpawnPointType.Light);

        renderer.material.color = Color.yellow;
        input.gameObject.tag = "PlayerLight";

        child.AddComponent<LightPlayerController>();
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

        child.AddComponent<ShadowAppearanceManager>();
        var shadowController = child.AddComponent<ShadowPlayerController>();
        shadowController.SetShadowPrefab(_ghostPrefab);

        GameManager.Instance.PlayerManager.AddPlayer(
            input.gameObject,
            PlayerManager.PlayerType.Shadow,
            input
        );

        GameManager.Instance.Timer.StartMatch();
    }
    #endregion
}
