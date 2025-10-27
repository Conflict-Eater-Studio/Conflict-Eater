using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class RoleSwapper : MonoBehaviour
{
    [SerializeField] private PlayerInputManager _playerInputManager;

    private List<InputUser> _playerInputs;
    private List<Gamepad> _gamepads;
    private void Awake() {
        _playerInputs = new List<InputUser>();
        _gamepads = new List<Gamepad>();
        _playerInputManager.playerJoinedEvent.AddListener(GetDevices);
    }
    private void GetDevices(PlayerInput playerInput) {  
        _playerInputs.Add(playerInput.user);
        _gamepads.Add(playerInput.GetDevice<Gamepad>());
    }

    private void Start() {
        GameManager.Instance.OnRoundEnd += RoleSwapper_OnRoundEnd;    
    }
    private void OnDisable() {
        GameManager.Instance.OnRoundEnd -= RoleSwapper_OnRoundEnd;
    }
    private void RoleSwapper_OnRoundEnd(object sender, EventArgs e) {
        if(_playerInputs == null || _playerInputs.Count == 0) return;
        if(_gamepads == null || _gamepads.Count == 0) return;
        _playerInputs[0].UnpairDevice(_gamepads[0]);
        _playerInputs[0].
    }
}
