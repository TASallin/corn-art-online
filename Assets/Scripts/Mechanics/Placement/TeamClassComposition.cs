using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Updated TeamClassComposition.cs with special class and character name handling
public class TeamClassComposition : MonoBehaviour
{
    // Special class and character name constants
    public const string DEFAULT_CLASS = "DefaultClass";
    public const string RANDOM_CLASS = "RandomClass";
    public const string RANDOM_CHARACTER = "RandomCharacter";
    public const string RANDOM_PLAYABLE_CHARACTER = "RandomPlayableCharacter";
    public const string RANDOM_GENERIC_CHARACTER = "RandomGenericCharacter";
    public const string RANDOM_UNPROMOTED_CHARACTER = "RandomUnpromotedCharacter";
    public const string RANDOM_PROMOTED_CHARACTER = "RandomPromotedCharacter";

    [System.Serializable]
    public class ClassProbability
    {
        public string className;
        public float probability;
    }

    [System.Serializable]
    public class GuaranteedClass
    {
        public string className;
        public int count;
        public bool useTotalCount = false;  // If true, count is a percentage of total units
        public string unitName;  // Empty string means use default
        public float scaleFactor;  // -1 means use default

        public GuaranteedClass()
        {
            unitName = "";
            scaleFactor = -1;
        }

        public GuaranteedClass(string className, int count, string unitName, float scaleFactor)
        {
            this.className = className;
            this.count = count;
            this.unitName = unitName;
            this.scaleFactor = scaleFactor;
            this.useTotalCount = false;
        }

        // Get actual count based on total unit count
        public int GetActualCount(int totalUnitCount)
        {
            if (useTotalCount)
            {
                // If useTotalCount is true, count is a percentage of total units
                int winners = MenuSettings.Instance.numberOfWinners;
                if (winners > 0)
                {
                    return winners;
                }
            }
            return count;
        }
    }

    [System.Serializable]
    public class UnitNameProbability
    {
        public string unitName;
        public float probability;
    }

    [System.Serializable]
    public class ScaleFactorProbability
    {
        public float scaleFactor;
        public float probability;
    }

    [System.Serializable]
    public class TeamClassDistribution
    {
        public int teamId;
        public List<GuaranteedClass> guaranteedClasses = new List<GuaranteedClass>();
        public List<ClassProbability> classProbabilities = new List<ClassProbability>();
        public List<UnitNameProbability> nameProbabilities = new List<UnitNameProbability>();
        public List<ScaleFactorProbability> scaleFactorProbabilities = new List<ScaleFactorProbability>();

        // Ensure probabilities sum to 1
        public void NormalizeProbabilities()
        {
            NormalizeList(classProbabilities);
            NormalizeList(nameProbabilities);
            NormalizeList(scaleFactorProbabilities);
        }

        private void NormalizeList<T>(List<T> probabilities) where T : class
        {
            var probabilityProperty = typeof(T).GetProperty("probability");
            if (probabilityProperty == null) return;

            float sum = 0;
            foreach (var item in probabilities)
            {
                sum += (float)probabilityProperty.GetValue(item);
            }

            if (sum > 0)
            {
                foreach (var item in probabilities)
                {
                    float currentProbability = (float)probabilityProperty.GetValue(item);
                    probabilityProperty.SetValue(item, currentProbability / sum);
                }
            }
        }

        // Get total guaranteed units
        public int GetGuaranteedCount(int totalUnitCount)
        {
            return guaranteedClasses.Sum(gc => gc.GetActualCount(totalUnitCount));
        }
    }

    [System.Serializable]
    public class CompositionData
    {
        public string name;
        public string gameMode = "";  // New field for game mode
        public string musicFile = "";  // New field for music
        public string armyName = ""; // New field for army name
        public int level = 7;
        public List<TeamClassDistribution> teams;
    }

    [System.Serializable]
    public class CompositionsContainer
    {
        public List<CompositionData> compositions;
    }

    [System.Serializable]
    public class ClassCount
    {
        public string className;
        public int count;
        public List<UnitStartingData> unitData; // To store the generated units with names and scales

        public ClassCount(string className, int count)
        {
            this.className = className;
            this.count = count;
            this.unitData = new List<UnitStartingData>();
        }
    }

    [System.Serializable]
    public class TeamComposition
    {
        public int teamId;
        public List<ClassCount> classDistribution = new List<ClassCount>();
    }

    [SerializeField] private TextAsset compositionsFile;
    private Dictionary<string, CompositionData> _compositionsLookup = new Dictionary<string, CompositionData>();

    private static TeamClassComposition _instance;
    public static TeamClassComposition Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("TeamClassComposition");
                _instance = go.AddComponent<TeamClassComposition>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // Default distributions for each team
    public List<TeamClassDistribution> teamDistributions = new List<TeamClassDistribution>();

    // Track additional composition data
    private string currentGameMode = "";
    private string currentMusicFile = "";
    private string currentArmyName = "";
    private int currentLevel = 7;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Load compositions from file if assigned
        if (compositionsFile != null)
        {
            LoadCompositionsFromFile();
        } else
        {
            InitializeDefaultDistributions();
        }
    }

    private void LoadCompositionsFromFile()
    {
        try
        {
            CompositionsContainer container = JsonUtility.FromJson<CompositionsContainer>(compositionsFile.text);

            if (container != null && container.compositions != null)
            {
                foreach (var comp in container.compositions)
                {
                    _compositionsLookup[comp.name] = comp;
                }
                Debug.Log($"Loaded {_compositionsLookup.Count} team compositions from file");
            } else
            {
                Debug.LogError("Failed to parse compositions from file");
                InitializeDefaultDistributions();
            }
        } catch (System.Exception e)
        {
            Debug.LogError($"Error loading compositions from file: {e.Message}");
            InitializeDefaultDistributions();
        }
    }

    // Setup default distributions
    private void InitializeDefaultDistributions()
    {
        // Create default distributions for team 1 and 2 if none exist
        if (teamDistributions.Count == 0)
        {
            for (int i = 1; i <= 2; i++)
            {
                TeamClassDistribution distribution = new TeamClassDistribution
                {
                    teamId = i,
                    guaranteedClasses = new List<GuaranteedClass>(),
                    classProbabilities = new List<ClassProbability>(),
                    nameProbabilities = new List<UnitNameProbability>(),
                    scaleFactorProbabilities = new List<ScaleFactorProbability>()
                };
                teamDistributions.Add(distribution);
            }
        }
    }

    /// <summary>
    /// Load a team composition by name from the file
    /// </summary>
    public bool LoadCompositionByName(string compositionName)
    {
        if (_compositionsLookup.TryGetValue(compositionName, out CompositionData compositionData))
        {
            // Clear existing distributions
            teamDistributions.Clear();

            // Copy team data from the composition
            foreach (var team in compositionData.teams)
            {
                teamDistributions.Add(team);

                // Normalize probabilities
                team.NormalizeProbabilities();
            }

            // Store additional composition data
            currentGameMode = compositionData.gameMode;
            currentMusicFile = compositionData.musicFile;
            currentArmyName = compositionData.armyName;
            currentLevel = compositionData.level;

            Debug.Log($"Loaded composition: {compositionName} | Game Mode: {currentGameMode} | Music: {currentMusicFile} | Army: {currentArmyName}");
            return true;
        } else
        {
            Debug.LogError($"Composition '{compositionName}' not found in file");
            return false;
        }
    }

    /// <summary>
    /// Get the game mode for the current composition
    /// </summary>
    public string GetCurrentGameMode()
    {
        return currentGameMode;
    }

    /// <summary>
    /// Get the music file for the current composition
    /// </summary>
    public string GetCurrentMusicFile()
    {
        return currentMusicFile;
    }

    /// <summary>
    /// Get the army name for the current composition
    /// </summary>
    public string GetCurrentArmyName()
    {
        return currentArmyName;
    }

    public void SetTestDistribution()
    {
        if (compositionsFile != null && LoadCompositionByName("Debug Team"))
        {
            Debug.Log("Successfully loaded Debug Team composition");
        } else
        {
            Debug.LogWarning("Failed to load Debug Team from file, using hardcoded version");
            SetTestDistributionHardcoded();
        }
    }

    /// <summary>
    /// Hardcoded version of the test distribution as a fallback
    /// </summary>
    private void SetTestDistributionHardcoded()
    {
        // Set default values for the new fields
        currentGameMode = "Debug";
        currentMusicFile = "debug_music.mp3";

        // Team 1: 50% Knights, 50% Archers
        TeamClassDistribution team1 = teamDistributions.Find(td => td.teamId == 1);
        if (team1 == null)
        {
            team1 = new TeamClassDistribution { teamId = 1 };
            teamDistributions.Add(team1);
        }

        team1.guaranteedClasses.Clear();
        team1.classProbabilities.Clear();
        team1.nameProbabilities.Clear();
        team1.scaleFactorProbabilities.Clear();

        team1.classProbabilities.Add(new ClassProbability { className = "Knight", probability = 0.5f });
        team1.classProbabilities.Add(new ClassProbability { className = "Archer", probability = 0.5f });

        // All units named "Debug Minion"
        team1.nameProbabilities.Add(new UnitNameProbability { unitName = "Debug Minion", probability = 1.0f });

        // 70% chance of 0.7 scale, 30% chance of 1.0 scale
        team1.scaleFactorProbabilities.Add(new ScaleFactorProbability { scaleFactor = 0.7f, probability = 0.7f });
        team1.scaleFactorProbabilities.Add(new ScaleFactorProbability { scaleFactor = 1.0f, probability = 0.3f });

        // Team 2: 1 Sorcerer (Debug Boss, 1.5 scale), 2 Ninjas, then 50% Knights, 50% Archers
        TeamClassDistribution team2 = teamDistributions.Find(td => td.teamId == 2);
        if (team2 == null)
        {
            team2 = new TeamClassDistribution { teamId = 2 };
            teamDistributions.Add(team2);
        }

        team2.guaranteedClasses.Clear();
        GuaranteedClass sorcerer = new GuaranteedClass();
        sorcerer.className = "Sorcerer";
        sorcerer.count = 1;
        sorcerer.unitName = "Debug Boss";
        sorcerer.scaleFactor = 1.5f;
        team2.guaranteedClasses.Add(sorcerer);

        GuaranteedClass ninja = new GuaranteedClass();
        ninja.className = "Ninja";
        ninja.count = 2;
        team2.guaranteedClasses.Add(ninja);

        // Add a percentage-based unit (20% of team size)
        GuaranteedClass percentageClass = new GuaranteedClass();
        percentageClass.className = "Warrior";
        percentageClass.count = 20; // 20% of total team size
        percentageClass.useTotalCount = true;
        team2.guaranteedClasses.Add(percentageClass);

        team2.classProbabilities.Clear();
        team2.classProbabilities.Add(new ClassProbability { className = "Knight", probability = 0.5f });
        team2.classProbabilities.Add(new ClassProbability { className = "Archer", probability = 0.5f });

        // Random units have same naming and scale settings as team 1
        team2.nameProbabilities.Clear();
        team2.nameProbabilities.Add(new UnitNameProbability { unitName = "Debug Minion", probability = 1.0f });

        team2.scaleFactorProbabilities.Clear();
        team2.scaleFactorProbabilities.Add(new ScaleFactorProbability { scaleFactor = 0.7f, probability = 0.7f });
        team2.scaleFactorProbabilities.Add(new ScaleFactorProbability { scaleFactor = 1.0f, probability = 0.3f });
    }

    /// <summary>
    /// Get all available composition names
    /// </summary>
    public List<string> GetAvailableCompositions()
    {
        return _compositionsLookup.Keys.ToList();
    }

    /// <summary>
    /// Check if a composition exists
    /// </summary>
    public bool CompositionExists(string compositionName)
    {
        return _compositionsLookup.ContainsKey(compositionName);
    }

    /// <summary>
    /// Set a guaranteed class for a team with optional name and scale
    /// </summary>
    public void SetGuaranteedClass(int teamId, string className, int count, string unitName = null, float? scaleFactor = null, bool useTotalCount = false)
    {
        float realScaleFactor;
        if (scaleFactor == null)
        {
            realScaleFactor = 1.0f;
        } else
        {
            realScaleFactor = scaleFactor.Value;
        }

        TeamClassDistribution teamDist = teamDistributions.Find(td => td.teamId == teamId);
        if (teamDist == null)
        {
            teamDist = new TeamClassDistribution
            {
                teamId = teamId,
                guaranteedClasses = new List<GuaranteedClass>(),
                classProbabilities = new List<ClassProbability>(),
                nameProbabilities = new List<UnitNameProbability>(),
                scaleFactorProbabilities = new List<ScaleFactorProbability>()
            };
            teamDistributions.Add(teamDist);
        }

        GuaranteedClass guaranteedClass = teamDist.guaranteedClasses.Find(gc => gc.className == className);
        if (guaranteedClass == null)
        {
            guaranteedClass = new GuaranteedClass(className, count, unitName, realScaleFactor);
            guaranteedClass.useTotalCount = useTotalCount;
            teamDist.guaranteedClasses.Add(guaranteedClass);
        } else
        {
            guaranteedClass.count = count;
            guaranteedClass.unitName = unitName;
            guaranteedClass.scaleFactor = realScaleFactor;
            guaranteedClass.useTotalCount = useTotalCount;
        }
    }

    /// <summary>
    /// Set the probability for a specific class on a specific team
    /// </summary>
    public void SetClassProbability(int teamId, string className, float probability)
    {
        TeamClassDistribution teamDist = teamDistributions.Find(td => td.teamId == teamId);
        if (teamDist == null)
        {
            teamDist = new TeamClassDistribution
            {
                teamId = teamId,
                guaranteedClasses = new List<GuaranteedClass>(),
                classProbabilities = new List<ClassProbability>()
            };
            teamDistributions.Add(teamDist);
        }

        ClassProbability classProbability = teamDist.classProbabilities.Find(cp => cp.className == className);
        if (classProbability == null)
        {
            classProbability = new ClassProbability
            {
                className = className,
                probability = probability
            };
            teamDist.classProbabilities.Add(classProbability);
        } else
        {
            classProbability.probability = probability;
        }

        teamDist.NormalizeProbabilities();
    }

    /// <summary>
    /// Set name probability for random units on a specific team
    /// </summary>
    public void SetNameProbability(int teamId, string unitName, float probability)
    {
        TeamClassDistribution teamDist = GetOrCreateTeamDistribution(teamId);

        UnitNameProbability nameProbability = teamDist.nameProbabilities.Find(np => np.unitName == unitName);
        if (nameProbability == null)
        {
            nameProbability = new UnitNameProbability { unitName = unitName, probability = probability };
            teamDist.nameProbabilities.Add(nameProbability);
        } else
        {
            nameProbability.probability = probability;
        }

        teamDist.NormalizeProbabilities();
    }

    /// <summary>
    /// Set scale factor probability for random units on a specific team
    /// </summary>
    public void SetScaleFactorProbability(int teamId, float scaleFactor, float probability)
    {
        TeamClassDistribution teamDist = GetOrCreateTeamDistribution(teamId);

        ScaleFactorProbability scaleFactorProbability = teamDist.scaleFactorProbabilities.Find(sfp => sfp.scaleFactor == scaleFactor);
        if (scaleFactorProbability == null)
        {
            scaleFactorProbability = new ScaleFactorProbability { scaleFactor = scaleFactor, probability = probability };
            teamDist.scaleFactorProbabilities.Add(scaleFactorProbability);
        } else
        {
            scaleFactorProbability.probability = probability;
        }

        teamDist.NormalizeProbabilities();
    }

    private TeamClassDistribution GetOrCreateTeamDistribution(int teamId)
    {
        TeamClassDistribution teamDist = teamDistributions.Find(td => td.teamId == teamId);
        if (teamDist == null)
        {
            teamDist = new TeamClassDistribution
            {
                teamId = teamId,
                guaranteedClasses = new List<GuaranteedClass>(),
                classProbabilities = new List<ClassProbability>(),
                nameProbabilities = new List<UnitNameProbability>(),
                scaleFactorProbabilities = new List<ScaleFactorProbability>()
            };
            teamDistributions.Add(teamDist);
        }
        return teamDist;
    }

    private string ResolveSpecialCharacterName(string characterName)
    {
        if (string.IsNullOrEmpty(characterName) || (!characterName.StartsWith("Random") && characterName != RANDOM_CHARACTER))
        {
            return characterName;
        }

        CharacterAssetLoader.CharacterData characterData = null;

        switch (characterName)
        {
            case RANDOM_CHARACTER:
                characterData = CharacterAssetLoader.Instance.GetRandomCharacter();
                break;

            case RANDOM_PLAYABLE_CHARACTER:
                characterData = CharacterAssetLoader.Instance.GetRandomCharacter(playableOnly: true);
                break;

            case RANDOM_GENERIC_CHARACTER:
                characterData = CharacterAssetLoader.Instance.GetRandomCharacter(genericOnly: true);
                break;

            case RANDOM_UNPROMOTED_CHARACTER:
                // Not yet implemented - using random character as fallback
                characterData = CharacterAssetLoader.Instance.GetRandomCharacter();
                Debug.LogWarning("RandomUnpromotedCharacter not yet implemented, using random character");
                break;

            case RANDOM_PROMOTED_CHARACTER:
                // Not yet implemented - using random character as fallback
                characterData = CharacterAssetLoader.Instance.GetRandomCharacter();
                Debug.LogWarning("RandomPromotedCharacter not yet implemented, using random character");
                break;
        }

        return characterData?.name ?? "Unit";
    }

    // Get class for a character when using DefaultClass
    private string GetDefaultClassForCharacter(string characterName)
    {
        // Use character data to get their default class
        var characterData = CharacterAssetLoader.Instance.GetCharacterData(characterName);
        if (characterData != null)
        {
            return characterData.className;
        }

        // Fallback if character not found
        Debug.LogWarning($"Character '{characterName}' not found in character data, using default Knight class");
        return "Knight";
    }

    // Get random class (unchanged from your implementation)
    private string GetRandomClass(TeamClassDistribution distribution)
    {
        if (distribution.classProbabilities.Count == 0)
            return "Knight"; // Default class

        double randomValue = GameManager.GetInstance().rng.NextDouble();
        float cumulative = 0;

        foreach (var classProbability in distribution.classProbabilities)
        {
            cumulative += classProbability.probability;
            if (randomValue <= cumulative)
                return classProbability.className;
        }

        return distribution.classProbabilities[0].className; // Fallback
    }

    // Modified to handle special character names
    private string GetRandomName(TeamClassDistribution distribution)
    {
        if (distribution.nameProbabilities.Count == 0)
            return "Unit"; // Default name

        double randomValue = GameManager.GetInstance().rng.NextDouble();
        float cumulative = 0;

        foreach (var nameProbability in distribution.nameProbabilities)
        {
            cumulative += nameProbability.probability;
            if (randomValue <= cumulative)
                return ResolveSpecialCharacterName(nameProbability.unitName);
        }

        return ResolveSpecialCharacterName(distribution.nameProbabilities[0].unitName); // Fallback
    }

    // Unchanged from your implementation
    private float GetRandomScaleFactor(TeamClassDistribution distribution)
    {
        if (distribution.scaleFactorProbabilities.Count == 0)
            return 1.0f; // Default scale

        double randomValue = GameManager.GetInstance().rng.NextDouble();
        float cumulative = 0;

        foreach (var scaleFactorProbability in distribution.scaleFactorProbabilities)
        {
            cumulative += scaleFactorProbability.probability;
            if (randomValue <= cumulative)
                return scaleFactorProbability.scaleFactor;
        }

        return distribution.scaleFactorProbabilities[0].scaleFactor; // Fallback
    }

    /// <summary>
    /// Gets a completely random class from all available classes in ClassDataManager
    /// </summary>
    private string GetRandomClassFromAllClasses()
    {
        ClassDataManager classManager = ClassDataManager.Instance;
        int classCount = classManager.GetClassCount();

        if (classCount == 0)
        {
            Debug.LogWarning("No classes found in ClassDataManager, defaulting to Knight");
            return "Knight";
        }

        int randomIndex = UnityEngine.Random.Range(0, classCount);
        UnitClass randomClass = classManager.GetClassDataByIndex(randomIndex);

        if (randomClass != null)
        {
            return randomClass.name;
        } else
        {
            Debug.LogWarning($"Failed to get random class at index {randomIndex}, defaulting to Knight");
            return "Knight";
        }
    }

    /// <summary>
    /// Determines how many units of each class should be in each team, including name/scale data
    /// </summary>
    public TeamComposition[] DetermineTeamCompositions(int team1Count, int team2Count)
    {
        TeamComposition[] teamCompositions = new TeamComposition[2];

        // Process team 1
        teamCompositions[0] = DetermineTeamComposition(1, team1Count);

        // Process team 2  
        teamCompositions[1] = DetermineTeamComposition(2, team2Count);

        return teamCompositions;
    }

    public TeamComposition GetTeamComposition(int teamId, int unitCount)
    {
        // For single team modes, use the existing distribution logic but force team ID
        return DetermineTeamComposition(teamId, unitCount);
    }

    private TeamComposition DetermineTeamComposition(int teamId, int unitCount)
    {
        TeamComposition composition = new TeamComposition { teamId = teamId };

        // Find the distribution for this team
        TeamClassDistribution teamDist = teamDistributions.Find(td => td.teamId == teamId);
        if (teamDist == null)
        {
            // Default to all Knights if no distribution is defined
            ClassCount knightCount = new ClassCount("Knight", unitCount);
            for (int i = 0; i < unitCount; i++)
            {
                UnitClass unitClass = ClassDataManager.Instance.GetClassDataByName("Knight");
                knightCount.unitData.Add(new UnitStartingData(Vector2.zero, teamId, unitClass, "Unit", 1.0f));
            }
            composition.classDistribution.Add(knightCount);
            return composition;
        }

        // Dictionary to accumulate total counts for each class with their unit data
        Dictionary<string, ClassCount> classCountData = new Dictionary<string, ClassCount>();

        // First, add all guaranteed classes with specific names and scale factors
        int guaranteedTotal = 0;
        foreach (GuaranteedClass gc in teamDist.guaranteedClasses)
        {
            // Calculate actual count based on whether it's a fixed count or percentage
            int actualCount = gc.GetActualCount(unitCount);

            // Add only as many guaranteed units as we have room for
            int countToAdd = Mathf.Min(actualCount, unitCount - guaranteedTotal);

            for (int i = 0; i < countToAdd; i++)
            {
                // First, determine the character name (handle special character names)
                string characterName = string.IsNullOrEmpty(gc.unitName) ? "Unit" :
                                       ResolveSpecialCharacterName(gc.unitName);

                // Then determine the class based on character and className
                string actualClassName = gc.className;
                if (actualClassName == DEFAULT_CLASS)
                {
                    // Use character's default class
                    actualClassName = GetDefaultClassForCharacter(characterName);
                } else if (actualClassName == RANDOM_CLASS)
                {
                    // Pick a completely random class from all available classes
                    actualClassName = GetRandomClassFromAllClasses();
                }

                // Now add the unit to the appropriate class count
                if (!classCountData.ContainsKey(actualClassName))
                    classCountData[actualClassName] = new ClassCount(actualClassName, 0);

                ClassCount classCount = classCountData[actualClassName];
                classCount.count++;

                UnitClass unitClass = ClassDataManager.Instance.GetClassDataByName(actualClassName);
                float scaleFactor = gc.scaleFactor <= 0 ? 1.0f : gc.scaleFactor;

                classCount.unitData.Add(new UnitStartingData(Vector2.zero, teamId, unitClass, characterName, scaleFactor));
            }

            guaranteedTotal += countToAdd;

            // If we've filled all units with guaranteed classes, stop
            if (guaranteedTotal >= unitCount)
                break;
        }

        // If we still have units to allocate based on probabilities
        int remainingUnits = unitCount - guaranteedTotal;
        if (remainingUnits > 0 && teamDist.classProbabilities.Count > 0)
        {
            teamDist.NormalizeProbabilities();

            // Assign random classes based on probabilities for each remaining unit
            for (int i = 0; i < remainingUnits; i++)
            {
                // First get the character
                string characterName = GetRandomName(teamDist);

                // Then get the class, using character's default class if needed
                string selectedClass = GetRandomClass(teamDist);
                if (selectedClass == DEFAULT_CLASS)
                {
                    selectedClass = GetDefaultClassForCharacter(characterName);
                } else if (selectedClass == RANDOM_CLASS)
                {
                    // Pick a completely random class from all available classes
                    selectedClass = GetRandomClassFromAllClasses();
                }

                if (!classCountData.ContainsKey(selectedClass))
                    classCountData[selectedClass] = new ClassCount(selectedClass, 0);

                ClassCount classCount = classCountData[selectedClass];
                classCount.count++;

                UnitClass unitClass = ClassDataManager.Instance.GetClassDataByName(selectedClass);
                float scaleFactor = GetRandomScaleFactor(teamDist);
                classCount.unitData.Add(new UnitStartingData(Vector2.zero, teamId, unitClass, characterName, scaleFactor));
            }
        }

        // Convert to final class distribution
        foreach (var kvp in classCountData)
        {
            if (kvp.Value.count > 0)  // Only add classes with at least one unit
            {
                composition.classDistribution.Add(kvp.Value);
            }
        }

        return composition;
    }

    /// <summary>
    /// Calculate the power of a unit based on its scale factor
    /// </summary>
    public float CalculateUnitPower(float scaleFactor)
    {
        // Base power is 1.0 for scale 1.0
        float power = 1.0f;

        if (scaleFactor > 1)
        {
            // Health scales with e^scaleFactor, damage scales linearly
            power = Mathf.Exp(scaleFactor);
        } else
        {
            // Health and damage both scale with scaleFactor²
            power = scaleFactor * scaleFactor;
        }

        return power;
    }

    /// <summary>
    /// Calculate the total power of a team composition
    /// </summary>
    public float CalculateTeamPower(TeamComposition composition)
    {
        float totalPower = 0;

        foreach (var classCount in composition.classDistribution)
        {
            foreach (var unitData in classCount.unitData)
            {
                totalPower += CalculateUnitPower(unitData.ScaleFactor);
            }
        }

        return totalPower;
    }

    /// <summary>
    /// Update the DetermineTeamCompositionsWithRelativeStrength method to handle percentage-based guaranteed classes
    /// </summary>
    public TeamComposition[] DetermineTeamCompositionsWithRelativeStrength(int team1Count, float team2RelativeStrength)
    {
        // First, determine team 1 composition
        TeamComposition team1Composition = DetermineTeamComposition(1, team1Count);
        float team1Power = CalculateTeamPower(team1Composition);

        // Calculate how much power team 2 should have
        float targetTeam2Power = team1Power * team2RelativeStrength;

        // Find the right number of team 2 units
        int team2Count = EstimateTeam2Count(team1Composition, targetTeam2Power);
        TeamComposition team2Composition = DetermineTeamComposition(2, team2Count);

        // Optional: Log the actual power ratio for debugging
        float team2Power = CalculateTeamPower(team2Composition);
        float actualRatio = team2Power / team1Power;
        Debug.Log($"Target Team 2 strength ratio: {team2RelativeStrength}, Actual ratio: {actualRatio}");

        return new TeamComposition[] { team1Composition, team2Composition };
    }

    /// <summary>
    /// Estimate the number of team 2 units needed to match target power
    /// </summary>
    private int EstimateTeam2Count(TeamComposition team1, float targetTeam2Power)
    {
        // Get the distribution for team 2
        TeamClassDistribution team2Dist = teamDistributions.Find(td => td.teamId == 2);
        if (team2Dist == null)
            return Mathf.RoundToInt(targetTeam2Power); // Fallback

        // Calculate power from guaranteed units first
        float guaranteedPower = 0;
        int guaranteedCount = 0;

        foreach (var gc in team2Dist.guaranteedClasses)
        {
            float scaleFactor = gc.scaleFactor <= 0 ? 1.0f : gc.scaleFactor;
            int actualCount = gc.count; // This will be the number we actually get
            guaranteedPower += CalculateUnitPower(scaleFactor) * actualCount;
            guaranteedCount += actualCount;
        }

        // If guaranteed units already meet or exceed target power, return that count
        if (guaranteedPower >= targetTeam2Power)
        {
            return guaranteedCount;
        }

        // Calculate remaining power needed
        float remainingPowerNeeded = targetTeam2Power - guaranteedPower;

        // Calculate average power per random unit
        float averageRandomPower = EstimateAverageRandomUnitPower(team2Dist);

        // Calculate additional units needed
        int additionalUnits = Mathf.CeilToInt(remainingPowerNeeded / averageRandomPower);

        return guaranteedCount + additionalUnits;
    }

    /// <summary>
    /// Calculate the average power of a random unit based on team distribution
    /// </summary>
    private float EstimateAverageRandomUnitPower(TeamClassDistribution distribution)
    {
        if (distribution.scaleFactorProbabilities.Count == 0)
            return 1.0f; // Default power

        // Calculate the weighted average scale factor of random units
        float averageScale = 0;
        foreach (var sfp in distribution.scaleFactorProbabilities)
        {
            averageScale += sfp.scaleFactor * sfp.probability;
        }

        return CalculateUnitPower(averageScale);
    }

    /// <summary>
    /// Adjust team 2 count to get closer to target relative strength
    /// </summary>
    private int AdjustTeam2Count(int currentCount, float currentRatio, float targetRatio)
    {
        // Simple proportional adjustment
        float adjustmentFactor = targetRatio / currentRatio;
        int newCount = Mathf.RoundToInt(currentCount * adjustmentFactor);

        // Ensure minimum count and not too drastic changes
        return Mathf.Clamp(newCount, 1, currentCount * 3);
    }

    // Add this method to TeamClassComposition class
    /// <summary>
    /// Generate a composition with only random units (no guaranteed units)
    /// </summary>
    public TeamComposition GenerateRandomOnlyComposition(int teamId, int unitCount)
    {
        TeamComposition composition = new TeamComposition { teamId = teamId };

        // Find the distribution for this team
        TeamClassDistribution teamDist = teamDistributions.Find(td => td.teamId == teamId);
        if (teamDist == null)
        {
            // Default to all Knights if no distribution is defined
            ClassCount knightCount = new ClassCount("Knight", unitCount);
            for (int i = 0; i < unitCount; i++)
            {
                UnitClass unitClass = ClassDataManager.Instance.GetClassDataByName("Knight");
                knightCount.unitData.Add(new UnitStartingData(Vector2.zero, teamId, unitClass, "Unit", 1.0f));
            }
            composition.classDistribution.Add(knightCount);
            return composition;
        }

        // Only use probability-based classes (skip guaranteed classes)
        if (teamDist.classProbabilities.Count > 0)
        {
            teamDist.NormalizeProbabilities();

            // Dictionary to accumulate total counts for each class
            Dictionary<string, ClassCount> classCountData = new Dictionary<string, ClassCount>();

            // Assign random classes based on probabilities for each unit
            for (int i = 0; i < unitCount; i++)
            {
                // Get the character
                string characterName = GetRandomName(teamDist);

                // Get the class, using character's default class if needed
                string selectedClass = GetRandomClass(teamDist);
                if (selectedClass == DEFAULT_CLASS)
                {
                    selectedClass = GetDefaultClassForCharacter(characterName);
                } else if (selectedClass == RANDOM_CLASS)
                {
                    selectedClass = GetRandomClassFromAllClasses();
                }

                if (!classCountData.ContainsKey(selectedClass))
                    classCountData[selectedClass] = new ClassCount(selectedClass, 0);

                ClassCount classCount = classCountData[selectedClass];
                classCount.count++;

                UnitClass unitClass = ClassDataManager.Instance.GetClassDataByName(selectedClass);
                float scaleFactor = GetRandomScaleFactor(teamDist);
                classCount.unitData.Add(new UnitStartingData(Vector2.zero, teamId, unitClass, characterName, scaleFactor));
            }

            // Convert to final class distribution
            foreach (var kvp in classCountData)
            {
                if (kvp.Value.count > 0)
                {
                    composition.classDistribution.Add(kvp.Value);
                }
            }
        }

        return composition;
    }

    /// <summary>
    /// Get team composition, always using random generation for certain game modes
    /// </summary>
    public TeamComposition GetTeamCompositionWithRandomFallback(int teamId, int unitCount, string gameMode, out string armyName)
    {
        armyName = null;

        // These modes ALWAYS use random compositions
        bool alwaysUseRandom = gameMode.ToLower() == "battle royale" ||
                              gameMode.ToLower() == "battleroyale" ||
                              gameMode.ToLower() == "hot potato" ||
                              gameMode.ToLower() == "team battle";

        bool forceCorrinTeam = MenuSettings.Instance.corrinPlayerTeam;

        if (forceCorrinTeam)
        {
            var corrinResult = RandomCompositionGenerator.GenerateCorrinComposition(teamId, unitCount, GameManager.GetInstance().rng);
            return corrinResult.Composition;
        }
        else if (alwaysUseRandom)
        {
            Debug.Log($"Using random composition for {gameMode} mode");
            var randomResult = RandomCompositionGenerator.GenerateRandomComposition(teamId, unitCount);
            armyName = randomResult.ArmyName;
            return randomResult.Composition;
        } else
        {
            // Use the existing distribution logic for other game modes
            return DetermineTeamComposition(teamId, unitCount);
        }
    }

    /// <summary>
    /// Get team composition with conditional random generation for player team
    /// </summary>
    public TeamComposition GetTeamCompositionWithConditionalRandom(int teamId, int unitCount, float relativeEnemyStrength, string gameMode, out string armyName)
    {
        armyName = null;

        if (teamId == 2)
        {
            //unitCount = Mathf.RoundToInt(unitCount * relativeEnemyStrength);
        }

        // Check if this is team 1 (player team) and randomPlayerTeam is enabled
        bool forceRandomForPlayerTeam = (teamId == 1) && MenuSettings.Instance.randomPlayerTeam;
        bool forceCorrinTeam = (teamId == 1) && MenuSettings.Instance.corrinPlayerTeam;
        bool randomEnemyTeam = (teamId == 2) && MenuSettings.Instance.randomEnemyTeam;

        // These modes ALWAYS use random compositions regardless of randomPlayerTeam setting
        bool alwaysUseRandom = gameMode.ToLower() == "battle royale" ||
                              gameMode.ToLower() == "battleroyale" ||
                              gameMode.ToLower() == "hot potato" ||
                              gameMode.ToLower() == "team battle";

        if (forceCorrinTeam)
        {
            var corrinResult = RandomCompositionGenerator.GenerateCorrinComposition(teamId, unitCount, GameManager.GetInstance().rng);
            return corrinResult.Composition;
        }
        else if (alwaysUseRandom)
        {
            Debug.Log($"Using random composition for {gameMode} mode");
            var randomResult = RandomCompositionGenerator.GenerateRandomComposition(teamId, unitCount);
            armyName = randomResult.ArmyName;
            return randomResult.Composition;
        }
        else if (forceRandomForPlayerTeam)
        {
            var randomResult = RandomCompositionGenerator.GenerateRandomComposition(teamId, unitCount);
            // Don't override armyName for non-random modes - keep level-based army name
            return randomResult.Composition;
        }
        else if (randomEnemyTeam)
        {
            var randomResult = RandomEnemyCompositionGenerator.GenerateRandomComposition(teamId, unitCount, relativeEnemyStrength);
            armyName = randomResult.ArmyName;
            return randomResult.Composition;
        }
        else
        {
            // Use the existing distribution logic for other teams/modes
            return DetermineTeamCompositionsWithRelativeStrength(unitCount, relativeEnemyStrength)[1];
        }
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }
}