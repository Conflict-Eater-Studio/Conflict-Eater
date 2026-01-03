using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;


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
    [SerializeField] private GameObject cube;
    [SerializeField] private Light2D _light;
    [SerializeField] private ShadowController _shadowController;
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
            AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.SFX.LightPowerupPickup);
            

        }

        if (other.TryGetComponent<ShadowController>(out var controller)) {
            if (controller.CurrentState is not ShadowActiveState) return;
            HandleShadowPowerup(controller);
            Disable();
            SpeedParticleSystem ps = other.GetComponent<SpeedParticleSystem>();
            if (ps != null) {
                ps.PlayFor(shadowPowerup._duration);
            }
                AudioManager.Instance.PlaySound(AudioManager.Instance.FMODEvents.SFX.ShadowPowerupPickup);

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
        yield return new WaitForSeconds(shadowPowerup._duration);

        if (controller != null && controller.CurrentState is ShadowActiveState) {
            controller.Movement.ResetSpeed();
        }
    }
    public void Enable() {
        _collected = false;
        _spriteRenderer.enabled = true;
        cube.SetActive(true);
        _light.enabled = true;
    }
    public void Disable() {
        _collected = true;
        _spriteRenderer.enabled = false;
        cube.SetActive(false);
        _light.enabled = false;
    }
}
