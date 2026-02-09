using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PowerupVisual : MonoBehaviour {
    [SerializeField] private PowerupAnimation _powerupAnimation;
    [SerializeField] private GameObject _cube;
    [SerializeField] private Light2D _light;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private ParticleSystem _particleSystem;

    private const int PARTICLE_EMIT_COUNT = 50;
    private const int LIGHT_MATERIAL_INDEX = 0;
    private const int SHADOW_MATERIAL_INDEX = 2;

    private bool _isActive = false;
    private MeshRenderer m_meshRenderer;

    private void Awake() {
        m_meshRenderer = _cube.GetComponent<MeshRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (!_isActive) return;
        
        if (other.CompareTag("PlayerLight")) {
            _particleSystem.Emit(PARTICLE_EMIT_COUNT);
        } else if (other.TryGetComponent<ShadowController>(out var controller)) {
            if (controller.CurrentState is not ShadowActiveState) return;
            _particleSystem.Emit(PARTICLE_EMIT_COUNT);
        }
    }

    /// <summary>
    /// Sets the light and shadow materials on the cube renderer.
    /// </summary>
    public void SetMaterial(Material lightMaterial, Material shadowMaterial) {
        Material[] newMaterials = m_meshRenderer.materials;
        newMaterials[LIGHT_MATERIAL_INDEX] = lightMaterial;
        newMaterials[SHADOW_MATERIAL_INDEX] = shadowMaterial;
        m_meshRenderer.materials = newMaterials;
    }

    public void Hide() {
        _spriteRenderer.enabled = false;
        _cube.SetActive(false);
        _light.enabled = false;
        _isActive = false;
        _powerupAnimation.StopAnimation();
    }

    public void Show() {
        _spriteRenderer.enabled = true;
        _cube.SetActive(true);
        _light.enabled = true;
        _isActive = true;
        _powerupAnimation.StartAnimation();
    }
}