using System;
using UnityEngine;
using Random = System.Random;

public class Powerup : MonoBehaviour {
    [SerializeField] private PowerupVisual powerupVisual;
    [SerializeField] private PowerupCollision powerupCollision;
    public event EventHandler OnPowerupCollected;

    private void OnEnable() {
        powerupCollision.OnPowerupEnd += Powerup_OnPowerupEnd;
    }
    private void OnDisable() {
        powerupCollision.OnPowerupEnd -= Powerup_OnPowerupEnd;
    }
    private void Powerup_OnPowerupEnd(object sender, EventArgs e) {
        Disable();
    }
    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("PlayerLight")){
            OnPowerupCollected?.Invoke(this, EventArgs.Empty);
        } else if (other.TryGetComponent<ShadowController>(out var controller)) {
            if (controller.CurrentState is not ShadowActiveState) return;
            OnPowerupCollected?.Invoke(this, EventArgs.Empty);
        }
    }
    public void Enable() {
        powerupVisual.Show();
        powerupCollision.EnableCollision();
    }

    public void Disable() {
        powerupVisual.Hide();
        powerupCollision.DisableCollision();
    }

    public void SetPowerup(LightPowerup lightPowerup, ShadowPowerup shadowPowerup) {
        SetLightPowerup(lightPowerup);
        SetShadowPowerup(shadowPowerup);
    }
    
    private void SetLightPowerup(LightPowerup lightPowerup) {
        powerupCollision.LightPowerup = lightPowerup;
    }
    private void SetShadowPowerup(ShadowPowerup shadowPowerup) {
        powerupCollision.ShadowPowerup = shadowPowerup;
    }

}
