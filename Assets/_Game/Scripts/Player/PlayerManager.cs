using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.Rendering;

/// <summary>
/// Manages players in the game, including their types, input devices, and swapping gamepads.
/// </summary>
public class PlayerManager
{
    #region Nested Types

    [Serializable]
    public struct PlayerData
    {
        public GameObject PlayerObject;
        public PlayerType Type;
        public PlayerInput Input;

        public PlayerData(GameObject obj, PlayerType type, PlayerInput input)
        {
            PlayerObject = obj;
            Type = type;
            Input = input;
        }
    }

    public enum PlayerType
    {
        Light,
        Shadow,
    }
    #endregion

    #region Fields

    private List<PlayerData> _players = new List<PlayerData>(); // List of all players
    #endregion

    #region Events
    public event EventHandler OnPlayerConnected;
    public event EventHandler OnPlayerSwapped;
    #endregion

    #region Player Management

    /// <summary>
    /// Adds a new player to the manager.
    /// </summary>
    public void AddPlayer(GameObject playerObj, PlayerType type, PlayerInput input)
    {
        _players.Add(new PlayerData(playerObj, type, input));
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
    public List<PlayerData> GetPlayers(PlayerType? type = null)
    {
        if (type == null)
            return new List<PlayerData>(_players);

        return _players.FindAll(p => p.Type == type.Value);
    }

    /// <summary>
    /// Returns the GameObject of the first player with the specified type.
    /// Returns null if no such player exists.
    /// </summary>
    public GameObject GetPlayerOfType(PlayerType type)
    {
        foreach (var player in _players)
        {
            if (player.Type == type)
                return player.PlayerObject;
        }
        return null;
    }

    #endregion

    #region Input & Gamepad Management

    /// <summary>
    /// Swaps the gamepads between two players.
    /// Useful for switching control between Light and Shadow players.
    /// </summary>
    public void SwapPlayerGamepads(PlayerType typeA, PlayerType typeB)
    {
        var playerA = _players.Find(p => p.Type == typeA);
        var playerB = _players.Find(p => p.Type == typeB);

        if (playerA.Input == null || playerB.Input == null)
            return;

        Gamepad padA = playerA.Input.devices.FirstOrDefault(d => d is Gamepad) as Gamepad;
        Gamepad padB = playerB.Input.devices.FirstOrDefault(d => d is Gamepad) as Gamepad;

        if (padA == null || padB == null)
            return;

        playerA.Input.user.UnpairDevice(padA);
        playerB.Input.user.UnpairDevice(padB);

        InputUser.PerformPairingWithDevice(padB, playerA.Input.user);
        InputUser.PerformPairingWithDevice(padA, playerB.Input.user);

        // HACK: Swapping the players in the list to keep the track of their role for UI
        _players.TrySwap(_players.IndexOf(playerA), _players.IndexOf(playerB), out _);

        OnPlayerSwapped?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
