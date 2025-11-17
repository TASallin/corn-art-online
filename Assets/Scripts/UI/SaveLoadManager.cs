using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

public class SaveLoadManager : MonoBehaviour
{
    private static SaveLoadManager _instance;
    public static SaveLoadManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SaveLoadManager");
                _instance = go.AddComponent<SaveLoadManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public const int MAX_SAVE_SLOTS = 5;

    // Separate file names for different modes
    private const string RECRUIT_AUTO_SAVE_FILENAME = "autosave.json";
    private const string RECRUIT_SAVE_FILE_PREFIX = "gamesave_";

    private const string WHEEL_AUTO_SAVE_FILENAME = "wheel_autosave.json";
    private const string WHEEL_SAVE_FILE_PREFIX = "wheel_gamesave_";

    private const string SAVE_FILE_EXTENSION = ".json";

    // Global preferences file
    private const string GLOBAL_PREFS_FILENAME = "global_preferences.json";

    [Serializable]
    public class GameSaveData
    {
        public string saveDateTime;
        public string streamMode; // "Recruit" or "WheelOfDeath"
        public string selectedLevel;
        public string selectedGameMode;
        public string levelMusic; // The level-specific music
        public string selectedBattleMusic; // User's battle music preference
        public string selectedMenuMusic; // User's menu music preference
        public int numberOfWinners;
        public int team1Count;
        public int team2Count;
        public bool randomPlayerTeam = true; // New field with default
        public bool corrinPlayerTeam = false;
        public bool randomWinConditions = false;
        public bool randomEnemyTeam = false;
        public string[] playerNames;
    }

    [Serializable]
    public class GlobalPreferences
    {
        public string menuMusic = "random";
        public int cameraMode = 0; // 0 = Fixed, 1 = Dynamic, 2 = Manual
        public string battleMusic = "default"; // Default battle music preference
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Load global preferences on startup
        LoadGlobalPreferences();
    }

    public void AutoSave()
    {
        GameSaveData saveData = CreateSaveDataFromCurrentSettings();
        string filePath = GetSaveFilePath(true, 0);
        SaveToFile(saveData, filePath);
        Debug.Log($"Autosave completed for {MenuSettings.Instance.streamMode} mode");
    }

    public void SaveGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SAVE_SLOTS)
        {
            Debug.LogError($"Invalid save slot index: {slotIndex}");
            return;
        }

        GameSaveData saveData = CreateSaveDataFromCurrentSettings();
        string filePath = GetSaveFilePath(false, slotIndex);
        SaveToFile(saveData, filePath);
        Debug.Log($"Game saved to {MenuSettings.Instance.streamMode} slot {slotIndex}");
    }

    public bool LoadGame(int slotIndex, bool isAutoSave = false)
    {
        string filePath = GetSaveFilePath(isAutoSave, slotIndex);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"Save file does not exist: {filePath}");
            return false;
        }

        try
        {
            string jsonData = File.ReadAllText(filePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(jsonData);
            ApplySaveDataToSettings(saveData);
            Debug.Log($"Game loaded from {(isAutoSave ? "autosave" : $"slot {slotIndex}")} - Mode: {saveData.streamMode}");
            return true;
        } catch (Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            return false;
        }
    }

    public void SaveGlobalPreferences()
    {
        GlobalPreferences prefs = new GlobalPreferences();

        // Get current music preferences from MusicManager
        if (MusicManager.Instance != null)
        {
            prefs.menuMusic = MusicManager.Instance.GetCurrentMenuTrack();
            prefs.battleMusic = MusicManager.Instance.GetCurrentBattleTrack();
        }

        // Get camera mode from PlayerPrefs (set by CameraModeUI)
        prefs.cameraMode = PlayerPrefs.GetInt("PreferredCameraMode", 0);

        string filePath = Path.Combine(Application.persistentDataPath, GLOBAL_PREFS_FILENAME);
        string jsonData = JsonUtility.ToJson(prefs, true);
        File.WriteAllText(filePath, jsonData);

        Debug.Log("Global preferences saved");
    }

    public void LoadGlobalPreferences()
    {
        string filePath = Path.Combine(Application.persistentDataPath, GLOBAL_PREFS_FILENAME);

        if (!File.Exists(filePath))
        {
            Debug.Log("No global preferences file found, using defaults");
            return;
        }

        try
        {
            string jsonData = File.ReadAllText(filePath);
            GlobalPreferences prefs = JsonUtility.FromJson<GlobalPreferences>(jsonData);

            // Apply music preferences
            if (MenuSettings.Instance != null)
            {
                MenuSettings.Instance.selectedMenuMusic = prefs.menuMusic;
                MenuSettings.Instance.selectedBattleMusic = prefs.battleMusic;
            }

            // Apply to MusicManager if it exists
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.SetMenuTrack(prefs.menuMusic);
                MusicManager.Instance.SetBattleTrack(prefs.battleMusic);
            }

            // Save camera preference for later use
            PlayerPrefs.SetInt("PreferredCameraMode", prefs.cameraMode);
            PlayerPrefs.Save();

            Debug.Log("Global preferences loaded");
        } catch (Exception e)
        {
            Debug.LogError($"Failed to load global preferences: {e.Message}");
        }
    }

    public bool SaveSlotExists(int slotIndex)
    {
        string filePath = GetSaveFilePath(false, slotIndex);
        return File.Exists(filePath);
    }

    public bool AutoSaveExists()
    {
        string filePath = GetSaveFilePath(true, 0);
        return File.Exists(filePath);
    }

    public GameSaveData GetSaveInfo(int slotIndex, bool isAutoSave = false)
    {
        string filePath = GetSaveFilePath(isAutoSave, slotIndex);

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            string jsonData = File.ReadAllText(filePath);
            return JsonUtility.FromJson<GameSaveData>(jsonData);
        } catch
        {
            return null;
        }
    }

    // Get all saves for the current stream mode
    public List<GameSaveData> GetAllSavesForCurrentMode()
    {
        List<GameSaveData> saves = new List<GameSaveData>();

        // Check autosave
        if (AutoSaveExists())
        {
            GameSaveData autoSave = GetSaveInfo(0, true);
            if (autoSave != null && autoSave.streamMode == MenuSettings.Instance.streamMode)
            {
                saves.Add(autoSave);
            }
        }

        // Check regular save slots
        for (int i = 0; i < MAX_SAVE_SLOTS; i++)
        {
            if (SaveSlotExists(i))
            {
                GameSaveData save = GetSaveInfo(i, false);
                if (save != null && save.streamMode == MenuSettings.Instance.streamMode)
                {
                    saves.Add(save);
                }
            }
        }

        return saves;
    }

    private GameSaveData CreateSaveDataFromCurrentSettings()
    {
        MenuSettings settings = MenuSettings.Instance;

        GameSaveData saveData = new GameSaveData
        {
            saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            streamMode = settings.streamMode,
            selectedLevel = settings.selectedLevel,
            selectedGameMode = settings.selectedGameMode,
            levelMusic = settings.levelMusic,
            selectedBattleMusic = settings.selectedBattleMusic,
            selectedMenuMusic = settings.selectedMenuMusic,
            numberOfWinners = settings.numberOfWinners,
            team1Count = settings.team1Count,
            team2Count = settings.team2Count,
            randomPlayerTeam = settings.randomPlayerTeam,
            corrinPlayerTeam = settings.corrinPlayerTeam,
            randomWinConditions = settings.randomWinConditions,
            randomEnemyTeam = settings.randomEnemyTeam,
            playerNames = settings.playerNames
        };
        return saveData;
    }

    // Update ApplySaveDataToSettings method:
    private void ApplySaveDataToSettings(GameSaveData saveData)
    {
        MenuSettings settings = MenuSettings.Instance;

        // Load basic settings first
        settings.streamMode = saveData.streamMode ?? "Recruit";
        settings.selectedLevel = saveData.selectedLevel;
        settings.selectedGameMode = saveData.selectedGameMode;

        // Handle music settings with backwards compatibility
        if (!string.IsNullOrEmpty(saveData.levelMusic))
        {
            settings.levelMusic = saveData.levelMusic;
        } else if (!string.IsNullOrEmpty(saveData.selectedMenuMusic)) // Old field name fallback
        {
            settings.levelMusic = saveData.selectedMenuMusic;
        }

        settings.selectedBattleMusic = string.IsNullOrEmpty(saveData.selectedBattleMusic) ? "default" : saveData.selectedBattleMusic;
        settings.selectedMenuMusic = string.IsNullOrEmpty(saveData.selectedMenuMusic) ? "random" : saveData.selectedMenuMusic;

        settings.team1Count = saveData.team1Count;
        settings.team2Count = saveData.team2Count;
        settings.randomPlayerTeam = saveData.randomPlayerTeam;
        settings.corrinPlayerTeam = saveData.corrinPlayerTeam;
        settings.randomWinConditions = saveData.randomWinConditions;
        settings.randomEnemyTeam = saveData.randomEnemyTeam;
        settings.playerNames = saveData.playerNames;

        // Apply the winners value
        settings.numberOfWinners = saveData.numberOfWinners;

        // Calculate deaths from winners if in Wheel of Death mode
        if (settings.IsWheelOfDeathMode() && settings.playerNames != null)
        {
            settings.numberOfDeaths = Mathf.Max(1, settings.playerNames.Length - settings.numberOfWinners);
        }

        // Update MusicManager with loaded settings
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetBattleTrack(settings.selectedBattleMusic);
            MusicManager.Instance.SetMenuTrack(settings.selectedMenuMusic);
        }

        Debug.Log($"Loaded save: Winners={settings.numberOfWinners}, Deaths={settings.numberOfDeaths}, Mode={settings.streamMode}, RandomTeam={settings.randomPlayerTeam}");
        Debug.Log($"Music settings: Level={settings.levelMusic}, Battle={settings.selectedBattleMusic}, Menu={settings.selectedMenuMusic}");
    }

    private string GetSaveFilePath(bool isAutoSave, int slotIndex)
    {
        string fileName;

        // Determine filename based on current stream mode
        if (MenuSettings.Instance.IsWheelOfDeathMode())
        {
            fileName = isAutoSave
                ? WHEEL_AUTO_SAVE_FILENAME
                : WHEEL_SAVE_FILE_PREFIX + slotIndex + SAVE_FILE_EXTENSION;
        } else
        {
            // Default to Recruit mode files
            fileName = isAutoSave
                ? RECRUIT_AUTO_SAVE_FILENAME
                : RECRUIT_SAVE_FILE_PREFIX + slotIndex + SAVE_FILE_EXTENSION;
        }

        return Path.Combine(Application.persistentDataPath, fileName);
    }

    private void SaveToFile(GameSaveData saveData, string filePath)
    {
        string jsonData = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(filePath, jsonData);
    }

    public void DeleteSave(int slotIndex, bool isAutoSave = false)
    {
        string filePath = GetSaveFilePath(isAutoSave, slotIndex);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"Deleted save file: {filePath}");
        }
    }

    // Utility method to check if a save belongs to current stream mode
    public bool IsSaveForCurrentMode(int slotIndex, bool isAutoSave = false)
    {
        GameSaveData saveData = GetSaveInfo(slotIndex, isAutoSave);
        if (saveData == null) return false;

        return saveData.streamMode == MenuSettings.Instance.streamMode;
    }

    // Update MigrateOldSaves method to handle the new field:
    public void MigrateOldSaves()
    {
        string[] allFiles = Directory.GetFiles(Application.persistentDataPath, "*.json");

        foreach (string filePath in allFiles)
        {
            try
            {
                string jsonData = File.ReadAllText(filePath);
                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(jsonData);
                bool needsSave = false;

                // If streamMode is null, it's an old save
                if (string.IsNullOrEmpty(saveData.streamMode))
                {
                    saveData.streamMode = "Recruit";
                    needsSave = true;
                }

                // If music fields are missing, set defaults
                if (string.IsNullOrEmpty(saveData.selectedBattleMusic))
                {
                    saveData.selectedBattleMusic = "default";
                    needsSave = true;
                }

                if (string.IsNullOrEmpty(saveData.selectedMenuMusic))
                {
                    saveData.selectedMenuMusic = "random";
                    needsSave = true;
                }

                // Note: randomPlayerTeam will default to true for old saves due to the field default value
                // No explicit migration needed for boolean fields with defaults

                if (needsSave)
                {
                    SaveToFile(saveData, filePath);
                    Debug.Log($"Migrated save file: {Path.GetFileName(filePath)}");
                }
            } catch (Exception e)
            {
                Debug.LogWarning($"Failed to check/migrate save file {Path.GetFileName(filePath)}: {e.Message}");
            }
        }
    }

    private void OnApplicationQuit()
    {
        // Save global preferences
        SaveGlobalPreferences();

        // Create autosave when application is closed
        AutoSave();
        Debug.Log($"Saved global preferences and created {MenuSettings.Instance.streamMode} autosave on application quit");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Save when app is paused (mobile)
            SaveGlobalPreferences();
            AutoSave();
        }
    }
}