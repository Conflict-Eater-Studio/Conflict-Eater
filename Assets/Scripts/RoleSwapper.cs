using System;
using UnityEngine;

public class RoleSwapper : MonoBehaviour
{
    private void Awake() {
        GameManager.Instance.Timer.OnRoundEnd += Swap;
    }
    private void Swap(object sender, EventArgs e) {
        Debug.Log("Swap");
    }
}
