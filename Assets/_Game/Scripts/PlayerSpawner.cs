using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class PlayerSpawner : MonoBehaviour
{
    #region Events
    public class RoleSelectionEventArgs : EventArgs
    {
        public int PlayerIndex; // 0 or 1
        public PlayerRole SelectedRole;
        public float HoldProgress; // 0 to 1 inclusive
        public bool IsConfirmed;
    }

    public class RoleSelectionReleasedEventArgs : EventArgs
    {
        public int PlayerIndex; // 0 or 1
        public PlayerRole ReleasedRole;
    }

    public class PlayerReadyEventArgs : EventArgs
    {
        public int PlayerIndex; // 0 or 1
        public PlayerRole AssignedRole;
    }

    public static event EventHandler<RoleSelectionEventArgs> OnRoleSelectionChanged;
    public static event EventHandler<RoleSelectionReleasedEventArgs> OnRoleSelectionReleased;
    public static event EventHandler<PlayerReadyEventArgs> OnPlayersReadyToSpawn;
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
    private float _holdDuration = 1f;

    [Tooltip("Time to stay on 'Ready' state before spawning players")]
    [SerializeField]
    private float _stayOnReadyTime = 1f;

    public static readonly Dictionary<PlayerRole, Color> RoleColors = new Dictionary<
        PlayerRole,
        Color
    >
    {
        { PlayerRole.Light, Color.yellow },
        { PlayerRole.Shadow, Color.red },
    };

    // Role selection state
    public enum PlayerRole
    {
        None,
        Light,
        Shadow,
    }

    private struct JoinRequest
    {
        public Gamepad Gamepad;
        public PlayerRole SelectedRole;
        public float HoldTime;
        public bool IsConfirmed;
    }

    // Track 2 players by their gamepad
    private Dictionary<Gamepad, int> _gamepadToPlayerIndex = new Dictionary<Gamepad, int>();

    // Track role requests for each join request
    private Dictionary<PlayerRole, JoinRequest> _roleRequests =
        new Dictionary<PlayerRole, JoinRequest>();

    private bool _playersSpawned = false;
    private int _nextPlayerIndex = 0;
    #endregion

    #region Public Methods
    /// <summary>
    /// Gets the role assigned to Player 1 (index 0).
    /// Returns PlayerRole.None if Player 1 hasn't selected a role yet.
    /// </summary>
    public PlayerRole GetPlayer1Role()
    {
        // Find the role request for player index 0
        foreach (var kvp in _roleRequests)
        {
            if (_gamepadToPlayerIndex.TryGetValue(kvp.Value.Gamepad, out int playerIndex))
            {
                if (playerIndex == 0)
                {
                    return kvp.Value.SelectedRole;
                }
            }
        }
        return PlayerRole.None;
    }
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

        var gamepads = Gamepad.all;

        HandleColorSelection(gamepads);
        DetectGamepadInput(gamepads);
        UpdateRoleSelection();
    }
    #endregion

    #region Player Join Logic

    private void HandleColorSelection(ReadOnlyArray<Gamepad> gamepads)
    {
        if (_playersSpawned)
            return;

        // This method is kept for compatibility but player index assignment
        // now happens in DetectGamepadInput when a trigger is first pressed
    }

    private void DetectGamepadInput(ReadOnlyArray<Gamepad> gamepads)
    {
        foreach (var gamepad in gamepads)
        {
            float leftTrigger = gamepad.leftTrigger.ReadValue();
            float rightTrigger = gamepad.rightTrigger.ReadValue();

            // Check if this gamepad is already claiming a role
            bool gamepadAlreadyClaimed = _roleRequests.Values.Any(r => r.Gamepad == gamepad);

            // Gamepad already has a role, skip it
            if (gamepadAlreadyClaimed)
                continue;

            // Assign player index when first trigger is pressed
            if (!_gamepadToPlayerIndex.ContainsKey(gamepad))
            {
                if (_nextPlayerIndex >= 2)
                    continue;

                // First gamepad to press a trigger gets index 0
                int playerIndex = _nextPlayerIndex++;
                _gamepadToPlayerIndex[gamepad] = playerIndex;
            }

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
        var updates = new List<(PlayerRole, JoinRequest)>();

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
            bool isStillHoldingRole = role switch
            {
                PlayerRole.Light => leftTrigger > 0.5f,
                PlayerRole.Shadow => rightTrigger > 0.5f,
                _ => false,
            };

            if (isStillHoldingRole)
            {
                request.HoldTime += Time.deltaTime;

                if (request.HoldTime >= _holdDuration && !request.IsConfirmed)
                {
                    request.IsConfirmed = true;
                    RaiseRoleSelectionChangedEvent(role, request);
                }
                else if (!request.IsConfirmed)
                {
                    RaiseRoleSelectionChangedEvent(role, request);
                }

                // Collect update to apply after enumeration
                updates.Add((role, request));
            }
            else
            {
                // Trigger was released - fire event and mark for removal
                RaiseRoleSelectionReleasedEvent(role, role);
                rolesToRemove.Add(role);
            }
        }

        // Apply updates
        foreach (var (role, request) in updates)
        {
            _roleRequests[role] = request;
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

    private void RaiseRoleSelectionChangedEvent(PlayerRole role, JoinRequest request)
    {
        if (!_gamepadToPlayerIndex.TryGetValue(request.Gamepad, out int playerIndex))
            playerIndex = -1;

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
        if (!_roleRequests.TryGetValue(role, out JoinRequest request))
            return;

        Gamepad gamepad = request.Gamepad;
        if (gamepad == null || !_gamepadToPlayerIndex.TryGetValue(gamepad, out int playerIndex))
            playerIndex = -1;

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

        OnPlayersReadyToSpawn?.Invoke(this, new PlayerReadyEventArgs() { });

        // Enabled joining and spawn players
        _playerInputManager.EnableJoining();

        // Note: Player colors are already saved to PlayerColorManager during color selection
        // via HandleColorSelection methods, so we don't need to set them again here

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
    }
    #endregion
}
