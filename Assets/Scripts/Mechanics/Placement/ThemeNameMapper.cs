using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ThemeNamingManager : MonoBehaviour
{
    private const string THEME_NAMES_DATA_PATH = "Text/ThemeNames";
    private const string ENEMY_THEME_NAMES_DATA_PATH = "Text/EnemyThemeNames";

    [System.Serializable]
    public class ThemeNameData
    {
        public string rawName;
        public string themeName;
        public bool addSuffix;
    }

    // Dictionary for quick lookup
    private Dictionary<string, ThemeNameData> themeNamesLookup = new Dictionary<string, ThemeNameData>();
    private Dictionary<string, ThemeNameData> enemyThemeNamesLookup = new Dictionary<string, ThemeNameData>();

    // Alliterative suffixes for each letter
    private Dictionary<char, List<string>> alliterativeSuffixes = new Dictionary<char, List<string>>()
    {
        {'A', new List<string> {"Arena", "Assault", "Attack", "Among Us"}},
        {'B', new List<string> {"Brawl", "Battle"}},
        {'C', new List<string> {"Clash"}},
        {'D', new List<string> {"Duel", "Dance"}},
        {'E', new List<string> {"Elites", "Empire", "Expired Soda"}},
        {'F', new List<string> {"Fight", "Feud", "Free for All", "Frenzy", "Fortnite"}},
        {'G', new List<string> {"Gamba"}},
        {'H', new List<string> {"Hunt"}},
        {'I', new List<string> {"Invasion"}},
        {'J', new List<string> {"Joust", "Jamboree", "Jobbers"}},
        {'K', new List<string> {"Kombat"}},
        {'L', new List<string> {"Lunge", "Life and Death"}},
        {'M', new List<string> {"Melee", "Morbin'"}},
        {'N', new List<string> {"Noose Take"}},
        {'O', new List<string> {"Operation"}},
        {'P', new List<string> {"Pursuit"}},
        {'Q', new List<string> {"Quarrel"}},
        {'R', new List<string> {"Route", "Royale"}},
        {'S', new List<string> {"Scuffle", "Skirmish", "Siege"}},
        {'T', new List<string> {"Tournament", "Tussle"}},
        {'U', new List<string> {"Ultimates"}},
        {'V', new List<string> {"Violence", "Vengeance"}},
        {'W', new List<string> {"War"}},
        {'X', new List<string> {"X", "Xenosaga"}},
        {'Y', new List<string> {"Yolo"}},
        {'Z', new List<string> {"Z"}}
    };

    // Singleton pattern
    private static ThemeNamingManager instance;
    public static ThemeNamingManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("ThemeNamingManager");
                instance = go.AddComponent<ThemeNamingManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        LoadThemeNames();
    }

    private void LoadThemeNames()
    {
        TextAsset csvFile = Resources.Load<TextAsset>(THEME_NAMES_DATA_PATH);

        if (csvFile == null)
        {
            Debug.LogWarning($"Theme names CSV not found at path: {THEME_NAMES_DATA_PATH}. Using default naming.");
            return;
        }

        string[] lines = csvFile.text.Split('\n');

        // Skip header row
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = ParseCSVLine(lines[i]);

            if (values.Length >= 3)
            {
                ThemeNameData data = new ThemeNameData
                {
                    rawName = values[0].Trim(),
                    themeName = values[1].Trim(),
                    addSuffix = values[2].Trim().ToUpper() == "TRUE"
                };

                themeNamesLookup[data.rawName.ToLower()] = data;
            }
        }

        csvFile = Resources.Load<TextAsset>(ENEMY_THEME_NAMES_DATA_PATH);
        lines = csvFile.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = ParseCSVLine(lines[i]);

            if (values.Length >= 3)
            {
                ThemeNameData data = new ThemeNameData
                {
                    rawName = values[0].Trim(),
                    themeName = values[1].Trim(),
                    addSuffix = values[2].Trim().ToUpper() == "TRUE"
                };

                enemyThemeNamesLookup[data.rawName.ToLower()] = data;
            }
        }
    }

    private string[] ParseCSVLine(string csvLine)
    {
        List<string> fields = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < csvLine.Length; i++)
        {
            char c = csvLine[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            } else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField);
                currentField = "";
            } else
            {
                currentField += c;
            }
        }

        fields.Add(currentField);
        return fields.ToArray();
    }

    /// <summary>
    /// Convert a raw theme name to a display name with optional suffix
    /// </summary>
    public string GetThemeName(string rawName)
    {
        string lookupKey = rawName.ToLower();

        if (themeNamesLookup.TryGetValue(lookupKey, out ThemeNameData themeData))
        {
            // Found in CSV
            if (themeData.addSuffix)
            {
                return AddAlliterativeSuffix(themeData.themeName);
            } else
            {
                return themeData.themeName;
            }
        } else
        {
            // Not in CSV, treat as add suffix = true
            return AddAlliterativeSuffix(rawName);
        }
    }

    public string GetEnemyThemeName(string rawName)
    {
        string lookupKey = rawName.ToLower();
        if (enemyThemeNamesLookup.TryGetValue(lookupKey, out ThemeNameData themeData))
        {
            return themeData.themeName;
        }
        else
        {
            // Not in CSV, use automatic enemy boss name
            return "";
        }
    }

    /// <summary>
    /// Add an alliterative suffix based on the first letter of the word
    /// </summary>
    private string AddAlliterativeSuffix(string baseName)
    {
        if (string.IsNullOrEmpty(baseName)) return baseName;

        char firstLetter = char.ToUpper(baseName[0]);

        if (alliterativeSuffixes.TryGetValue(firstLetter, out List<string> suffixes))
        {
            // Use game RNG for consistency
            System.Random rng = GameManager.GetInstance().rng;
            string suffix = suffixes[rng.Next(suffixes.Count)];
            return $"{baseName} {suffix}";
        }

        // Fallback if no suffix found for that letter
        return $"{baseName} Battle";
    }
}