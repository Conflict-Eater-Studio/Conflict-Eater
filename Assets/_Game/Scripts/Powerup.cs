using System;
using UnityEngine;
using Random = System.Random;

public class Powerup : MonoBehaviour {
    [SerializeField] private PowerupVisual powerupVisual;
    [SerializeField] private PowerupCollision powerupCollision;
    public event EventHandler OnPowerupCollected;

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("PlayerLight") || other.TryGetComponent<ShadowController>(out var controller)) {
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
