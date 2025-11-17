using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISfx : MonoBehaviour
{
    public AudioSource sfxSource;

    private static UISfx instance;
    public static UISfx Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UISfx>();
                if (instance == null)
                {
                    GameObject go = new GameObject("UISfx");
                    instance = go.AddComponent<UISfx>();
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

            // Subscribe to audio settings changes
            AudioSettings.OnAudioSettingsChanged += ApplyAudioSettings;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void ApplyAudioSettings()
    {
        if (AudioSettings.Instance == null) return;

        float effectiveVolume = AudioSettings.Instance.GetEffectiveVolume(AudioChannelManager.AudioType.SoundEffect);
        sfxSource.volume = effectiveVolume;
    }

    public void PlayUIAudio(AudioClip clip)
    {
        sfxSource.clip = clip;
        sfxSource.Play();
    }
}
