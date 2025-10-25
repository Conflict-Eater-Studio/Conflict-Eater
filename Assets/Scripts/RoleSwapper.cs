using System;
using UnityEngine;

enum Role {
    Light,
    Shadows,
}
public class RoleSwapper : MonoBehaviour
{
    [SerializeField] private Role firstPlayerRole;
    [SerializeField] private Role secondPlayerRole;
    
    private void Start() {
        GameManager.Instance.OnRoundEnd += RoleSwapper_OnRoundEnd;    
        Debug.Log($"1Player: {firstPlayerRole.ToString()}");
        Debug.Log($"2Player: {secondPlayerRole.ToString()}");
    }
    private void OnDisable() {
        GameManager.Instance.OnRoundEnd -= RoleSwapper_OnRoundEnd;
    }
    private void RoleSwapper_OnRoundEnd(object sender, EventArgs e) {
        (firstPlayerRole, secondPlayerRole) = (secondPlayerRole, firstPlayerRole);
        Debug.Log($"1Player: {firstPlayerRole.ToString()}");
        Debug.Log($"2Player: {secondPlayerRole.ToString()}");
    }
}
