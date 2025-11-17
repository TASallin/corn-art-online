using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Menu Toggle")]
    [SerializeField] private Button menuToggleButton;
    [SerializeField] private GameObject audioSettingsPanel;

    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider voiceVolumeSlider;

    [Header("Volume Labels")]
    [SerializeField] private TextMeshProUGUI masterVolumeLabel;
    [SerializeField] private TextMeshProUGUI musicVolumeLabel;
    [SerializeField] private TextMeshProUGUI sfxVolumeLabel;
    [SerializeField] private TextMeshProUGUI voiceVolumeLabel;

    [Header("Mute Button")]
    [SerializeField] private Button muteButton;
    [SerializeField] private Sprite mutedSprite;
    [SerializeField] private Sprite unmutedSprite;

    [Header("UI Settings")]
    [SerializeField] private bool showPercentage = true;
    [SerializeField] private bool startClosed = true;

    [Header("Audio Clips")]
    public AudioClip openClip;
    public AudioClip closeClip;
    public AudioClip muteClip;

    private void Start()
    {
        InitializeUI();
        AddListeners();

        // Set initial panel state
        if (startClosed && audioSettingsPanel != null)
        {
            audioSettingsPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    private void InitializeUI()
    {
        if (AudioSettings.Instance == null)
        {
            Debug.LogError("AudioSettings instance not found!");
            return;
        }

        SetSliderRange(masterVolumeSlider);
        SetSliderRange(musicVolumeSlider);
        SetSliderRange(sfxVolumeSlider);
        SetSliderRange(voiceVolumeSlider);

        UpdateUI();
    }

    private void SetSliderRange(Slider slider)
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
        }
    }

    private void AddListeners()
    {
        // Menu toggle button
        if (menuToggleButton != null)
            menuToggleButton.onClick.AddListener(ToggleMenu);

        // Slider listeners
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        if (voiceVolumeSlider != null)
            voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);

        if (muteButton != null)
            muteButton.onClick.AddListener(OnMuteButtonClicked);
    }

    private void ToggleMenu()
    {
        if (audioSettingsPanel != null)
        {
            audioSettingsPanel.SetActive(!audioSettingsPanel.activeSelf);
            if (audioSettingsPanel.activeSelf)
            {
                UISfx.Instance.PlayUIAudio(openClip);
            } else
            {
                UISfx.Instance.PlayUIAudio(closeClip);
            }
        }
    }

    public void OpenMenu()
    {
        if (audioSettingsPanel != null)
        {
            audioSettingsPanel.SetActive(true);
        }
    }

    public void CloseMenu()
    {
        if (audioSettingsPanel != null)
        {
            audioSettingsPanel.SetActive(false);
        }
    }

    private void UpdateUI()
    {
        if (AudioSettings.Instance == null) return;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = AudioSettings.Instance.masterVolume;
            UpdateVolumeLabel(masterVolumeLabel, AudioSettings.Instance.masterVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = AudioSettings.Instance.musicVolume;
            UpdateVolumeLabel(musicVolumeLabel, AudioSettings.Instance.musicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = AudioSettings.Instance.sfxVolume;
            UpdateVolumeLabel(sfxVolumeLabel, AudioSettings.Instance.sfxVolume);
        }

        if (voiceVolumeSlider != null)
        {
            voiceVolumeSlider.value = AudioSettings.Instance.voiceVolume;
            UpdateVolumeLabel(voiceVolumeLabel, AudioSettings.Instance.voiceVolume);
        }

        UpdateMuteButton();
    }

    private void UpdateVolumeLabel(TextMeshProUGUI label, float value)
    {
        if (label != null)
        {
            if (showPercentage)
            {
                label.text = $"{Mathf.RoundToInt(value * 100)}%";
            } else
            {
                label.text = value.ToString("F2");
            }
        }
    }

    private void UpdateMuteButton()
    {
        if (muteButton != null && AudioSettings.Instance != null)
        {
            muteButton.GetComponent<Image>().sprite = AudioSettings.Instance.isMuted ? mutedSprite : unmutedSprite;
        }
    }

    private void OnMasterVolumeChanged(float value)
    {
        AudioSettings.Instance.SetMasterVolume(value);
        UpdateVolumeLabel(masterVolumeLabel, value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        AudioSettings.Instance.SetMusicVolume(value);
        UpdateVolumeLabel(musicVolumeLabel, value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        AudioSettings.Instance.SetSFXVolume(value);
        UpdateVolumeLabel(sfxVolumeLabel, value);
    }

    private void OnVoiceVolumeChanged(float value)
    {
        AudioSettings.Instance.SetVoiceVolume(value);
        UpdateVolumeLabel(voiceVolumeLabel, value);
    }

    private void OnMuteButtonClicked()
    {
        AudioSettings.Instance.ToggleMute();
        UISfx.Instance.PlayUIAudio(muteClip);
        UpdateMuteButton();
    }

    private void OnDestroy()
    {
        if (menuToggleButton != null)
            menuToggleButton.onClick.RemoveListener(ToggleMenu);

        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);

        if (voiceVolumeSlider != null)
            voiceVolumeSlider.onValueChanged.RemoveListener(OnVoiceVolumeChanged);

        if (muteButton != null)
            muteButton.onClick.RemoveListener(OnMuteButtonClicked);
    }
}