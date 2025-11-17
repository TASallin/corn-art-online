using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class MusicSettingsUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Dropdown battleMusicDropdown;
    [SerializeField] private TMP_Dropdown menuMusicDropdown;
    public AudioClip selectClip;

    private void Start()
    {
        SetupDropdowns();

        // Add listeners
        battleMusicDropdown.onValueChanged.AddListener(OnBattleMusicChanged);
        menuMusicDropdown.onValueChanged.AddListener(OnMenuMusicChanged);
    }

    private void SetupDropdowns()
    {
        if (MusicManager.Instance == null) return;

        // Setup Battle Music Dropdown
        List<string> battleOptions = MusicManager.Instance.GetBattleTrackNames();
        battleMusicDropdown.ClearOptions();
        battleMusicDropdown.AddOptions(battleOptions);

        // Set current selection from MenuSettings or MusicManager
        string currentBattle = MenuSettings.Instance != null ?
            MenuSettings.Instance.selectedBattleMusic :
            MusicManager.Instance.GetCurrentBattleTrack();

        int battleIndex = battleOptions.IndexOf(currentBattle);
        if (battleIndex >= 0)
        {
            battleMusicDropdown.value = battleIndex;
        }

        // Setup Menu Music Dropdown
        List<string> menuOptions = MusicManager.Instance.GetMenuTrackNames();
        menuMusicDropdown.ClearOptions();
        menuMusicDropdown.AddOptions(menuOptions);

        // Set current selection from MenuSettings or MusicManager
        string currentMenu = MenuSettings.Instance != null ?
            MenuSettings.Instance.selectedMenuMusic :
            MusicManager.Instance.GetCurrentMenuTrack();

        int menuIndex = menuOptions.IndexOf(currentMenu);
        if (menuIndex >= 0)
        {
            menuMusicDropdown.value = menuIndex;
        }
    }

    // Public method to refresh dropdowns after loading a save
    public void RefreshDropdowns()
    {
        if (MusicManager.Instance == null || MenuSettings.Instance == null) return;

        // Temporarily remove listeners to prevent triggering changes
        battleMusicDropdown.onValueChanged.RemoveListener(OnBattleMusicChanged);
        menuMusicDropdown.onValueChanged.RemoveListener(OnMenuMusicChanged);

        // Update battle music dropdown
        if (battleMusicDropdown != null)
        {
            string currentBattle = MenuSettings.Instance.selectedBattleMusic;
            for (int i = 0; i < battleMusicDropdown.options.Count; i++)
            {
                if (battleMusicDropdown.options[i].text == currentBattle)
                {
                    battleMusicDropdown.value = i;
                    break;
                }
            }
        }

        // Update menu music dropdown
        if (menuMusicDropdown != null)
        {
            string currentMenu = MenuSettings.Instance.selectedMenuMusic;
            for (int i = 0; i < menuMusicDropdown.options.Count; i++)
            {
                if (menuMusicDropdown.options[i].text == currentMenu)
                {
                    menuMusicDropdown.value = i;
                    break;
                }
            }
        }

        // Re-add listeners
        battleMusicDropdown.onValueChanged.AddListener(OnBattleMusicChanged);
        menuMusicDropdown.onValueChanged.AddListener(OnMenuMusicChanged);
    }

    public void RefreshGlobalDropdowns()
    {
        if (MusicManager.Instance == null || MenuSettings.Instance == null) return;

        // Temporarily remove listeners to prevent triggering changes
        battleMusicDropdown.onValueChanged.RemoveListener(OnBattleMusicChanged);
        menuMusicDropdown.onValueChanged.RemoveListener(OnMenuMusicChanged);

        // Update battle music dropdown
        if (battleMusicDropdown != null)
        {
            string currentBattle = MusicManager.Instance.GetCurrentBattleTrack();
            for (int i = 0; i < battleMusicDropdown.options.Count; i++)
            {
                if (battleMusicDropdown.options[i].text == currentBattle)
                {
                    battleMusicDropdown.value = i;
                    break;
                }
            }
        }

        // Update menu music dropdown
        if (menuMusicDropdown != null)
        {
            string currentMenu = MusicManager.Instance.GetCurrentMenuTrack();
            for (int i = 0; i < menuMusicDropdown.options.Count; i++)
            {
                if (menuMusicDropdown.options[i].text == currentMenu)
                {
                    menuMusicDropdown.value = i;
                    break;
                }
            }
        }

        // Re-add listeners
        battleMusicDropdown.onValueChanged.AddListener(OnBattleMusicChanged);
        menuMusicDropdown.onValueChanged.AddListener(OnMenuMusicChanged);
    }

    private void OnBattleMusicChanged(int index)
    {
        string selectedTrack = battleMusicDropdown.options[index].text;
        UISfx.Instance.PlayUIAudio(selectClip);

        // Update both MusicManager and MenuSettings
        if (MenuSettings.Instance != null)
        {
            MenuSettings.Instance.SetBattleMusic(selectedTrack);
        } else
        {
            MusicManager.Instance.SetBattleTrack(selectedTrack);
        }

        // Save global preferences
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGlobalPreferences();
        }
    }

    private void OnMenuMusicChanged(int index)
    {
        string selectedTrack = menuMusicDropdown.options[index].text;
        UISfx.Instance.PlayUIAudio(selectClip);

        // Update both MusicManager and MenuSettings
        if (MenuSettings.Instance != null)
        {
            MenuSettings.Instance.SetMenuMusic(selectedTrack);
        } else
        {
            MusicManager.Instance.SetMenuTrack(selectedTrack);
        }

        // Save global preferences
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGlobalPreferences();
        }
    }

    private void OnDestroy()
    {
        battleMusicDropdown.onValueChanged.RemoveListener(OnBattleMusicChanged);
        menuMusicDropdown.onValueChanged.RemoveListener(OnMenuMusicChanged);
    }
}