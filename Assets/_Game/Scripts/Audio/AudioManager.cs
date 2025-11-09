using System;
using System.Collections.Generic;
using System.Linq;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using RESULT = FMOD.RESULT;
using STOP_MODE = FMOD.Studio.STOP_MODE;

/// <summary>
/// FMOD audio management system
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField]
    private FMODEvents _fmodEvents;

    [SerializeField]
    private bool _enableDebugLogs = false;

    private Dictionary<Guid, EventInstance> _activeInstances =
        new Dictionary<Guid, EventInstance>();
    private float _masterVolume = 1f;
    private float _sfxVolume = 1f;
    private float _bgmVolume = 1f;

    public FMODEvents FMODEvents => _fmodEvents;
    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = Mathf.Clamp01(value);
            UpdateMasterVolume();
        }
    }
    public float SFXVolume
    {
        get => _sfxVolume;
        set
        {
            _sfxVolume = Mathf.Clamp01(value);
            UpdateSFXVolume();
        }
    }
    public float BGMVolume
    {
        get => _bgmVolume;
        set
        {
            _bgmVolume = Mathf.Clamp01(value);
            UpdateBGMVolume();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_fmodEvents == null)
        {
            Debug.LogError("AudioManager: FMODEvents ScriptableObject is not assigned!");
        }
    }

    private void Start()
    {
        if (_fmodEvents != null && !_fmodEvents.BGM.BGM8Bit.IsNull)
        {
            PlaySound(_fmodEvents.BGM.BGM8Bit);
        }
    }

    private void OnDestroy()
    {
        // Create a list to avoid modifying dictionary during iteration
        List<Guid> instanceIds = _activeInstances.Keys.ToList();
        foreach (var id in instanceIds)
        {
            RemoveEventInstance(id);
        }
        _activeInstances.Clear();
    }

    /// <summary>
    /// Plays a one-shot sound at a specific world position (fire and forget, no tracking)
    /// </summary>
    public void PlayOneShot(EventReference sound, Vector3 worldPosition)
    {
        if (sound.IsNull)
        {
            LogWarning("Attempted to play null EventReference as one-shot");
            return;
        }

        RuntimeManager.PlayOneShot(sound, worldPosition);
    }

    /// <summary>
    /// Plays a one-shot sound (fire and forget, no tracking)
    /// </summary>
    public void PlayOneShot(EventReference sound)
    {
        if (sound.IsNull)
        {
            LogWarning("Attempted to play null EventReference as one-shot");
            return;
        }

        RuntimeManager.PlayOneShot(sound);
    }

    /// <summary>
    /// Plays a sound and returns a Guid to control the instance.
    /// You must manually stop and remove the instance when done.
    /// </summary>
    public Guid PlaySound(EventReference sound, bool autoStart = true)
    {
        if (sound.IsNull)
        {
            LogWarning("Attempted to play null EventReference");
            return Guid.Empty;
        }

        try
        {
            EventInstance instance = RuntimeManager.CreateInstance(sound);

            if (!instance.isValid())
            {
                LogError("Failed to create valid FMOD instance");
                return Guid.Empty;
            }

            Guid id = Guid.NewGuid();
            _activeInstances.Add(id, instance);

            if (autoStart)
            {
                RESULT result = instance.start();
                if (result != RESULT.OK)
                {
                    LogError($"Failed to start FMOD instance: {result}");
                    RemoveEventInstance(id);
                    return Guid.Empty;
                }
            }

            LogDebug($"Created sound instance: {id}");
            return id;
        }
        catch (Exception e)
        {
            LogError($"Exception creating sound instance: {e.Message}");
            return Guid.Empty;
        }
    }

    /// <summary>
    /// Starts a previously created but not started instance
    /// </summary>
    public bool StartEventInstance(Guid id)
    {
        if (_activeInstances.TryGetValue(id, out var instance))
        {
            RESULT result = instance.start();
            if (result != RESULT.OK)
            {
                LogError($"Failed to start instance {id}: {result}");
                return false;
            }
            return true;
        }
        LogWarning($"Instance {id} not found");
        return false;
    }

    /// <summary>
    /// Stops an event instance with optional fade
    /// </summary>
    public bool StopEventInstance(Guid id, STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
    {
        if (_activeInstances.TryGetValue(id, out var instance))
        {
            RESULT result = instance.stop(stopMode);
            if (result != RESULT.OK)
            {
                LogError($"Failed to stop instance {id}: {result}");
                return false;
            }

            // Remove immediately if stopping without fadeout
            if (stopMode == STOP_MODE.IMMEDIATE)
            {
                RemoveEventInstance(id);
            }

            return true;
        }
        LogWarning($"Instance {id} not found");
        return false;
    }

    /// <summary>
    /// Pauses an event instance
    /// </summary>
    public bool PauseEventInstance(Guid id, bool pause)
    {
        if (_activeInstances.TryGetValue(id, out var instance))
        {
            RESULT result = instance.setPaused(pause);
            if (result != RESULT.OK)
            {
                LogError($"Failed to pause instance {id}: {result}");
                return false;
            }
            return true;
        }
        LogWarning($"Instance {id} not found");
        return false;
    }

    /// <summary>
    /// Sets a parameter on an event instance
    /// </summary>
    public bool SetInstanceParameter(Guid id, string parameterName, float value)
    {
        if (_activeInstances.TryGetValue(id, out var instance))
        {
            RESULT result = instance.setParameterByName(parameterName, value);
            if (result != RESULT.OK)
            {
                LogError($"Failed to set parameter {parameterName} on instance {id}: {result}");
                return false;
            }
            return true;
        }
        LogWarning($"Instance {id} not found");
        return false;
    }

    /// <summary>
    /// Gets the playback state of an event instance
    /// </summary>
    public bool IsInstancePlaying(Guid id)
    {
        if (_activeInstances.TryGetValue(id, out var instance))
        {
            RESULT result = instance.getPlaybackState(out PLAYBACK_STATE state);
            if (result == RESULT.OK)
            {
                return state == PLAYBACK_STATE.PLAYING || state == PLAYBACK_STATE.STARTING;
            }
        }
        return false;
    }

    /// <summary>
    /// Stops and releases an event instance
    /// </summary>
    public bool RemoveEventInstance(Guid id)
    {
        if (_activeInstances.TryGetValue(id, out var instance))
        {
            instance.stop(STOP_MODE.IMMEDIATE);
            instance.release();
            _activeInstances.Remove(id);
            LogDebug($"Removed instance: {id}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Stops all active instances
    /// </summary>
    public void StopAllSounds(STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
    {
        List<Guid> instanceIds = _activeInstances.Keys.ToList();
        foreach (var id in instanceIds)
        {
            StopEventInstance(id, stopMode);
        }
    }

    /// <summary>
    /// Pauses or resumes all active instances
    /// </summary>
    public void PauseAllSounds(bool pause)
    {
        foreach (var instance in _activeInstances.Values)
        {
            instance.setPaused(pause);
        }
    }

    private void UpdateMasterVolume()
    {
        RuntimeManager.GetBus("bus:/").setVolume(_masterVolume);
    }

    private void UpdateSFXVolume()
    {
        RuntimeManager.GetBus("bus:/SFX").setVolume(_sfxVolume);
    }

    private void UpdateBGMVolume()
    {
        RuntimeManager.GetBus("bus:/BGM").setVolume(_bgmVolume);
    }

    private void LogDebug(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[AudioManager] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[AudioManager] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[AudioManager] {message}");
    }
}
