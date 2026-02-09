using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using Random = System.Random;


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
    None,
    FarCry,
    SarcasticSmile,
    HauntedRadar
}

public enum LightPowerupType {
    None,
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
    
    public event EventHandler OnPowerupCollected;
    
    private bool _isActive = false;
    private Coroutine  shadowPowerupCoroutine;
    private Coroutine  lightPowerupCoroutine;

    private void OnTriggerEnter2D(Collider2D other) {
        if (_isActive) return;

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

            AudioManager.Instance.PlaySound(AudioManager.Instance.FMODEvents.SFX.ShadowPowerupPickup);

        }
        OnPowerupCollected?.Invoke(this, EventArgs.Empty);
    }
    private void HandleShadowPowerup(ShadowController controller) {
        switch (shadowPowerup._powerupType) {
            case ShadowPowerupType.FarCry:
                if (shadowPowerupCoroutine != null) {
                    StopCoroutine(shadowPowerupCoroutine);
                    shadowPowerupCoroutine = null;
                }
                shadowPowerupCoroutine = StartCoroutine(FarCry(controller));
                break;
            case ShadowPowerupType.SarcasticSmile:
                if (shadowPowerupCoroutine != null) {
                    StopCoroutine(shadowPowerupCoroutine);
                    shadowPowerupCoroutine = null;
                }
                shadowPowerupCoroutine = StartCoroutine(SarcasticSmile());
                break;
            case ShadowPowerupType.HauntedRadar:
                GameManager.Instance.CurrentShadowPowerupType = ShadowPowerupType.HauntedRadar;
                break;
        }
    }
    private void HandleLightPowerup() {
        switch (lightPowerup._powerupType) {
            case LightPowerupType.EmpathyMode:
                GameManager.Instance.IsFrightenedShadowState = true;
                GameManager.Instance.CurrentLightPowerupType = LightPowerupType.EmpathyMode;
                break;
            case LightPowerupType.SilentTreatment:
                if (lightPowerupCoroutine != null) {
                    StopCoroutine(lightPowerupCoroutine);
                    lightPowerupCoroutine = null;
                }
                lightPowerupCoroutine = StartCoroutine(SilentTreatment());
                GameManager.Instance.CurrentLightPowerupType = LightPowerupType.SilentTreatment;
                break;
            case LightPowerupType.DeepBreath:
                GameManager.Instance.CurrentLightPowerupType = LightPowerupType.DeepBreath;
                break;
        }
    }
    
    private IEnumerator FarCry(ShadowController controller) {
        SpeedParticleSystem ps = controller.GetComponent<SpeedParticleSystem>();
        
        if (ps != null) {
            ps.PlayFor(shadowPowerup._duration);
        }
        
        GameManager.Instance.CurrentShadowPowerupType = ShadowPowerupType.FarCry;
        controller.Movement.SetSpeed(speedBoost);
        yield return new WaitForSeconds(shadowPowerup._duration);
        GameManager.Instance.CurrentShadowPowerupType = ShadowPowerupType.None;

        if (controller) {
            controller.Movement.ResetSpeed();
        }
    }
    private IEnumerator SarcasticSmile() {
        GameManager.Instance.CurrentShadowPowerupType = ShadowPowerupType.SarcasticSmile;
        yield return new WaitForSeconds(shadowPowerup._duration);
        GameManager.Instance.CurrentShadowPowerupType = ShadowPowerupType.None;
    }
    private IEnumerator SilentTreatment() {
        GameManager.Instance.CurrentLightPowerupType = LightPowerupType.SilentTreatment;
        yield return new WaitForSeconds(lightPowerup._duration);
        GameManager.Instance.CurrentLightPowerupType = LightPowerupType.None;
    }
    private LightPowerupType GetRandomLightType() {
        var random = new Random();
        return random.Next(0, 100) > 50 ? LightPowerupType.EmpathyMode : LightPowerupType.SilentTreatment;
    }
    private ShadowPowerupType GetRandomShadowType() {
        var random = new Random();
        return random.Next(0, 100) > 50 ? ShadowPowerupType.FarCry : ShadowPowerupType.SarcasticSmile;
    }
    public void SetRandomLightPowerup() {
        lightPowerup._powerupType = GetRandomLightType();
    }
    public void SetRandomShadowPowerup() {
        shadowPowerup._powerupType = GetRandomShadowType();
    }
    public LightPowerupType GetLightPowerupType() => lightPowerup._powerupType;
    public ShadowPowerupType GetShadowPowerupType() => shadowPowerup._powerupType;
    public void Enable() {
        _isActive = false;
        _spriteRenderer.enabled = true;
        cube.SetActive(true);
        _light.enabled = true;
    }
    public void Disable() {
        _isActive = true;
        _spriteRenderer.enabled = false;
        cube.SetActive(false);
        _light.enabled = false;
    }
}
