using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Manages a pulsing glow effect for a shadow/ghost using a Light2D component.
/// Adjusts intensity over time to create a breathing/pulsing visual effect.
/// </summary>
public class ShadowGlow : MonoBehaviour
{
    #region #region Inspector Fields
    [Header("Light2D Settings")]
    private Light2D glow;
    private float baseIntensity;
    [SerializeField] private float pulseStrength = 0.3f;
    [SerializeField] private float pulseSpeed = 2f;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initializes references and stores the base light intensity.
    /// </summary>
    private void Awake()
    {
        glow = GetComponentInChildren<Light2D>();
        if (glow != null)
            baseIntensity = glow.intensity;
    }

    /// <summary>
    /// Updates the light intensity each frame to create a pulsing effect.
    /// </summary>
    private void Update()
    {
        if (glow == null) return;

        glow.intensity = baseIntensity + Mathf.Sin(Time.time * pulseSpeed) * pulseStrength;
    }
    #endregion
}
