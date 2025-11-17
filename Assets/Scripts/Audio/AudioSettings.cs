using UnityEngine;
using System;

[System.Serializable]
public class AudioSettings : MonoBehaviour
{
    private static AudioSettings instance;
    public static AudioSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AudioSettings>();
                if (instance == null)
                {
                    GameObject go = new GameObject("AudioSettings");
                    instance = go.AddComponent<AudioSettings>();
                }
            }
            return instance;
        }
    }

    [Header("Audio Settings")]
    public bool isMuted = false;
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float voiceVolume = 1f;

    // Events for when settings change
    public static event Action OnAudioSettingsChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved settings
        LoadSettings();
    }

    // Helper method to get the final volume for a type
    public float GetEffectiveVolume(AudioChannelManager.AudioType type)
    {
        if (isMuted) return 0f;

        switch (type)
        {
            case AudioChannelManager.AudioType.Voice:
                return masterVolume * voiceVolume;
            case AudioChannelManager.AudioType.SoundEffect:
                return masterVolume * sfxVolume;
            default:
                return masterVolume;
        }
    }

    // For music (which might not go through the channel manager)
    public float GetMusicVolume()
    {
        return isMuted ? 0f : masterVolume * musicVolume;
    }

    // Methods to update settings
    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        OnSettingsChanged();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        OnSettingsChanged();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        OnSettingsChanged();
    }

    public void SetVoiceVolume(float value)
    {
        voiceVolume = Mathf.Clamp01(value);
        OnSettingsChanged();
    }

    public void SetMuted(bool muted)
    {
        isMuted = muted;
        OnSettingsChanged();
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        OnSettingsChanged();
    }

    private void OnSettingsChanged()
    {
        SaveSettings();
        OnAudioSettingsChanged?.Invoke();
    }

    // Save/Load settings using PlayerPrefs
    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("Audio_MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("Audio_MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("Audio_SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("Audio_VoiceVolume", voiceVolume);
        PlayerPrefs.SetInt("Audio_IsMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("Audio_MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("Audio_MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("Audio_SFXVolume", 1f);
        voiceVolume = PlayerPrefs.GetFloat("Audio_VoiceVolume", 1f);
        isMuted = PlayerPrefs.GetInt("Audio_IsMuted", 0) == 1;
    }
}