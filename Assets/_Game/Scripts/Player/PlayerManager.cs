using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

/// <summary>
/// Manages players in the game, including their types, input devices, and swapping gamepads.
/// </summary>
[Serializable]
public class PlayerManager
{
    #region Nested Types

    [Serializable]
    public class PlayerData
    {
        public event Action<int> OnScoreChanged;

        public GameObject PlayerObject { get; private set; }
        public PlayerRole Role { get; private set; }
        public PlayerIndex Index { get; private set; }
        public PlayerInput Input { get; private set; }
        public int Score { get; private set; }
        public List<int> RoundScores { get; private set; }

        public PlayerData(GameObject obj, PlayerRole role, PlayerIndex index, PlayerInput input)
        {
            PlayerObject = obj;
            Role = role;
            Index = index;
            Input = input;
            Score = 0;
            RoundScores = new() { 0 };
        }

        public void AddScore(int points, int? toRound = null)
        {
            Score += points;
            if (toRound != null && toRound >= 0 && toRound < RoundScores.Count)
            {
                // Add points to specific round (if ever needed)
                RoundScores[toRound.Value] += points;
                return;
            }
            else
            {
                // Add points to current round
                RoundScores[RoundScores.Count - 1] += points;
            }

            OnScoreChanged?.Invoke(points);
        }

        public void StartNewRound()
        {
            RoundScores.Add(0);
        }

        public void SwapRole(PlayerRole newRole)
        {
            Role = newRole;
        }

        public void SetPlayerObject(GameObject obj)
        {
            PlayerObject = obj;
            // Update Input reference to match the new GameObject
            Input = obj.GetComponent<PlayerInput>();
        }
    }

    public enum PlayerRole
    {
        Light,
        Skull,
        None,
    }

    public enum PlayerIndex
    {
        P1 = 1,
        P2 = 2,
    }
    #endregion

    #region Fields

    private List<PlayerData> _players = new List<PlayerData>();
    public IReadOnlyList<PlayerData> Players => _players.AsReadOnly();
    #endregion

    #region Events
    public event EventHandler OnPlayerConnected;
    public event EventHandler OnPlayerSwapped;
    public event Action<PlayerData, int> OnPlayerScoreChanged;
    #endregion

    #region Player Management

    /// <summary>
    /// Adds a new player to the manager.
    /// </summary>
    public void AddPlayer(
        GameObject playerObj,
        PlayerRole type,
        PlayerIndex index,
        PlayerInput input
    )
    {
        var player = new PlayerData(playerObj, type, index, input);
        _players.Add(player);

        player.OnScoreChanged += (points) => OnPlayerScoreChanged?.Invoke(player, points);

        OnPlayerConnected?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Removes a player from the manager by their GameObject.
    /// </summary>
    public void RemovePlayer(GameObject playerObj)
    {
        _players.RemoveAll(p => p.PlayerObject == playerObj);
    }

    /// <summary>
    /// Returns a list of all players, optionally filtered by type.
    /// </summary>
    public List<PlayerData> GetPlayers(PlayerRole? type = null)
    {
        if (type == null)
            return new List<PlayerData>(_players);

        return _players.FindAll(p => p.Role == type.Value);
    }

    /// <summary>
    /// Returns the GameObject of the first player with the specified type.
    /// Returns null if no such player exists.
    /// </summary>
    public GameObject GetPlayerOfType(PlayerRole type)
    {
        if (_players.Count == 0)
            return null;
        return _players.First(p => p.Role == type).PlayerObject ?? null;
    }

    #endregion

    #region Input & Gamepad Management

    /// <summary>
    /// Swaps the GameObjects and Roles between two players.
    /// Each player keeps their PlayerIndex, Score, and Gamepad, but controls the other player's character.
    /// </summary>
    public void SwapPlayerRoles(PlayerRole typeA, PlayerRole typeB)
    {
        var playerA = _players.Find(p => p.Role == typeA);
        var playerB = _players.Find(p => p.Role == typeB);

        if (playerA == null || playerB == null)
            return;

        // Get the gamepads before swapping
        Gamepad padA = playerA.Input?.devices.FirstOrDefault(d => d is Gamepad) as Gamepad;
        Gamepad padB = playerB.Input?.devices.FirstOrDefault(d => d is Gamepad) as Gamepad;

        if (padA == null || padB == null)
            return;

        // Unpair gamepads from current PlayerInputs
        playerA.Input.user.UnpairDevice(padA);
        playerB.Input.user.UnpairDevice(padB);

        // Swap GameObjects - each player now controls the other's character
        GameObject tempGameObject = playerA.PlayerObject;
        playerA.SetPlayerObject(playerB.PlayerObject); // This updates Input reference
        playerB.SetPlayerObject(tempGameObject); // This updates Input reference

        // Now pair gamepads to the new PlayerInputs
        // P1 keeps padA, but now it's paired to the GameObject they swapped to
        InputUser.PerformPairingWithDevice(padA, playerA.Input.user);
        InputUser.PerformPairingWithDevice(padB, playerB.Input.user);

        // Swap roles
        PlayerRole tempRole = playerA.Role;
        playerA.SwapRole(playerB.Role);
        playerB.SwapRole(tempRole);

        // Note: PlayerIndex, Score, and Gamepad stay with the original player

        OnPlayerSwapped?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
