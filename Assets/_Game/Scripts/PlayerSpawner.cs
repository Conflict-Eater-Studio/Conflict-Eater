using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Rendering.Universal;

public class PlayerSpawner : MonoBehaviour
{
    #region Events
    public class RoleSelectionEventArgs : EventArgs
    {
        public PlayerManager.PlayerIndex PlayerIndex;
        public PlayerManager.PlayerRole PlayerRole;
        public float HoldProgress; // 0 to 1 inclusive
        public bool IsConfirmed;
    }

    public static event EventHandler<RoleSelectionEventArgs> OnRoleSelectionStarted;
    public static event EventHandler<RoleSelectionEventArgs> OnRoleSelectionChanged;
    public static event EventHandler<RoleSelectionEventArgs> OnRoleSelectionReleased;
    public static event EventHandler OnPlayersReadyToSpawn;
    #endregion

    #region Fields
    [Header("References")]
    [SerializeField]
    private PlayerInputManager _playerInputManager;

    private Grid _grid;

    [Header("Prefabs & Positions")]
    [SerializeField]
    private GameObject _ghostPrefab;

    [SerializeField]
    private GameObject _showLightPlayerPrefab;

    [SerializeField]
    private RuntimeAnimatorController animationController;

    [SerializeField]
    private Material _laserMaterial;

    [Header("Role Selection Settings")]
    [SerializeField]
    private float _holdDuration = 1f;

    public static readonly Dictionary<PlayerManager.PlayerRole, Color> RoleColors = new Dictionary<
        PlayerManager.PlayerRole,
        Color
    >
    {
        { PlayerManager.PlayerRole.Light, Color.yellow },
        { PlayerManager.PlayerRole.Skull, Color.red },
    };

    private class JoinRequest
    {
        public Gamepad Gamepad;
        public PlayerManager.PlayerRole SelectedRole;
        public float HoldTime;
        public bool IsConfirmed;
        public PlayerManager.PlayerIndex PlayerIndex;
    }

    private List<JoinRequest> _activeRequests = new List<JoinRequest>();

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

        var gamepads = Gamepad.all;

        DetectGamepadInput(gamepads);
        UpdateRoleSelection();
    }
    #endregion

    #region Player Join Logic

    /// <summary>
    /// Initial detection of gamepad input for joining players.
    /// </summary>
    /// <param name="gamepads"></param>
    private void DetectGamepadInput(ReadOnlyArray<Gamepad> gamepads)
    {
        foreach (var gamepad in gamepads)
        {
            float leftTrigger = gamepad.leftTrigger.ReadValue();
            float rightTrigger = gamepad.rightTrigger.ReadValue();

            // Skip if no input detected
            if (leftTrigger <= 0.5f && rightTrigger <= 0.5f)
                continue;

            // Gamepad already has a role, skip it
            if (_activeRequests.Any(r => r.Gamepad == gamepad) || _activeRequests.Count >= 2)
                continue;

            // Determine available player index
            PlayerManager.PlayerIndex? availableIndex = null;
            if (!_activeRequests.Any(r => r.PlayerIndex == PlayerManager.PlayerIndex.P1))
                availableIndex = PlayerManager.PlayerIndex.P1;
            else if (!_activeRequests.Any(r => r.PlayerIndex == PlayerManager.PlayerIndex.P2))
                availableIndex = PlayerManager.PlayerIndex.P2;
            if (availableIndex == null)
                continue;

            // Init new join request with gamepad assigned for given player index and empty role
            JoinRequest newRequest = new JoinRequest
            {
                Gamepad = gamepad,
                HoldTime = 0f,
                IsConfirmed = false,
                PlayerIndex = availableIndex.Value,
                SelectedRole = PlayerManager.PlayerRole.None,
            };

            if (
                leftTrigger > 0.5f
                && !_activeRequests.Any(r => r.SelectedRole == PlayerManager.PlayerRole.Light)
            )
            {
                // L2 held, light role available -> claim Light role, add request to active list and fire event
                newRequest.SelectedRole = PlayerManager.PlayerRole.Light;
                Debug.Log($"Player {newRequest.PlayerIndex} claimed Light role.");
            }
            else if (
                rightTrigger > 0.5f
                && !_activeRequests.Any(r => r.SelectedRole == PlayerManager.PlayerRole.Skull)
            )
            {
                // R2 held, skull role available -> claim Skull role, add request to active list and fire event
                newRequest.SelectedRole = PlayerManager.PlayerRole.Skull;
                Debug.Log($"Player {newRequest.PlayerIndex} claimed Skull role.");
            }
            else
            {
                // Desired role not available, skip
                Debug.Log($"Player {availableIndex.Value} tried to join but role not available.");
                continue;
            }

            _activeRequests.Add(newRequest);
            OnRoleSelectionStarted?.Invoke(
                this,
                new RoleSelectionEventArgs
                {
                    PlayerIndex = newRequest.PlayerIndex,
                    PlayerRole = newRequest.SelectedRole,
                    HoldProgress = Mathf.Clamp(newRequest.HoldTime / _holdDuration, 0f, 1f),
                    IsConfirmed = newRequest.IsConfirmed,
                }
            );
        }
    }

    private void UpdateRoleSelection()
    {
        // Update hold times for each role request
        var removeForPlayer = new List<PlayerManager.PlayerIndex>();

        for (int i = 0; i < _activeRequests.Count; i++)
        {
            // Skip requests that are already confirmed
            if (_activeRequests[i].IsConfirmed)
                continue;

            // If gamepad is null, mark for removal from active requests
            if (_activeRequests[i].Gamepad == null)
            {
                removeForPlayer.Add(_activeRequests[i].PlayerIndex);
                continue;
            }

            float leftTrigger = _activeRequests[i].Gamepad.leftTrigger.ReadValue();
            float rightTrigger = _activeRequests[i].Gamepad.rightTrigger.ReadValue();

            // Check if the player is still holding their claimed role's trigger
            bool isStillHoldingRole = _activeRequests[i].SelectedRole switch
            {
                PlayerManager.PlayerRole.Light => leftTrigger > 0.5f,
                PlayerManager.PlayerRole.Skull => rightTrigger > 0.5f,
                _ => false,
            };

            if (isStillHoldingRole)
            {
                _activeRequests[i].HoldTime += Time.deltaTime;

                if (_activeRequests[i].HoldTime >= _holdDuration && !_activeRequests[i].IsConfirmed)
                {
                    _activeRequests[i].IsConfirmed = true;
                    Debug.Log(
                        $"Player {_activeRequests[i].PlayerIndex} confirmed {_activeRequests[i].SelectedRole} role."
                    );
                    RaiseRoleSelectionChangedEvent(_activeRequests[i]);
                }
                else if (!_activeRequests[i].IsConfirmed)
                {
                    RaiseRoleSelectionChangedEvent(_activeRequests[i]);
                }
            }
            else
            {
                // Trigger was released - fire event and mark for removal
                Debug.Log(
                    $"Player {_activeRequests[i].PlayerIndex} released {_activeRequests[i].SelectedRole} role."
                );
                RaiseRoleSelectionReleasedEvent(_activeRequests[i]);
                removeForPlayer.Add(_activeRequests[i].PlayerIndex);
            }
        }

        // Remove released roles
        foreach (var playerIndex in removeForPlayer)
        {
            _activeRequests.RemoveAll(r => r.PlayerIndex == playerIndex);
        }

        // Check if both roles are claimed and confirmed
        if (_activeRequests.Count == 2)
        {
            bool lightConfirmed = _activeRequests.Any(r =>
                r.SelectedRole == PlayerManager.PlayerRole.Light && r.IsConfirmed
            );
            bool shadowConfirmed = _activeRequests.Any(r =>
                r.SelectedRole == PlayerManager.PlayerRole.Skull && r.IsConfirmed
            );

            if (lightConfirmed && shadowConfirmed)
            {
                _playersSpawned = true;
                Debug.Log("Both players confirmed roles. Ready to spawn.");
                OnPlayersReadyToSpawn?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void RaiseRoleSelectionChangedEvent(JoinRequest request)
    {
        OnRoleSelectionChanged?.Invoke(
            this,
            new RoleSelectionEventArgs
            {
                PlayerIndex = request.PlayerIndex,
                PlayerRole = request.SelectedRole,
                HoldProgress = Mathf.Clamp(request.HoldTime / _holdDuration, 0f, 1f),
                IsConfirmed = request.IsConfirmed,
            }
        );
    }

    private void RaiseRoleSelectionReleasedEvent(JoinRequest request)
    {
        OnRoleSelectionReleased?.Invoke(
            this,
            new RoleSelectionEventArgs
            {
                PlayerIndex = request.PlayerIndex,
                PlayerRole = request.SelectedRole,
                HoldProgress = Mathf.Clamp(request.HoldTime / _holdDuration, 0f, 1f),
                IsConfirmed = request.IsConfirmed,
            }
        );
    }

    public void ExecutePlayerSpawn()
    {
        // Enable joining and spawn players
        _playerInputManager.EnableJoining();

        foreach (var request in _activeRequests)
        {
            Debug.Log($"Spawning player {request.PlayerIndex} as {request.SelectedRole}");

            // Join the player using PlayerInputManager
            var input = _playerInputManager.JoinPlayer(
                (int)request.PlayerIndex - 1,
                -1,
                null,
                request.Gamepad
            );

            string childName =
                request.SelectedRole == PlayerManager.PlayerRole.Light
                    ? "LightAnchor"
                    : "ShadowAnchor";

            GameObject child = new GameObject(childName);
            child.transform.SetParent(input.transform);
            child.transform.localPosition = Vector3.zero;

            if (request.SelectedRole == PlayerManager.PlayerRole.Light)
            {
                SetupLightPlayer(input, child, request.PlayerIndex);
            }
            else if (request.SelectedRole == PlayerManager.PlayerRole.Skull)
            {
                SetupSkullPlayer(input, child, request.PlayerIndex);
            }
        }
    }

    private void SetupLightPlayer(
        PlayerInput input,
        GameObject child,
        PlayerManager.PlayerIndex playerIndex
    )
    {
        input.name = "LightControllerRoot";

        GameObject playerNameObj = input.GetComponentInChildren<Canvas>().gameObject;

        if (_grid == null)
        {
            _grid = GameManager.Instance.Grid;
        }

        input.transform.position = _grid.GetSpawnPoint(Grid.SpawnPointType.Light);
        input.gameObject.AddComponent<Animator>();
        input.gameObject.GetComponent<Animator>().runtimeAnimatorController = animationController;

        input.gameObject.tag = "PlayerLight";

        child.AddComponent<LightPlayerController>();

        PlayerController playerController = child.GetComponent<LightPlayerController>();
        if (playerController != null)
        {
            playerController.SetPlayerNameObj(input.transform.GetChild(0).gameObject);
            playerController.SetPlayerNick(
                playerIndex == PlayerManager.PlayerIndex.P1 ? "Player 1" : "Player 2"
            );
            playerController.SetPlayerScoreObj(
            input.transform.GetChild(1).gameObject
            );
        }

        var collider = child.AddComponent<CircleCollider2D>();
        collider.radius = 0.45f;

        if (_showLightPlayerPrefab != null)
        {
            GameObject lightPrefabChild = Instantiate(_showLightPlayerPrefab, child.transform);
            lightPrefabChild.name = _showLightPlayerPrefab.name;

            child
                .GetComponent<LightPlayerController>()
                .SetActiveLight(lightPrefabChild.GetComponent<Light2D>());
        }

        GameManager.Instance.PlayerManager.AddPlayer(
            input.gameObject,
            PlayerManager.PlayerRole.Light,
            playerIndex,
            input
        );

        Debug.Log($"Light player spawned for {playerIndex}.");
    }

    private void SetupSkullPlayer(
        PlayerInput input,
        GameObject child,
        PlayerManager.PlayerIndex playerIndex
    )
    {
        input.name = "ShadowControllerRoot";

        if (_grid == null)
        {
            _grid = GameManager.Instance.Grid;
        }

        input.transform.position = _grid.GetSpawnPoint(Grid.SpawnPointType.Shadow);

        Debug.Log(_grid.GetSpawnPoint(Grid.SpawnPointType.Shadow));

        Destroy(input.GetComponent<Renderer>());
        Destroy(input.GetComponent<CircleCollider2D>());

        input.gameObject.tag = "PlayerShadow";

        child.AddComponent<ShadowAppearanceManager>();
        var shadowController = child.AddComponent<ShadowPlayerController>();

        PlayerController playerController = shadowController;

        if (playerController != null)
        {
            playerController.SetPlayerNameObj(input.transform.GetChild(0).gameObject);
            playerController.SetPlayerNick(
                playerIndex == PlayerManager.PlayerIndex.P1 ? "Player 1" : "Player 2"
            );
            playerController.SetPlayerScoreObj(
            input.transform.GetChild(1).gameObject
            );
        }

        shadowController.SetShadowPrefab(_ghostPrefab);
        shadowController.SetLaserMaterial(_laserMaterial);

        GameManager.Instance.PlayerManager.AddPlayer(
            input.gameObject,
            PlayerManager.PlayerRole.Skull,
            playerIndex,
            input
        );

        Debug.Log($"Shadow player spawned for {playerIndex}.");
    }
    #endregion
}
