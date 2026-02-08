using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;
using System.Collections.Generic;
using System;
using UnityEngine.Tilemaps;

/// <summary>
/// GridLightController handles all lights under a grid parent.
/// It can:
/// - Change light colors and global bloom depending on the frightened state
/// - Pulse light intensity when frightened
/// - Automatically set colors for new lights added to the hierarchy
/// </summary>
public class GridLightController : MonoBehaviour
{
    /// <summary>
    /// Defines a color transition pair used for animated material tinting.
    /// From - starting color
    /// To   - target color
    /// </summary>
    [Serializable]
    struct MaterialColors
    {
        public Color From;
        public Color To;
    }

    #region Inspector Fields
    [Header("Light Colors")]
    [SerializeField] private Color _normalLightColor;
    [SerializeField] private Color _empathyModeLightColor;
    [SerializeField] private Color _silentTreatmentLightColor;

    [Header("Bloom Colors")]
    [SerializeField] private Color _normalBloomColor;
    [SerializeField] private Color _empathyModeBloomColor;
    [SerializeField] private Color _silentTreatmentBloomColor;

    [Header("Material Colors")]
    [SerializeField] private MaterialColors _normalMaterialColors;
    [SerializeField] private MaterialColors _farCryMaterialColors;
    [SerializeField] private MaterialColors _sarcasticSmileMaterialColors;

    [Header("TilemapRenderer")]
    [SerializeField] private TilemapRenderer _tilemapRenderer; 
    private Material _material;

    [Header("Intensity Settings")]
    [Range(0f, 5f)]
    [SerializeField] private float _normalIntensity = 2f;

    [Range(0f, 5f)]
    [SerializeField] private float _frightenedMinIntensity = 0.3f;

    [Range(0f, 5f)]
    [SerializeField] private float _frightenedMaxIntensity = 2f;

    [Range(0f, 2f)]
    [SerializeField] private float _pulseDuration = 0.6f;
    #endregion

    #region Private Fields
    private LightPowerupType _lastPowerupType;
    private Tween _intensityTween;
    private float _currentPulseValue;
    private Bloom _bloom;
    private Tween _materialTween;
    private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
    private ShadowPowerupType _lastShadowType;

    private readonly List<Light2D> _lights = new();
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        _material = _tilemapRenderer.material;
        CacheLights();

        if (GameManager.Instance.GetGlobalVolume() != null &&
                GameManager.Instance.GetGlobalVolume().profile.TryGet(out _bloom))
        {
            // OK
        }
        else
        {
            Debug.LogWarning("Bloom not found in Global Volume");
        }

        _lastPowerupType = GameManager.Instance.CurrentLightPowerupType;
        ApplyState(_lastPowerupType, true);

        _lastShadowType = GameManager.Instance.CurrentShadowPowerupType;
        ApplyShadowMaterial(_lastShadowType);
    }

    private void Update()
    {
        var currentType = GameManager.Instance.CurrentLightPowerupType;

        if (currentType != _lastPowerupType)
        {
            _lastPowerupType = currentType;
            ApplyState(currentType, true);
        }

        var shadowType = GameManager.Instance.CurrentShadowPowerupType;

        if (shadowType != _lastShadowType)
        {
            _lastShadowType = shadowType;
            ApplyShadowMaterial(shadowType);
        }

    }

    /// <summary>
    /// Called automatically when children are added or removed.
    /// Updates cached lights and ensures new lights have the correct color
    /// and current pulse value if frightened.
    /// </summary>
    private void OnTransformChildrenChanged()
    {
        CacheLights();

        Color targetColor = _lastPowerupType switch
        {
            LightPowerupType.EmpathyMode => _empathyModeLightColor,
            LightPowerupType.SilentTreatment => _silentTreatmentLightColor,
            _ => _normalLightColor
        };

        foreach (var light in _lights)
        {
            light.color = targetColor;

            if (_lastPowerupType != LightPowerupType.None)
                light.intensity = _currentPulseValue;
        }

    }
    #endregion

    #region Light Management
    /// <summary>
    /// Cache all Light2D components under this GameObject.
    /// </summary>
    private void CacheLights()
    {
        _lights.Clear();
        _lights.AddRange(GetComponentsInChildren<Light2D>(true));
    }

    /// <summary>
    /// Apply the given state to lights and bloom.
    /// If frightened, start pulsing; otherwise set static intensity and normal colors.
    /// </summary>
    private void ApplyState(LightPowerupType type, bool force)
    {
        StopPulse();

        switch (type)
        {
            case LightPowerupType.None:
                SetLightsColor(_normalLightColor);
                SetLightsIntensity(_normalIntensity);
                SetBloomColor(_normalBloomColor);
                break;

            case LightPowerupType.EmpathyMode:
                StartPulse();
                SetLightsColor(_empathyModeLightColor);
                SetBloomColor(_empathyModeBloomColor);
                break;

            case LightPowerupType.SilentTreatment:
                StartPulse();
                SetLightsColor(_silentTreatmentLightColor);
                SetBloomColor(_silentTreatmentBloomColor);
                break;
        }
    }

    /// <summary>
    /// Starts a looping DOTween to pulse light intensity between min and max values.
    /// </summary>
    private void StartPulse()
    {
        if (_intensityTween != null && _intensityTween.IsActive())
            return;

        _currentPulseValue = _frightenedMinIntensity;

        _intensityTween = DOTween.To(
            () => _currentPulseValue,
            value =>
            {
                _currentPulseValue = value;
                foreach (var light in _lights)
                    light.intensity = value;
            },
            _frightenedMaxIntensity,
            _pulseDuration
        )
        .SetEase(Ease.InOutSine)
        .SetLoops(-1, LoopType.Yoyo);
    }

    /// <summary>
    /// Stops the pulsing tween.
    /// </summary>
    private void StopPulse()
    {
        _intensityTween?.Kill();
        _intensityTween = null;
    }

    /// <summary>
    /// Sets all cached lights to the given color.
    /// </summary>
    private void SetLightsColor(Color color)
    {
        foreach (var light in _lights)
            light.color = color;
    }

    /// <summary>
    /// Sets all cached lights to the given intensity value.
    /// </summary>
    private void SetLightsIntensity(float value)
    {
        foreach (var light in _lights)
            light.intensity = value;
    }

    /// <summary>
    /// Updates the global Bloom tint color if Bloom exists.
    /// </summary>
    private void SetBloomColor(Color color)
    {
        if (_bloom == null)
            return;

        _bloom.tint.value = color;
    }
    #endregion

    #region Material Management 
    /// <summary>
    /// Applies the appropriate material color animation depending on the active
    /// ShadowPowerupType. Each powerup maps to its own color profile defined
    /// in the inspector.
    /// </summary>
    /// <param name="type">Current shadow powerup type.</param>
    private void ApplyShadowMaterial(ShadowPowerupType type)
    {
        switch (type)
        {
            case ShadowPowerupType.None:
                AnimateMaterialColor(_normalMaterialColors);
                break;

            case ShadowPowerupType.FarCry:
                AnimateMaterialColor(_farCryMaterialColors);
                break;

            case ShadowPowerupType.SarcasticSmile:
                AnimateMaterialColor(_sarcasticSmileMaterialColors);
                break;
        }
    }

    /// <summary>
    /// Animates the tilemap material base color between two values using DOTween.
    /// Visual feedback for active shadow powerups.
    /// </summary>
    /// <param name="colors">
    /// Struct containing start (From) and target (To) colors.
    /// </param>
    private void AnimateMaterialColor(MaterialColors colors)
    {
        _materialTween?.Kill();

        _material.SetColor(ColorId, colors.From);

        _materialTween = DOTween.To(
            () => _material.GetColor(ColorId),
            c => _material.SetColor(ColorId, c),
            colors.To,
            0.5f
        )
        .SetLoops(-1, LoopType.Yoyo)
        .SetEase(Ease.InOutSine);
    }
    #endregion
}
