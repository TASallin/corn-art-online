using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SFB; // StandaloneFileBrowser

[System.Serializable]
public class CharacterJsonData
{
    public string name;
    public string twitchUsername;
}

public class NameEntryManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField newNameInput;
    public Transform contentPanel;
    public GameObject nameEntryPrefab;
    public Button addNameButton;
    public Button loadFromTextButton;
    public Button loadFromJsonFolderButton;
    public NumberOfWinnersUI winnersUI;
    public TMP_Text duplicateWarningText;

    [Header("File Loading Settings")]
    public TMP_Dropdown loadingMethodDropdown;  // Optional: dropdown to select method
    public AudioClip openClip;
    public AudioClip errorClip;
    public AudioClip unitClip;

    private List<string> playerNames = new List<string>();
    private List<NameEntryUI> activeEntries = new List<NameEntryUI>();
    private enum LoadingMethod { TextFile, JsonFolder }
    private LoadingMethod currentLoadingMethod = LoadingMethod.TextFile;

    private void Start()
    {
        newNameInput.onSubmit.AddListener(OnNameSubmit);

        if (addNameButton != null)
        {
            addNameButton.onClick.AddListener(AddCurrentName);
        }

        if (loadFromTextButton != null)
        {
            loadFromTextButton.onClick.AddListener(() => LoadNames(LoadingMethod.TextFile));
        }

        if (loadFromJsonFolderButton != null)
        {
            loadFromJsonFolderButton.onClick.AddListener(() => LoadNames(LoadingMethod.JsonFolder));
        }

        if (duplicateWarningText != null)
        {
            duplicateWarningText.gameObject.SetActive(false);
        }

        // Initialize from MenuSettings if it has data
        RefreshFromMenuSettings();
    }

    public void OnEnable()
    {
        // Only refresh if we have no entries but MenuSettings has data
        if (activeEntries.Count == 0 && MenuSettings.Instance.playerNames != null && MenuSettings.Instance.playerNames.Length > 0)
        {
            RefreshFromMenuSettings();
        }
    }

    // Make sure UpdateMenuSettings is called whenever the list changes
    private void UpdateSystem()
    {
        UpdateMenuSettings(); // This now updates MenuSettings properly
        UpdateWinnersLimit();
        CheckForDuplicates();
    }

    private void UpdateMenuSettings()
    {
        MenuSettings.Instance.playerNames = playerNames.ToArray();
        Debug.Log($"Updated MenuSettings with {playerNames.Count} players");
    }

    // Add this method for loading from MenuSettings
    public void RefreshFromMenuSettings()
    {
        Debug.Log($"Refreshing names from MenuSettings. Found {MenuSettings.Instance.playerNames?.Length ?? 0} names");

        // Clear only UI elements without affecting MenuSettings
        foreach (var entry in activeEntries)
        {
            Destroy(entry.gameObject);
        }
        activeEntries.Clear();
        playerNames.Clear();

        // Re-add names from MenuSettings to both UI and local list
        if (MenuSettings.Instance.playerNames != null && MenuSettings.Instance.playerNames.Length > 0)
        {
            // Add all names first without updating the system
            foreach (string name in MenuSettings.Instance.playerNames)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    playerNames.Add(name);

                    GameObject entry = Instantiate(nameEntryPrefab, contentPanel);
                    NameEntryUI entryUI = entry.GetComponent<NameEntryUI>();

                    if (entryUI != null)
                    {
                        entryUI.Initialize(name, this);
                        activeEntries.Add(entryUI);
                    }
                }
            }

            // Now update the system once with all players added
            UpdateSystem();
        }

        Debug.Log($"After refresh: {playerNames.Count} names in list, {activeEntries.Count} UI entries");
    }

    private void OnLoadingMethodChanged(int index)
    {
        currentLoadingMethod = (LoadingMethod)index;
    }

    private void ShowFileDialog()
    {
        string path = "";

        if (currentLoadingMethod == LoadingMethod.TextFile)
        {
            var filter = new[] { new ExtensionFilter("Text Files", "txt") };
            var paths = StandaloneFileBrowser.OpenFilePanel("Select Names File", "", filter, false);
            path = paths.Length > 0 ? paths[0] : "";
        } else
        {
            var paths = StandaloneFileBrowser.OpenFolderPanel("Select Character Folder", "", false);
            path = paths.Length > 0 ? paths[0] : "";
        }

        if (!string.IsNullOrEmpty(path))
        {
            LoadNamesFromPath(path);
        } else
        {
            UISfx.Instance.PlayUIAudio(errorClip);
        }
    }

    private void LoadNames(LoadingMethod method)
    {
        currentLoadingMethod = method;
        UISfx.Instance.PlayUIAudio(openClip);
        ShowFileDialog();
    }

    private void LoadNamesFromPath(string path)
    {
        ClearAllNames();

        if (currentLoadingMethod == LoadingMethod.TextFile)
        {
            LoadFromTextFile(path);
        } else
        {
            LoadFromJsonFolder(path);
        }
    }

    private void LoadFromTextFile(string filePath)
    {
        try
        {
            string[] lines = System.IO.File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine))
                {
                    AddName(trimmedLine);
                }
            }

            Debug.Log($"Loaded {lines.Length} names from text file");
            UISfx.Instance.PlayUIAudio(unitClip);
        } catch (System.Exception e)
        {
            Debug.LogError($"Error loading text file: {e.Message}");
            UISfx.Instance.PlayUIAudio(errorClip);
        }
    }

    private void LoadFromJsonFolder(string folderPath)
    {
        try
        {
            string[] jsonFiles = Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly);

            int namesLoaded = 0;
            foreach (string filePath in jsonFiles)
            {
                string jsonContent = System.IO.File.ReadAllText(filePath);
                CharacterJsonData characterData = JsonUtility.FromJson<CharacterJsonData>(jsonContent);

                if (characterData != null && !string.IsNullOrEmpty(characterData.twitchUsername))
                {
                    AddName(characterData.twitchUsername);
                    namesLoaded++;
                }
            }

            Debug.Log($"Loaded {namesLoaded} names from {jsonFiles.Length} JSON files");
            UISfx.Instance.PlayUIAudio(unitClip);
        } catch (System.Exception e)
        {
            Debug.LogError($"Error loading JSON folder: {e.Message}");
            UISfx.Instance.PlayUIAudio(errorClip);
        }
    }

    public void OnNameSubmit(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            AddName(name);
            UISfx.Instance.PlayUIAudio(unitClip);
            newNameInput.text = "";
            newNameInput.ActivateInputField();
        } else
        {
            UISfx.Instance.PlayUIAudio(errorClip);
        }
    }

    public void AddCurrentName()
    {
        OnNameSubmit(newNameInput.text);
    }

    private void AddName(string name)
    {
        playerNames.Add(name);

        GameObject entry = Instantiate(nameEntryPrefab, contentPanel);
        NameEntryUI entryUI = entry.GetComponent<NameEntryUI>();

        if (entryUI != null)
        {
            entryUI.Initialize(name, this);
            activeEntries.Add(entryUI);
        }

        UpdateSystem();
    }

    public void RemoveEntry(NameEntryUI entryUI)
    {
        int index = activeEntries.IndexOf(entryUI);
        if (index >= 0)
        {
            activeEntries.RemoveAt(index);
            playerNames.RemoveAt(index);
            Destroy(entryUI.gameObject);
            UpdateSystem();
        }
    }

    public void UpdateEntryName(NameEntryUI entryUI, string newName)
    {
        int index = activeEntries.IndexOf(entryUI);
        if (index >= 0)
        {
            playerNames[index] = newName;
            UpdateSystem();
        }
    }

    private void UpdateWinnersLimit()
    {
        if (winnersUI != null)
        {
            int maxWinners = Mathf.Max(1, playerNames.Count - 1);
            winnersUI.SetMaxValue(maxWinners);
        }
    }

    private void CheckForDuplicates()
    {
        // Find duplicate names
        var duplicates = playerNames.GroupBy(x => x)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        // Update each entry's duplicate status
        for (int i = 0; i < playerNames.Count; i++)
        {
            if (i < activeEntries.Count)
            {
                activeEntries[i].SetDuplicateStatus(duplicates.Contains(playerNames[i]));
            }
        }

        // Show/hide warning text
        if (duplicateWarningText != null)
        {
            duplicateWarningText.gameObject.SetActive(duplicates.Count > 0);
        }
    }

    public void ClearAllNames()
    {
        foreach (var entry in activeEntries)
        {
            Destroy(entry.gameObject);
        }
        activeEntries.Clear();
        playerNames.Clear();

        // IMPORTANT: Remove this line to prevent clearing MenuSettings
        // UpdateSystem();
    }

    // Add a separate method to clear both UI and settings
    public void ClearAllNamesAndSettings()
    {
        ClearAllNames();
        UpdateSystem(); // This updates MenuSettings
    }
}