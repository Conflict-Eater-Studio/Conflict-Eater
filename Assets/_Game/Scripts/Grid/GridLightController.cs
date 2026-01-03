using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// GridLightController handles all lights under a grid parent.
/// It can:
/// - Change light colors and global bloom depending on the frightened state
/// - Pulse light intensity when frightened
/// - Automatically set colors for new lights added to the hierarchy
/// </summary>
public class GridLightController : MonoBehaviour
{
    #region Inspector Fields
    [Header("Light Colors")]
    [SerializeField] private Color _normalLightColor;
    [SerializeField] private Color _frightenedLightColor;

    [Header("Bloom Colors")]
    [SerializeField] private Color _normalBloomColor;
    [SerializeField] private Color _frightenedBloomColor;

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
    private bool _lastFrightenedState;
    private Tween _intensityTween;
    private float _currentPulseValue;
    private Bloom _bloom;

    private readonly List<Light2D> _lights = new();
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
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

        _lastFrightenedState = GameManager.Instance.IsFrightenedShadowState;
        ApplyState(_lastFrightenedState, force: true);
    }

    private void Update()
    {
        bool currentState = GameManager.Instance.IsFrightenedShadowState;

        if (currentState != _lastFrightenedState)
        {
            _lastFrightenedState = currentState;
            ApplyState(currentState, force: true);
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

        Color targetColor = _lastFrightenedState
            ? _frightenedLightColor
            : _normalLightColor;

        foreach (var light in _lights)
        {
            light.color = targetColor;

            if (_lastFrightenedState)
            {
                light.intensity = _currentPulseValue;
            }
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
    private void ApplyState(bool frightened, bool force)
    {
        if (frightened)
        {
            StartPulse();
            SetLightsColor(_frightenedLightColor);
            SetBloomColor(_frightenedBloomColor);
        }
        else
        {
            StopPulse();
            SetLightsColor(_normalLightColor);
            SetLightsIntensity(_normalIntensity);
            SetBloomColor(_normalBloomColor);
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

}
