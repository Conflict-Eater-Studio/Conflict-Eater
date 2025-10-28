using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class PlayerManager
{
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

    private List<PlayerData> _players = new List<PlayerData>();

    public void AddPlayer(GameObject playerObj, PlayerType type, PlayerInput input)
    {
        _players.Add(new PlayerData(playerObj, type, input));
    }

    public void RemovePlayer(GameObject playerObj)
    {
        _players.RemoveAll(p => p.PlayerObject == playerObj);
    }

    public List<PlayerData> GetPlayers(PlayerType? type = null)
    {
        if (type == null) return new List<PlayerData>(_players);
        return _players.FindAll(p => p.Type == type.Value);
    }

    public GameObject GetPlayerOfType(PlayerType type)
    {
        foreach (var player in _players)
        {
            if (player.Type == type)
                return player.PlayerObject;
        }
        return null;
    }

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
    }

    public enum PlayerType
    {
        Light,
        Shadow
    }
}
