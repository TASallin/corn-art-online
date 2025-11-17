using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SetupMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text titleText;
    public TMP_Text numberOfWinnersLabelText; // The label that says "Number of Winners" or "Number of Deaths"
    public GameObject randomizerObject;

    [Header("Title Settings")]
    public string recruitModeTitle = "Recruit Mode";
    public string wheelOfDeathTitle = "Wheel of Death";

    [Header("Label Settings")]
    public string recruitModeLabel = "Number of Winners";
    public string wheelOfDeathLabel = "Number of Deaths";

    private void Start()
    {
        UpdateUIBasedOnStreamMode();
    }

    private void OnEnable()
    {
        // Also update when the menu is re-enabled (e.g., after loading)
        UpdateUIBasedOnStreamMode();

        // Refresh music dropdowns after a short delay to ensure everything is loaded
        StartCoroutine(RefreshMusicDropdownsDelayed());
    }

    private void UpdateUIBasedOnStreamMode()
    {
        MenuSettings settings = MenuSettings.Instance;

        if (settings.IsWheelOfDeathMode())
        {
            if (titleText != null)
                titleText.text = wheelOfDeathTitle;

            if (numberOfWinnersLabelText != null)
                numberOfWinnersLabelText.text = wheelOfDeathLabel;
            randomizerObject.SetActive(false);
        } else // Default to Recruit mode
        {
            if (titleText != null)
                titleText.text = recruitModeTitle;

            if (numberOfWinnersLabelText != null)
                numberOfWinnersLabelText.text = recruitModeLabel;
        }

        Debug.Log($"Setup Menu loaded - Stream Mode: {settings.streamMode}, Game Mode will be: {settings.selectedGameMode}");
    }

    private IEnumerator RefreshMusicDropdownsDelayed()
    {
        // Wait a frame to ensure everything is initialized
        yield return null;

        RefreshMusicDropdowns();
    }

    public void RefreshMusicDropdowns()
    {
        // Find and update MusicSettingsUI dropdowns
        MusicSettingsUI musicUI = FindObjectOfType<MusicSettingsUI>();
        if (musicUI != null)
        {
            musicUI.RefreshGlobalDropdowns();
            Debug.Log("Music dropdowns refreshed");
        }
    }

    // Call this method after loading a save
    public void OnSaveLoaded()
    {
        UpdateUIBasedOnStreamMode();
        RefreshMusicDropdowns();
    }
}