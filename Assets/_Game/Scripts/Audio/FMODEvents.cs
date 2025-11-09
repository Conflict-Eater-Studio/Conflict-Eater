using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "FMODEvents", menuName = "Scriptable Objects/FMODEvents")]
public class FMODEvents : ScriptableObject
{
    [Header("Sound Effects")]
    public AudioEvents.SFX SFX;

    [Header("Background Music")]
    public AudioEvents.BGM BGM;

    /// <summary>
    /// Validates all event references are assigned
    /// </summary>
    private void OnValidate()
    {
        ValidateEventReferences();
    }

    private void ValidateEventReferences()
    {
        // Check SFX
        if (SFX != null)
        {
            if (SFX.SoundScore.IsNull)
                Debug.LogWarning($"[FMODEvents] SFX.SoundScore is not assigned in {name}");
        }

        // Check BGM
        if (BGM != null)
        {
            if (BGM.BGM8Bit.IsNull)
                Debug.LogWarning($"[FMODEvents] BGM.BGM8Bit is not assigned in {name}");
        }
    }
}

namespace AudioEvents
{
    [System.Serializable]
    public class SFX
    {
        [Tooltip("Sound played when scoring")]
        public EventReference SoundScore;
    }

    [System.Serializable]
    public class BGM
    {
        [Tooltip("8-bit background music")]
        public EventReference BGM8Bit;
    }
}
