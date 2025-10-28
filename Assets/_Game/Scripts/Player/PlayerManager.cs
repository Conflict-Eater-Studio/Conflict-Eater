using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager
{
    [Serializable]
    public struct PlayerData
    {
        public GameObject PlayerObject;
        public PlayerType Type;

        public PlayerData(GameObject obj, PlayerType type)
        {
            PlayerObject = obj;
            Type = type;
        }
    }

    private List<PlayerData> _players = new List<PlayerData>();

    public void AddPlayer(GameObject playerObj, PlayerType type)
    {
        _players.Add(new PlayerData(playerObj, type));
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

    public enum PlayerType
    {
        Light,
        Shadow
    }
}
