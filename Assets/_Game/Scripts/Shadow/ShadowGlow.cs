using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShadowGlow : MonoBehaviour
{
    private Light2D glow;
    private float baseIntensity;
    public float pulseStrength = 0.3f;
    public float pulseSpeed = 2f;

    private void Awake()
    {
        glow = GetComponentInChildren<Light2D>();
        if (glow != null)
            baseIntensity = glow.intensity;
    }

    private void Update()
    {
        if (glow == null) return;

        glow.intensity = baseIntensity + Mathf.Sin(Time.time * pulseSpeed) * pulseStrength;
    }
}
