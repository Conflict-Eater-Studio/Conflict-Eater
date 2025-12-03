using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;


[Serializable]
public class LightPowerup {
    public LightPowerupType _powerupType;
    public int _duration;

}
[Serializable]
public class ShadowPowerup{
    public ShadowPowerupType _powerupType;
    public int _duration;
}
public enum ShadowPowerupType {
    FarCry,
    SarcasticSmile,
    HauntedRadar
}

public enum LightPowerupType {
    DeepBreath,
    EmpathyMode,
    SilentTreatment
}
public class PowerupCollision : MonoBehaviour {
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private LightPowerup lightPowerup;
    [SerializeField] private ShadowPowerup shadowPowerup;
    [SerializeField] private ParticleSystem _particleSystem;
    [SerializeField] private float speedBoost = 15f;
    
    private bool _collected = false;
    private Coroutine boostRoutine;

    private void OnTriggerEnter2D(Collider2D other) {
        if (_collected) return;

        if (other.CompareTag("PlayerLight")) {
            HandleLightPowerup();
            Disable();
            _particleSystem.Emit(50);
            AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.SFX.PowerupPickup);
            

        }

        if (other.TryGetComponent<ShadowController>(out var controller)) {
            if (controller.CurrentState is not ShadowActiveState) return;
            HandleShadowPowerup(controller);
            Disable();
            _particleSystem.Emit(50);
            AudioManager.Instance.PlaySound(AudioManager.Instance.FMODEvents.SFX.PowerupPickup);

        }
    }
    private void HandleShadowPowerup(ShadowController controller) {
        switch (shadowPowerup._powerupType) {
            case ShadowPowerupType.FarCry:
                if (boostRoutine != null) {
                    StopCoroutine(boostRoutine);
                    boostRoutine = null;
                }
                boostRoutine = StartCoroutine(BoostShadowSpeed(controller));
                break;
            case ShadowPowerupType.SarcasticSmile:
                break;
            case ShadowPowerupType.HauntedRadar:
                break;
        }
    }
    private void HandleLightPowerup() {
        switch (lightPowerup._powerupType) {
            case LightPowerupType.EmpathyMode:
                GameManager.Instance.IsFrightenedShadowState = true;
            break;
            case LightPowerupType.SilentTreatment:
                break;
            case LightPowerupType.DeepBreath:
                break;
        }
    }

    private IEnumerator BoostShadowSpeed(ShadowController controller) {
        controller.Movement.SetSpeed(speedBoost);
        Debug.Log("Shadow speed boosted to " + speedBoost);
        yield return new WaitForSeconds(shadowPowerup._duration);

        if (controller != null && controller.CurrentState is ShadowActiveState) {
            controller.Movement.ResetSpeed();
            Debug.Log("Shadow speed boosted");
        }
    }
    public void Enable() {
        _collected = false;
        _spriteRenderer.enabled = true;
        
    }
    public void Disable() {
        _collected = true;
        _spriteRenderer.enabled = false;
    }
}
