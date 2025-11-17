using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CharacterAssetLoader : MonoBehaviour
{
    // File paths as constants
    private const string CHARACTER_DATA_PATH = "Text/CharacterData";
    private const string SPRITES_BASE_PATH = "Sprites/Portraits/";
    private const string CORRIN_SPRITES_BASE_PATH = "Corrin/";
    private const string AUDIO_BASE_PATH = "Audio/Voice/";
    private const string BACKGROUND_BASE_PATH = "Sprites/Backgrounds/";
    private const string SFX_BASE_PATH = "Audio/SFX";

    // Sprite suffixes
    private const string NEUTRAL_SUFFIX = " Neutral";
    private const string DAMAGE_SUFFIX = " Damage";
    private const string CRIT_SUFFIX = " Crit";

    // Audio suffixes (placeholder for now)
    private const string ATTACK_AUDIO_SUFFIX = " Attack";
    private const string HURT_AUDIO_SUFFIX = " Damage";
    private const string DEAD_AUDIO_SUFFIX = " Dead";
    private const string CRIT_AUDIO_SUFFIX = " Crit";
    private const string VICTORY_AUDIO_SUFFIX = " Win";

    private const string BACKGROUND_SUFFIX = " Map";

    [System.Serializable]
    public class CharacterData
    {
        public string name;
        public string className;
        public string portraitPrefix;
        public string audioPrefix;
        public bool playable;
        public bool unique;
        public List<string> flags;  // New field for flags

        public CharacterData()
        {
            flags = new List<string>();
        }
    }

    // Data structures for caching
    private Dictionary<string, CharacterData> characterDataDict = new Dictionary<string, CharacterData>();
    private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();

    // Singleton pattern
    private static CharacterAssetLoader instance;
    public static CharacterAssetLoader Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CharacterAssetLoader>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CharacterAssetLoader");
                    instance = go.AddComponent<CharacterAssetLoader>();
                }
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

        LoadCharacterData();
    }

    private void LoadCharacterData()
    {
        TextAsset csvFile = Resources.Load<TextAsset>(CHARACTER_DATA_PATH);

        if (csvFile == null)
        {
            Debug.LogError($"Character data CSV not found at path: {CHARACTER_DATA_PATH}");
            return;
        }

        string[] lines = csvFile.text.Split('\n');

        // Skip header row
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            // Use proper CSV parsing that handles quoted fields
            string[] values = ParseCSVLine(lines[i]);

            if (values.Length >= 7) // Updated for flags column
            {
                bool isPlayable = values.Length > 4 && !string.IsNullOrEmpty(values[4]) && values[4].Trim().ToLower() == "true";
                bool isUnique = values.Length > 5 && !string.IsNullOrEmpty(values[5]) && values[5].Trim().ToLower() == "true";

                CharacterData data = new CharacterData
                {
                    name = values[0].Trim(),
                    className = values[1].Trim(),
                    portraitPrefix = values[2].Trim(),
                    audioPrefix = values[3].Trim(),
                    playable = isPlayable,
                    unique = isUnique
                };

                // Parse flags if present
                if (values.Length > 6 && !string.IsNullOrEmpty(values[6]))
                {
                    // The flags field might be quoted and contain commas
                    string flagsField = values[6].Trim();

                    // Remove surrounding quotes if present
                    if (flagsField.StartsWith("\"") && flagsField.EndsWith("\""))
                    {
                        flagsField = flagsField.Substring(1, flagsField.Length - 2);
                    }

                    // Split by comma and trim each flag
                    string[] flagArray = flagsField.Split(',');
                    foreach (string flag in flagArray)
                    {
                        string trimmedFlag = flag.Trim();
                        if (!string.IsNullOrEmpty(trimmedFlag))
                        {
                            data.flags.Add(trimmedFlag);
                        }
                    }
                }

                characterDataDict[data.name.ToLower()] = data;
            } else if (values.Length >= 6) // Backwards compatibility without flags
            {
                bool isPlayable = values.Length > 4 && !string.IsNullOrEmpty(values[4]) && values[4].Trim().ToLower() == "true";
                bool isUnique = values.Length > 5 && !string.IsNullOrEmpty(values[5]) && values[5].Trim().ToLower() == "true";

                CharacterData data = new CharacterData
                {
                    name = values[0].Trim(),
                    className = values[1].Trim(),
                    portraitPrefix = values[2].Trim(),
                    audioPrefix = values[3].Trim(),
                    playable = isPlayable,
                    unique = isUnique
                };

                characterDataDict[data.name.ToLower()] = data;
            } else if (values.Length >= 4) // Even more backwards compatibility
            {
                CharacterData data = new CharacterData
                {
                    name = values[0].Trim(),
                    className = values[1].Trim(),
                    portraitPrefix = values[2].Trim(),
                    audioPrefix = values[3].Trim(),
                    playable = false,
                    unique = false
                };

                characterDataDict[data.name.ToLower()] = data;
            }
        }

        Debug.Log($"Loaded {characterDataDict.Count} characters from CSV");
    }

    // Add a proper CSV line parser that handles quoted fields
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

        // Don't forget the last field
        fields.Add(currentField);

        return fields.ToArray();
    }

    // Main method to set up both sprites and audio
    public void SetupCharacterAssets(GameObject unitPrefab, string characterName)
    {
        if (characterName == "Corrin" || characterName == "CorrinF" || characterName == "CorrinM")
        {
            BuildCorrinAssets(unitPrefab);
        } else
        {
            SetupCharacterSprites(unitPrefab, characterName);
            LoadCharacterAudio(unitPrefab, characterName);
        }
    }

    // Sprite setup
    public void SetupCharacterSprites(GameObject unitPrefab, string characterName)
    {
        UnitSprite unitSprite = unitPrefab.GetComponent<UnitSprite>();
        if (unitSprite == null)
        {
            Debug.LogError("UnitSprite component not found on prefab!");
            return;
        }

        if (!characterDataDict.TryGetValue(characterName.ToLower(), out CharacterData characterData))
        {
            Debug.Log($"Character {characterName} not found in CSV, keeping default sprites");
            return;
        }

        // Load or retrieve cached sprites using portraitPrefix
        unitSprite.neutralPortrait.sprite = GetOrLoadSprite(characterData.portraitPrefix + NEUTRAL_SUFFIX);
        unitSprite.damagePortrait.sprite = GetOrLoadSprite(characterData.portraitPrefix + DAMAGE_SUFFIX);
        unitSprite.critPortrait.sprite = GetOrLoadSprite(characterData.portraitPrefix + CRIT_SUFFIX);
        if (unitSprite.damagePortrait.sprite == null)
        {
            unitSprite.damagePortrait.sprite = unitSprite.neutralPortrait.sprite;
        }
        if (unitSprite.critPortrait.sprite == null)
        {
            unitSprite.critPortrait.sprite = unitSprite.neutralPortrait.sprite;
        }
    }

    // Audio loading
    public void LoadCharacterAudio(GameObject unitPrefab, string characterName)
    {
        UnitSprite unitSprite = unitPrefab.GetComponent<UnitSprite>();
        if (unitSprite == null)
        {
            Debug.LogError("UnitSprite component not found on prefab!");
            return;
        }

        if (!characterDataDict.TryGetValue(characterName.ToLower(), out CharacterData characterData))
        {
            Debug.Log($"Character {characterName} not found in CSV, skipping audio load");
            return;
        }

        // Load or retrieve cached audio using audioPrefix
        AudioClip attackClip = GetOrLoadAudio(characterData.audioPrefix + ATTACK_AUDIO_SUFFIX);
        AudioClip hurtClip = GetOrLoadAudio(characterData.audioPrefix + HURT_AUDIO_SUFFIX);
        AudioClip deadClip = GetOrLoadAudio(characterData.audioPrefix + DEAD_AUDIO_SUFFIX);
        AudioClip critClip = GetOrLoadAudio(characterData.audioPrefix + CRIT_AUDIO_SUFFIX);

        if (attackClip != null)
        {
            unitSprite.attackAudio = attackClip;
        }
        if (hurtClip != null)
        {
            unitSprite.damageAudio = hurtClip;
        }
        if (deadClip != null)
        {
            unitSprite.deadAudio = deadClip;
        }
        if (critClip != null)
        {
            unitSprite.critAudio = critClip;
        }
    }

    public void BuildCorrinAssets(GameObject unitPrefab)
    {
        UnitSprite unitSprite = unitPrefab.GetComponent<UnitSprite>();
        Unit unit = unitPrefab.GetComponent<Unit>();
        if (unitSprite == null)
        {
            Debug.LogError("UnitSprite component not found on prefab!");
            return;
        }

        bool isMale = false;
        if (GameManager.GetInstance().rng.Next(2) == 1)
        {
            isMale = true;
        }
        int bodyType = GameManager.GetInstance().rng.Next(2) + 1;
        int faceIndex = GameManager.GetInstance().rng.Next(7) + 1;
        int hairIndex = GameManager.GetInstance().rng.Next(12) + 1;
        int detailIndex = GameManager.GetInstance().rng.Next(24) + 1;
        if (detailIndex > 12)
        {
            detailIndex = 0; //No facial detail
        }
        Color hairColor = new Color((float)GameManager.GetInstance().rng.NextDouble(), (float)GameManager.GetInstance().rng.NextDouble(), (float)GameManager.GetInstance().rng.NextDouble(), 1f);
        unit.corrinIsMale = isMale;
        unit.corrinBodyType = bodyType;
        unit.corrinFace = faceIndex;
        unit.corrinHair = hairIndex;
        unit.corrinDetail = detailIndex;
        unit.corrinHairColor = hairColor;
        unit.unitName = "Corrin";
        string facePath = GetCorrinFacePrefix(isMale, bodyType, faceIndex);
        string hairPath = GetCorrinHairPrefix(isMale, bodyType, hairIndex);
        string detailPath = GetCorrinDetailPrefix(isMale, bodyType, detailIndex);

        // Load or retrieve cached sprites using portraitPrefix
        unitSprite.neutralPortrait.sprite = GetOrLoadSprite(facePath + NEUTRAL_SUFFIX);
        unitSprite.damagePortrait.sprite = GetOrLoadSprite(facePath + DAMAGE_SUFFIX);
        unitSprite.critPortrait.sprite = GetOrLoadSprite(facePath + CRIT_SUFFIX);

        unitSprite.corrinHair.sprite = GetOrLoadSprite(hairPath);
        unitSprite.corrinHair.color = hairColor;
        unitSprite.corrinHair.gameObject.SetActive(true);
        if (detailIndex > 0)
        {
            unitSprite.corrinDetail.sprite = GetOrLoadSprite(detailPath);
            unitSprite.corrinDetail.gameObject.SetActive(true);
        }

        if (isMale)
        {
            LoadCharacterAudio(unitPrefab, "CorrinM");
        } else
        {
            LoadCharacterAudio(unitPrefab, "CorrinF");
        } // TODO add more corrin voices, maybe
    }

    private string GetCorrinPortraitPrefix(bool isMale, int bodyType)
    {
        string genderChar = "F";
        if (isMale)
        {
            genderChar = "M";
        }
        return CORRIN_SPRITES_BASE_PATH + "Corrin" + genderChar + bodyType;
    }

    public string GetCorrinFacePrefix(bool isMale, int bodyType, int faceIndex)
    {
        return GetCorrinPortraitPrefix(isMale, bodyType) + " Face " + faceIndex;
    }

    public string GetCorrinHairPrefix(bool isMale, int bodyType, int hairIndex)
    {
        return GetCorrinPortraitPrefix(isMale, bodyType) + " Hair " + hairIndex;
    }

    public string GetCorrinDetailPrefix(bool isMale, int bodyType, int detailIndex)
    {
        return GetCorrinPortraitPrefix(isMale, bodyType) + " Detail " + detailIndex;
    }

    public Sprite GetOrLoadSprite(string spriteName)
    {
        // Make the private method public
        if (spriteCache.TryGetValue(spriteName, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        string fullPath = SPRITES_BASE_PATH + spriteName;
        Sprite loadedSprite = Resources.Load<Sprite>(fullPath);

        if (loadedSprite != null)
        {
            spriteCache[spriteName] = loadedSprite;
            return loadedSprite;
        } else
        {
            return null;
        }
    }

    public AudioClip GetOrLoadAudio(string audioName)
    {
        if (audioCache.TryGetValue(audioName, out AudioClip cachedClip))
        {
            return cachedClip;
        }

        string fullPath = AUDIO_BASE_PATH + audioName;
        AudioClip loadedClip = Resources.Load<AudioClip>(fullPath);

        if (loadedClip != null)
        {
            audioCache[audioName] = loadedClip;
            return loadedClip;
        } else
        {
            Debug.LogWarning($"Audio not found at path: {fullPath}");
            return null;
        }
    }

    public AudioClip LoadSoundEffect(string fileName)
    {
        string fullPath = SFX_BASE_PATH + fileName;
        AudioClip loadedClip = Resources.Load<AudioClip>(fullPath);
        return loadedClip;
    }

    public Sprite LoadBackground(string backgroundName)
    {
        string fullPath = BACKGROUND_BASE_PATH + backgroundName;
        Sprite loadedSprite = Resources.Load<Sprite>(fullPath);
        return loadedSprite;
    }

    public Sprite LoadChapterBackground(string chapterName)
    {
        return LoadBackground(chapterName + BACKGROUND_SUFFIX);
    }

    // Get character data by name
    public CharacterData GetCharacterData(string characterName)
    {
        if (characterDataDict.TryGetValue(characterName.ToLower(), out CharacterData characterData))
        {
            return characterData;
        }
        return null;
    }

    // New method to get a random character based on criteria
    public CharacterData GetRandomCharacter(bool playableOnly = false, bool uniqueOnly = false, bool genericOnly = false)
    {
        // Filter characters based on criteria
        var filteredChars = characterDataDict.Values.Where(c =>
            (!playableOnly || c.playable) &&
            (!uniqueOnly || c.unique) &&
            (!genericOnly || !c.unique)
        ).ToList();

        if (filteredChars.Count == 0)
        {
            Debug.LogWarning($"No characters found matching criteria: playableOnly={playableOnly}, uniqueOnly={uniqueOnly}, genericOnly={genericOnly}");
            return null;
        }

        // Select random character from filtered list
        int randomIndex = UnityEngine.Random.Range(0, filteredChars.Count);
        return filteredChars[randomIndex];
    }

    // Clear all caches
    public void ClearAllCaches()
    {
        ClearSpriteCache();
        ClearAudioCache();
    }

    public void ClearSpriteCache()
    {
        spriteCache.Clear();
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    public void ClearAudioCache()
    {
        audioCache.Clear();
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    // Get statistics about the caches
    public void LogAllCacheStatistics()
    {
        LogSpriteCacheStatistics();
        LogAudioCacheStatistics();
    }

    public void LogSpriteCacheStatistics()
    {
        Debug.Log($"Currently cached sprites: {spriteCache.Count}");
        foreach (var entry in spriteCache)
        {
            Debug.Log($"  - {entry.Key}: {(entry.Value != null ? "Loaded" : "Null")}");
        }
    }

    public void LogAudioCacheStatistics()
    {
        Debug.Log($"Currently cached audio: {audioCache.Count}");
        foreach (var entry in audioCache)
        {
            Debug.Log($"  - {entry.Key}: {(entry.Value != null ? "Loaded" : "Null")}");
        }
    }

    /// <summary>
    /// Get all characters matching the specified criteria
    /// </summary>
    public List<CharacterData> GetAllCharacters(bool playableOnly = false, bool uniqueOnly = false, bool genericOnly = false)
    {
        return characterDataDict.Values.Where(c =>
            (!playableOnly || c.playable) &&
            (!uniqueOnly || c.unique) &&
            (!genericOnly || !c.unique)
        ).ToList();
    }

    public List<string> GetAllUniqueFlags()
    {
        HashSet<string> uniqueFlags = new HashSet<string>();
        foreach (var character in characterDataDict.Values)
        {
            foreach (var flag in character.flags)
            {
                uniqueFlags.Add(flag);
            }
        }
        return uniqueFlags.ToList();
    }

    // Add method to get characters with a specific flag
    public List<CharacterData> GetCharactersWithFlag(string flag)
    {
        return characterDataDict.Values.Where(c => c.flags.Contains(flag)).ToList();
    }
}