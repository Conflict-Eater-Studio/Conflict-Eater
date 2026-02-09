
using System.Collections;

using UnityEngine;



public class PowerupCollision : MonoBehaviour {
    [SerializeField] private ShadowController _shadowController;
    [SerializeField] private float speedBoost = 12.5f;
    
    public LightPowerup LightPowerup { get; set; }
    public ShadowPowerup ShadowPowerup { get; set; }

    private Coroutine  shadowPowerupCoroutine;
    private Coroutine  lightPowerupCoroutine;
    
    private bool _isActive = false;

    private void OnTriggerEnter2D(Collider2D other) {
        if (!_isActive) return;

        if (other.CompareTag("PlayerLight")) {
            HandleLightPowerup();
            AudioManager.Instance.PlayOneShot(AudioManager.Instance.FMODEvents.SFX.LightPowerupPickup);
        }

        if (other.TryGetComponent<ShadowController>(out var controller)) {
            if (controller.CurrentState is not ShadowActiveState) return;
            HandleShadowPowerup(controller);
            AudioManager.Instance.PlaySound(AudioManager.Instance.FMODEvents.SFX.ShadowPowerupPickup);
        }
    }
    private void HandleShadowPowerup(ShadowController controller) {
        switch (ShadowPowerup._powerupType) {
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
        switch (LightPowerup._powerupType) {
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
            ps.PlayFor(ShadowPowerup._duration);
        }
        
        GameManager.Instance.CurrentShadowPowerupType = ShadowPowerupType.FarCry;
        controller.Movement.SetSpeed(speedBoost);
        yield return new WaitForSeconds(ShadowPowerup._duration);
        GameManager.Instance.CurrentShadowPowerupType = ShadowPowerupType.None;

        if (controller) {
            controller.Movement.ResetSpeed();
        }
    }
    private IEnumerator SarcasticSmile() {
        GameManager.Instance.CurrentShadowPowerupType = ShadowPowerupType.SarcasticSmile;
        yield return new WaitForSeconds(ShadowPowerup._duration);
        GameManager.Instance.CurrentShadowPowerupType = ShadowPowerupType.None;
    }
    private IEnumerator SilentTreatment() {
        GameManager.Instance.CurrentLightPowerupType = LightPowerupType.SilentTreatment;
        yield return new WaitForSeconds(LightPowerup._duration);
        GameManager.Instance.CurrentLightPowerupType = LightPowerupType.None;
    }

    public void EnableCollision() {
        _isActive = true;
    }
    public void DisableCollision() {
        _isActive = false;
    }
}
