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
        public PlayerColor.ColorType PlayerColor;
    }

    public class RoleSelectionReleasedEventArgs : EventArgs
    {
        public int PlayerIndex; // 0 or 1
        public PlayerRole ReleasedRole;
    }

    public class ColorChangedEventArgs : EventArgs
    {
        public int PlayerIndex; // 0 or 1
        public PlayerColor.ColorType NewColor;
    }

    public static event EventHandler<RoleSelectionEventArgs> OnRoleSelectionChanged;
    public static event EventHandler<RoleSelectionReleasedEventArgs> OnRoleSelectionReleased;
    public static event EventHandler<ColorChangedEventArgs> OnColorChanged;
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
    private float _holdDuration = 1f;

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

    private struct JoinRequest
    {
        public Gamepad Gamepad;
        public PlayerRole SelectedRole;
        public float HoldTime;
        public bool IsConfirmed;
        public PlayerColor.ColorType Color;
    }

    // Track 2 players by their gamepad
    private Dictionary<Gamepad, int> _gamepadToPlayerIndex = new Dictionary<Gamepad, int>();
    private PlayerColor.ColorType[] _playerColors = new PlayerColor.ColorType[2];

    // Track role requests for each join request
    private Dictionary<PlayerRole, JoinRequest> _roleRequests =
        new Dictionary<PlayerRole, JoinRequest>();

    private bool _playersSpawned = false;
    private int _nextPlayerIndex = 0;
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

        foreach (var gamepad in gamepads)
        {
            // Assign player index to new gamepads (max 2 players)
            if (!_gamepadToPlayerIndex.ContainsKey(gamepad))
            {
                if (_nextPlayerIndex >= 2)
                    continue;

                int playerIndex = _nextPlayerIndex++;
                _gamepadToPlayerIndex[gamepad] = playerIndex;

                // Default colors: P1 = Red, P2 = Blue
                PlayerColor.ColorType defaultColor =
                    playerIndex == 0 ? PlayerColor.ColorType.Red : PlayerColor.ColorType.Blue;
                _playerColors[playerIndex] = defaultColor;

                // Update PlayerColorManager to keep it in sync
                PlayerColorManager.SetPlayerColor(playerIndex, defaultColor);

                // Raise color changed event
                OnColorChanged?.Invoke(
                    this,
                    new ColorChangedEventArgs { PlayerIndex = playerIndex, NewColor = defaultColor }
                );
            }

            int pIndex = _gamepadToPlayerIndex[gamepad];

            // Don't allow color changes if this player has confirmed a role
            bool hasConfirmedRole = _roleRequests.Values.Any(r =>
                r.Gamepad == gamepad && r.IsConfirmed
            );

            if (hasConfirmedRole)
                continue;

            // Cycle color with bumpers
            if (gamepad.leftShoulder.wasPressedThisFrame)
            {
                CycleColor(pIndex, -1);
            }
            else if (gamepad.rightShoulder.wasPressedThisFrame)
            {
                CycleColor(pIndex, 1);
            }
        }
    }

    private void CycleColor(int playerIndex, int direction)
    {
        PlayerColor.ColorType currentColor = _playerColors[playerIndex];
        int currentIndex = Array.IndexOf(PlayerColor.AllColors, currentColor);
        int startIndex = currentIndex;

        // Create a set of taken colors
        HashSet<PlayerColor.ColorType> takenColors = new HashSet<PlayerColor.ColorType>();
        for (int i = 0; i < _playerColors.Length; i++)
        {
            if (i != playerIndex)
                takenColors.Add(_playerColors[i]);
        }

        do
        {
            currentIndex =
                (currentIndex + direction + PlayerColor.AllColors.Length)
                % PlayerColor.AllColors.Length;

            if (!takenColors.Contains(PlayerColor.AllColors[currentIndex]))
                break;
        } while (currentIndex != startIndex);

        _playerColors[playerIndex] = PlayerColor.AllColors[currentIndex];

        PlayerColorManager.SetPlayerColor(playerIndex, PlayerColor.AllColors[currentIndex]);

        OnColorChanged?.Invoke(
            this,
            new ColorChangedEventArgs
            {
                PlayerIndex = playerIndex,
                NewColor = PlayerColor.AllColors[currentIndex],
            }
        );
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

            // Get player index and color
            if (!_gamepadToPlayerIndex.ContainsKey(gamepad))
                continue;

            int playerIndex = _gamepadToPlayerIndex[gamepad];
            PlayerColor.ColorType gamepadColor = _playerColors[playerIndex];

            // Check if Light role is being claimed
            if (leftTrigger > 0.5f && !_roleRequests.ContainsKey(PlayerRole.Light))
            {
                _roleRequests[PlayerRole.Light] = new JoinRequest
                {
                    Gamepad = gamepad,
                    SelectedRole = PlayerRole.Light,
                    HoldTime = 0f,
                    IsConfirmed = false,
                    Color = gamepadColor,
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
                    Color = gamepadColor,
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
                PlayerColor = request.Color,
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

        OnPlayersReadyToSpawn?.Invoke(this, EventArgs.Empty);

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
