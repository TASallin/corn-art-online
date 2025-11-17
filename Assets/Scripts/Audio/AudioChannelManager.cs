using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class AudioChannelManager : MonoBehaviour
{
    [System.Serializable]
    public class PriorityLevel
    {
        public int maxChannels;
        public int currentlyActive;
        [Range(0f, 1f)] public float duckingVolume = 1f; // Volume multiplier when ducked
    }

    public enum AudioType
    {
        Voice,
        SoundEffect
    }

    [Header("Channel Settings")]
    [SerializeField] private List<PriorityLevel> voicePriorityLevels = new List<PriorityLevel>();
    [SerializeField] private List<PriorityLevel> sfxPriorityLevels = new List<PriorityLevel>();

    [Header("Ducking Settings")]
    [SerializeField] private float duckingFadeTime = 0.2f;
    [SerializeField] private int duckingPriorityThreshold = 2; // Sources below this priority get ducked

    private HashSet<AudioSource> activeVoiceSources = new HashSet<AudioSource>();
    private HashSet<AudioSource> activeSFXSources = new HashSet<AudioSource>();
    private Dictionary<AudioSource, float> sourceCooldowns = new Dictionary<AudioSource, float>();
    private Dictionary<AudioSource, int> sourcePriorities = new Dictionary<AudioSource, int>();
    private Dictionary<AudioSource, float> originalVolumes = new Dictionary<AudioSource, float>();
    private Dictionary<AudioSource, AudioType> sourceTypes = new Dictionary<AudioSource, AudioType>();

    private Camera mainCamera;

    private static AudioChannelManager instance;
    public static AudioChannelManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AudioChannelManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("AudioChannelManager");
                    instance = go.AddComponent<AudioChannelManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Cache the main camera
            mainCamera = Camera.main;

            // Initialize default priority levels with ducking values
            if (voicePriorityLevels.Count == 0)
            {
                voicePriorityLevels.Add(new PriorityLevel { maxChannels = 2, duckingVolume = 0.3f });
                voicePriorityLevels.Add(new PriorityLevel { maxChannels = 2, duckingVolume = 0.5f });
                voicePriorityLevels.Add(new PriorityLevel { maxChannels = 2, duckingVolume = 1.0f });
            }
            if (sfxPriorityLevels.Count == 0)
            {
                sfxPriorityLevels.Add(new PriorityLevel { maxChannels = 2, duckingVolume = 0.3f });
                sfxPriorityLevels.Add(new PriorityLevel { maxChannels = 2, duckingVolume = 0.5f });
                sfxPriorityLevels.Add(new PriorityLevel { maxChannels = 2, duckingVolume = 1.0f });
            }

            // Subscribe to audio settings changes
            AudioSettings.OnAudioSettingsChanged += ApplyAudioSettingsToActiveSources;
        } else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        AudioSettings.OnAudioSettingsChanged -= ApplyAudioSettingsToActiveSources;
    }

    private void Update()
    {
        // Update camera reference if needed
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Update cooldowns
        UpdateCooldowns();

        // Clean up finished audio sources
        CleanupFinishedSources();

        // Update ducking
        UpdateDucking();
    }

    public bool IsAudioSourceOnScreen(AudioSource source)
    {
        if (source == null || mainCamera == null) return false;

        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(source.transform.position);

        // Check if the point is in front of the camera and within viewport bounds
        return viewportPoint.z > 0 &&
               viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1;
    }

    private void ApplyAudioSettingsToActiveSources()
    {
        if (AudioSettings.Instance == null) return;

        // Apply to all active voice sources
        foreach (var source in activeVoiceSources)
        {
            if (source != null && originalVolumes.TryGetValue(source, out float originalVolume))
            {
                float effectiveVolume = originalVolume * AudioSettings.Instance.GetEffectiveVolume(AudioType.Voice);
                source.volume = effectiveVolume;
            }
        }

        // Apply to all active SFX sources
        foreach (var source in activeSFXSources)
        {
            if (source != null && originalVolumes.TryGetValue(source, out float originalVolume))
            {
                float effectiveVolume = originalVolume * AudioSettings.Instance.GetEffectiveVolume(AudioType.SoundEffect);
                source.volume = effectiveVolume;
            }
        }
    }

    private void UpdateCooldowns()
    {
        // Create a list of keys to update separately to avoid modifying during iteration
        List<AudioSource> sourcesToUpdate = new List<AudioSource>(sourceCooldowns.Keys);
        List<AudioSource> cooldownsToRemove = new List<AudioSource>();

        foreach (var source in sourcesToUpdate)
        {
            float currentCooldown = sourceCooldowns[source];
            if (currentCooldown > 0)
            {
                currentCooldown -= Time.unscaledDeltaTime;
                if (currentCooldown <= 0)
                {
                    cooldownsToRemove.Add(source);
                } else
                {
                    sourceCooldowns[source] = currentCooldown;
                }
            }
        }

        foreach (var source in cooldownsToRemove)
        {
            sourceCooldowns.Remove(source);
        }
    }

    private void CleanupFinishedSources()
    {
        // Clean up voice sources
        var voicesToRemove = new List<AudioSource>();
        foreach (var source in activeVoiceSources)
        {
            if (source == null || !source.isPlaying)
            {
                voicesToRemove.Add(source);
            }
        }
        foreach (var source in voicesToRemove)
        {
            activeVoiceSources.Remove(source);
            sourcePriorities.Remove(source);
            originalVolumes.Remove(source);
            sourceTypes.Remove(source);
        }

        // Clean up SFX sources
        var sfxToRemove = new List<AudioSource>();
        foreach (var source in activeSFXSources)
        {
            if (source == null || !source.isPlaying)
            {
                sfxToRemove.Add(source);
            }
        }
        foreach (var source in sfxToRemove)
        {
            activeSFXSources.Remove(source);
            sourcePriorities.Remove(source);
            originalVolumes.Remove(source);
            sourceTypes.Remove(source);
        }

        // Update active counts
        UpdateActiveCounts();
    }

    private void UpdateDucking()
    {
        // Check if any high priority sounds are playing
        bool shouldDuck = false;
        foreach (var source in activeVoiceSources)
        {
            if (sourcePriorities.TryGetValue(source, out int priority) && priority >= duckingPriorityThreshold)
            {
                shouldDuck = true;
                break;
            }
        }

        if (!shouldDuck)
        {
            foreach (var source in activeSFXSources)
            {
                if (sourcePriorities.TryGetValue(source, out int priority) && priority >= duckingPriorityThreshold)
                {
                    shouldDuck = true;
                    break;
                }
            }
        }

        // Duck or restore volumes
        if (shouldDuck)
        {
            //ApplyDucking(activeVoiceSources, voicePriorityLevels);
            //ApplyDucking(activeSFXSources, sfxPriorityLevels);
        } else
        {
            RestoreVolumes(activeVoiceSources);
            RestoreVolumes(activeSFXSources);
        }
    }

    private void ApplyDucking(HashSet<AudioSource> sources, List<PriorityLevel> priorityLevels)
    {
        foreach (var source in sources)
        {
            if (source != null && sourcePriorities.TryGetValue(source, out int priority))
            {
                if (priority < duckingPriorityThreshold)
                {
                    int clampedPriority = Mathf.Min(priority, priorityLevels.Count - 1);
                    float targetVolume = originalVolumes[source] * priorityLevels[clampedPriority].duckingVolume;

                    if (sourceTypes.TryGetValue(source, out AudioType type) && AudioSettings.Instance != null)
                    {
                        targetVolume *= AudioSettings.Instance.GetEffectiveVolume(type);
                    }

                    StartCoroutine(FadeVolume(source, targetVolume, duckingFadeTime));
                }
            }
        }
    }

    private void RestoreVolumes(HashSet<AudioSource> sources)
    {
        foreach (var source in sources)
        {
            if (source != null && originalVolumes.TryGetValue(source, out float originalVolume))
            {
                float targetVolume = originalVolume;

                if (sourceTypes.TryGetValue(source, out AudioType type) && AudioSettings.Instance != null)
                {
                    targetVolume *= AudioSettings.Instance.GetEffectiveVolume(type);
                }

                StartCoroutine(FadeVolume(source, targetVolume, duckingFadeTime));
            }
        }
    }

    private IEnumerator FadeVolume(AudioSource source, float targetVolume, float fadeTime)
    {
        if (source == null) yield break;

        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            if (source == null) yield break;

            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        if (source != null)
        {
            source.volume = targetVolume;
        }
    }

    private bool IsSourceOnCooldown(AudioSource source)
    {
        return sourceCooldowns.TryGetValue(source, out float cooldown) && cooldown > 0;
    }

    private void SetSourceCooldown(AudioSource source, float duration)
    {
        sourceCooldowns[source] = duration;
    }

    private void UpdateActiveCounts()
    {
        // Reset all counts
        foreach (var level in voicePriorityLevels)
        {
            level.currentlyActive = 0;
        }
        foreach (var level in sfxPriorityLevels)
        {
            level.currentlyActive = 0;
        }

        // Count active channels based on their priorities
        CountActiveSources(activeVoiceSources, voicePriorityLevels);
        CountActiveSources(activeSFXSources, sfxPriorityLevels);
    }

    private void CountActiveSources(HashSet<AudioSource> sources, List<PriorityLevel> levels)
    {
        foreach (var source in sources)
        {
            if (sourcePriorities.TryGetValue(source, out int priority))
            {
                // Clamp priority to valid range
                int clampedPriority = Mathf.Min(priority, levels.Count - 1);
                levels[clampedPriority].currentlyActive++;
            }
        }
    }

    public bool CanPlayAudio(AudioType type, int priority)
    {
        List<PriorityLevel> levels = type == AudioType.Voice ? voicePriorityLevels : sfxPriorityLevels;

        // Clamp priority to valid range for checking
        int clampedPriority = Mathf.Min(priority, levels.Count - 1);

        // Check if we have space in any priority level up to the requested one
        for (int i = 0; i <= clampedPriority; i++)
        {
            if (levels[i].currentlyActive < levels[i].maxChannels)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryPlayAudio(AudioSource audioSource, AudioType type, int priority = 0)
    {
        if (audioSource == null) return false;

        // Check cooldown
        if (IsSourceOnCooldown(audioSource))
        {
            return false;
        }

        // Don't interrupt currently playing audio
        if (audioSource.isPlaying)
        {
            return false;
        }

        if (!CanPlayAudio(type, priority))
        {
            return false;
        }

        // Store original volume for ducking
        if (!originalVolumes.ContainsKey(audioSource))
        {
            originalVolumes[audioSource] = audioSource.volume;
        }

        // Add to appropriate active list
        if (type == AudioType.Voice)
        {
            activeVoiceSources.Add(audioSource);
        } else
        {
            activeSFXSources.Add(audioSource);
        }

        sourcePriorities[audioSource] = priority;
        sourceTypes[audioSource] = type;

        // Apply audio settings
        float effectiveVolume = originalVolumes[audioSource] *
        (AudioSettings.Instance != null ? AudioSettings.Instance.GetEffectiveVolume(type) : 1f);
        audioSource.volume = effectiveVolume;
        audioSource.volume = effectiveVolume;

        audioSource.Play();
        audioSource.ignoreListenerPause = true;

        // Set cooldown based on clip length
        if (audioSource.clip != null)
        {
            SetSourceCooldown(audioSource, audioSource.clip.length * 1.5f);
        }

        UpdateActiveCounts();
        return true;
    }

    public bool TryPlayAudio(AudioSource audioSource, AudioClip clip, AudioType type, int priority = 0)
    {
        if (audioSource == null || clip == null) return false;

        // Check cooldown
        if (IsSourceOnCooldown(audioSource))
        {
            return false;
        }

        // Don't interrupt currently playing audio
        if (audioSource.isPlaying)
        {
            return false;
        }

        if (!CanPlayAudio(type, priority))
        {
            return false;
        }

        // Store original volume for ducking
        if (!originalVolumes.ContainsKey(audioSource))
        {
            originalVolumes[audioSource] = audioSource.volume;
        }

        // Only change the clip if we can play
        audioSource.clip = clip;

        // Add to appropriate active list
        if (type == AudioType.Voice)
        {
            activeVoiceSources.Add(audioSource);
        } else
        {
            activeSFXSources.Add(audioSource);
        }

        sourcePriorities[audioSource] = priority;
        sourceTypes[audioSource] = type;

        // Apply audio settings
        float effectiveVolume = originalVolumes[audioSource] * AudioSettings.Instance.GetEffectiveVolume(type);
        audioSource.volume = effectiveVolume;

        audioSource.Play();

        // Set cooldown based on clip length
        SetSourceCooldown(audioSource, clip.length * 1.5f);

        UpdateActiveCounts();
        return true;
    }

    public void StopAudio(AudioSource source)
    {
        if (source == null) return;

        if (activeVoiceSources.Contains(source))
        {
            activeVoiceSources.Remove(source);
        } else if (activeSFXSources.Contains(source))
        {
            activeSFXSources.Remove(source);
        }

        sourcePriorities.Remove(source);
        sourceCooldowns.Remove(source);
        originalVolumes.Remove(source);
        sourceTypes.Remove(source);

        source.Stop();
        UpdateActiveCounts();
    }

    public (int used, int max) GetChannelUsage(AudioType type, int priority)
    {
        List<PriorityLevel> levels = type == AudioType.Voice ? voicePriorityLevels : sfxPriorityLevels;

        if (priority >= levels.Count)
        {
            return (0, 0);
        }

        return (levels[priority].currentlyActive, levels[priority].maxChannels);
    }

    // Get total channel usage across all priorities
    public (int totalUsed, int totalMax) GetTotalChannelUsage(AudioType type)
    {
        List<PriorityLevel> levels = type == AudioType.Voice ? voicePriorityLevels : sfxPriorityLevels;

        int totalUsed = 0;
        int totalMax = 0;

        foreach (var level in levels)
        {
            totalUsed += level.currentlyActive;
            totalMax += level.maxChannels;
        }

        return (totalUsed, totalMax);
    }
}