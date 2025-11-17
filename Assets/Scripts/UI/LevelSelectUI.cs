using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class LevelSelectUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown levelDropdown;
    public Image chapterBackground;
    public AudioClip selectClip;

    private List<string> levelNames = new List<string>();

    private void Start()
    {
        InitializeLevelDropdown();
    }

    private void InitializeLevelDropdown()
    {
        if (levelDropdown == null)
        {
            Debug.LogError("Level dropdown reference is missing!");
            return;
        }

        // Get available levels from TeamClassComposition
        levelNames = TeamClassComposition.Instance.GetAvailableCompositions();

        // Clear existing options
        levelDropdown.ClearOptions();

        // Add the level names as options
        levelDropdown.AddOptions(levelNames);

        // Set up the dropdown event listener
        levelDropdown.onValueChanged.AddListener(OnLevelSelected);

        // Select the first level by default if available
        if (levelNames.Count > 0)
        {
            if (String.IsNullOrEmpty(MenuSettings.Instance.selectedLevel))
            {
                levelDropdown.value = 0;
                OnLevelSelected(0);
            } else
            {
                SelectLevel(MenuSettings.Instance.selectedLevel);
            }
        }

        Debug.Log($"Loaded {levelNames.Count} levels into dropdown");
    }

    public void OnLevelSelected(int index)
    {
        if (index >= 0 && index < levelNames.Count)
        {
            string selectedLevel = levelNames[index];
            MenuSettings.Instance.SetSelectedLevel(selectedLevel);
            chapterBackground.sprite = CharacterAssetLoader.Instance.LoadChapterBackground(selectedLevel);
        }
    }

    public void OnLevelClicked(int index)
    {
        UISfx.Instance.PlayUIAudio(selectClip);
        OnLevelSelected(index);
    }

    // Optional: If you want to set the dropdown to a specific level programmatically
    public void SelectLevel(string levelName)
    {
        int index = levelNames.IndexOf(levelName);
        if (index >= 0)
        {
            levelDropdown.value = index;
            OnLevelSelected(index);
        }
    }
}