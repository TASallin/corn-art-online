using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

// Wrapper class to include army name with composition
public class RandomCompositionResult
{
    public TeamClassComposition.TeamComposition Composition { get; set; }
    public string ArmyName { get; set; }

    public RandomCompositionResult(TeamClassComposition.TeamComposition composition, string armyName)
    {
        Composition = composition;
        ArmyName = armyName;
    }
}

public static class RandomCompositionGenerator
{
    // Theme types (unchanged)
    public enum CompositionTheme
    {
        SameCharacter,
        FlagBased,
        SameClass,
        ClassFlagBased,
        PromotionBased,
        WeaponBased,
        StatBased,
        Random
    }

    private const float THEME_PROBABILITY = 0.9f;
    private const float SAME_CHARACTER_WEIGHT = 10f;
    private const float FLAG_BASED_WEIGHT = 40f;
    private const float SAME_CLASS_WEIGHT = 15f;
    private const float CLASS_FLAG_BASED_WEIGHT = 40f;
    private const float PROMOTION_BASED_WEIGHT = 5f;
    private const float WEAPON_BASED_WEIGHT = 15f;
    private const float STAT_BASED_WEIGHT = 15f;
    private const float DEFAULT_CLASS_PROBABILITY = 0.5f;
    private const float EXCLUDE_NON_UNIQUE_PROBABILITY = 0.7f;

    /// <summary>
    /// Select a theme based on weighted probability
    /// </summary>
    private static CompositionTheme SelectTheme(System.Random rng)
    {
        // Get available data to check if themes are possible
        var characterFlags = CharacterAssetLoader.Instance.GetAllUniqueFlags();
        var classFlags = ClassDataManager.Instance.GetAllClassFlags();
        var availableWeapons = ClassDataManager.Instance.GetAllPreferredWeapons();
        var availableStats = ClassDataManager.Instance.GetAvailableStatTypes();

        bool characterFlagThemePossible = characterFlags.Count > 0;
        bool classFlagThemePossible = classFlags.Count > 0;
        bool weaponThemePossible = availableWeapons.Count > 0;
        bool statThemePossible = availableStats.Count > 0;

        // Build weighted list
        var themes = new List<(CompositionTheme theme, float weight)>();
        themes.Add((CompositionTheme.SameCharacter, SAME_CHARACTER_WEIGHT));
        themes.Add((CompositionTheme.SameClass, SAME_CLASS_WEIGHT));
        themes.Add((CompositionTheme.PromotionBased, PROMOTION_BASED_WEIGHT));

        if (characterFlagThemePossible)
        {
            themes.Add((CompositionTheme.FlagBased, FLAG_BASED_WEIGHT));
        }

        if (classFlagThemePossible)
        {
            themes.Add((CompositionTheme.ClassFlagBased, CLASS_FLAG_BASED_WEIGHT));
        }

        if (weaponThemePossible)
        {
            themes.Add((CompositionTheme.WeaponBased, WEAPON_BASED_WEIGHT));
        }

        if (statThemePossible)
        {
            themes.Add((CompositionTheme.StatBased, STAT_BASED_WEIGHT));
        }

        // Select based on weights
        float totalWeight = themes.Sum(t => t.weight);
        float randomValue = (float)rng.NextDouble() * totalWeight;
        float cumulative = 0;

        foreach (var (theme, weight) in themes)
        {
            cumulative += weight;
            if (randomValue <= cumulative)
            {
                Debug.Log($"Selected theme: {theme} (weight: {weight}/{totalWeight:F1})");
                return theme;
            }
        }

        return CompositionTheme.SameCharacter; // Fallback
    }

    /// <summary>
    /// Generate a random composition for single team modes
    /// </summary>
    public static RandomCompositionResult GenerateRandomComposition(int teamId, int unitCount)
    {
        System.Random rng = GameManager.GetInstance().rng;

        // Determine if we're using a theme or random
        bool useTheme = rng.NextDouble() < THEME_PROBABILITY;

        if (useTheme)
        {
            // Choose between available themes
            CompositionTheme theme = SelectTheme(rng);

            switch (theme)
            {
                case CompositionTheme.SameCharacter:
                    return GenerateSameCharacterComposition(teamId, unitCount, rng);
                case CompositionTheme.FlagBased:
                    return GenerateFlagBasedComposition(teamId, unitCount, rng);
                case CompositionTheme.SameClass:
                    return GenerateSameClassComposition(teamId, unitCount, rng);
                case CompositionTheme.ClassFlagBased:
                    return GenerateClassFlagBasedComposition(teamId, unitCount, rng);
                case CompositionTheme.PromotionBased:
                    return GeneratePromotionBasedComposition(teamId, unitCount, rng);
                case CompositionTheme.WeaponBased:
                    return GenerateWeaponBasedComposition(teamId, unitCount, rng);
                case CompositionTheme.StatBased:
                    return GenerateStatBasedComposition(teamId, unitCount, rng);
                default:
                    return GenerateCompletelyRandomComposition(teamId, unitCount, rng);
            }
        } else
        {
            return GenerateCompletelyRandomComposition(teamId, unitCount, rng);
        }
    }

    /// <summary>
    /// Generate promotion-based themed composition
    /// </summary>
    private static RandomCompositionResult GeneratePromotionBasedComposition(int teamId, int unitCount, System.Random rng)
    {
        // Randomly choose promoted or unpromoted
        bool usePromoted = rng.NextDouble() < 0.5;

        var classesWithPromotion = ClassDataManager.Instance.GetClassesByPromotionStatus(usePromoted);

        if (classesWithPromotion.Count == 0)
        {
            Debug.LogWarning($"No {(usePromoted ? "promoted" : "unpromoted")} classes found, falling back to random");
            return GenerateCompletelyRandomComposition(teamId, unitCount, rng);
        }

        string rawThemeName = usePromoted ? "Promoted" : "Unpromoted";
        string armyName = ThemeNamingManager.Instance.GetThemeName(rawThemeName);

        return GenerateCompositionFromClassList(teamId, unitCount, classesWithPromotion, armyName, rng);
    }

    /// <summary>
    /// Generate weapon-based themed composition
    /// </summary>
    private static RandomCompositionResult GenerateWeaponBasedComposition(int teamId, int unitCount, System.Random rng)
    {
        var availableWeapons = ClassDataManager.Instance.GetAllPreferredWeapons();

        if (availableWeapons.Count == 0)
        {
            Debug.LogWarning("No weapon types found, falling back to random");
            return GenerateCompletelyRandomComposition(teamId, unitCount, rng);
        }

        // Pick a random weapon type
        WeaponEnum selectedWeapon = availableWeapons[rng.Next(availableWeapons.Count)];

        var classesWithWeapon = ClassDataManager.Instance.GetClassesByPreferredWeapon(selectedWeapon);

        if (classesWithWeapon.Count == 0)
        {
            Debug.LogWarning($"No classes found with weapon {selectedWeapon}, falling back to random");
            return GenerateCompletelyRandomComposition(teamId, unitCount, rng);
        }

        string armyName = ThemeNamingManager.Instance.GetThemeName(selectedWeapon.ToString());

        return GenerateCompositionFromClassList(teamId, unitCount, classesWithWeapon, armyName, rng);
    }

    /// <summary>
    /// Generate stat-based themed composition
    /// </summary>
    private static RandomCompositionResult GenerateStatBasedComposition(int teamId, int unitCount, System.Random rng)
    {
        var availableStats = ClassDataManager.Instance.GetAvailableStatTypes();

        if (availableStats.Count == 0)
        {
            Debug.LogWarning("No stats available, falling back to random");
            return GenerateCompletelyRandomComposition(teamId, unitCount, rng);
        }

        // Pick a random stat
        ClassDataManager.StatType selectedStat = availableStats[rng.Next(availableStats.Count)];

        var classesWithHighStat = ClassDataManager.Instance.GetClassesWithHighBaseStat(selectedStat);

        if (classesWithHighStat.Count == 0)
        {
            Debug.LogWarning($"No classes found with high {selectedStat}, falling back to random");
            return GenerateCompletelyRandomComposition(teamId, unitCount, rng);
        }

        string armyName = ThemeNamingManager.Instance.GetThemeName(selectedStat.ToString());

        return GenerateCompositionFromClassList(teamId, unitCount, classesWithHighStat, armyName, rng);
    }

    /// <summary>
    /// Helper method to generate composition from a list of classes
    /// </summary>
    private static RandomCompositionResult GenerateCompositionFromClassList(int teamId, int unitCount, List<UnitClass> availableClasses, string armyName, System.Random rng)
    {
        // Determine if using default class or forcing matching classes
        bool useDefaultClass = rng.NextDouble() < DEFAULT_CLASS_PROBABILITY;

        TeamClassComposition.TeamComposition composition = new TeamClassComposition.TeamComposition { teamId = teamId };
        Dictionary<string, TeamClassComposition.ClassCount> classCountMap = new Dictionary<string, TeamClassComposition.ClassCount>();

        if (useDefaultClass)
        {
            // Get characters whose default class is in our available classes
            var availableClassNames = availableClasses.Select(c => c.name).ToHashSet();
            var matchingCharacters = new List<CharacterAssetLoader.CharacterData>();

            foreach (string className in availableClassNames)
            {
                matchingCharacters.AddRange(GetCharactersWithDefaultClass(className));
            }

            if (matchingCharacters.Count == 0)
            {
                Debug.Log($"No characters found with matching default classes, using random characters");
                useDefaultClass = false;
            } else
            {
                // Apply unique filter if needed
                bool excludeNonUnique = rng.NextDouble() < EXCLUDE_NON_UNIQUE_PROBABILITY;
                if (excludeNonUnique)
                {
                    var uniqueCharacters = matchingCharacters.Where(c => c.unique).ToList();
                    if (uniqueCharacters.Count > 0)
                    {
                        matchingCharacters = uniqueCharacters;
                    }
                }

                for (int i = 0; i < unitCount; i++)
                {
                    var randomCharacter = matchingCharacters[rng.Next(matchingCharacters.Count)];
                    string className = randomCharacter.className;

                    if (!classCountMap.ContainsKey(className))
                    {
                        classCountMap[className] = new TeamClassComposition.ClassCount(className, 0);
                    }

                    var classCount = classCountMap[className];
                    classCount.count++;

                    UnitClass unitClass = ClassDataManager.Instance.GetClassDataByName(className);
                    classCount.unitData.Add(new UnitStartingData(
                        Vector2.zero,
                        teamId,
                        unitClass,
                        randomCharacter.name,
                        1.0f
                    ));
                }
            }
        }

        if (!useDefaultClass)
        {
            // Use random characters but force them to use matching classes
            bool excludeNonUnique = rng.NextDouble() < EXCLUDE_NON_UNIQUE_PROBABILITY;
            var characters = GetFilteredCharacters(excludeNonUnique);

            if (characters.Count == 0)
            {
                return new RandomCompositionResult(CreateDefaultComposition(teamId, unitCount), "Default");
            }

            for (int i = 0; i < unitCount; i++)
            {
                var randomCharacter = characters[rng.Next(characters.Count)];
                var randomClass = availableClasses[rng.Next(availableClasses.Count)];
                string className = randomClass.name;

                if (!classCountMap.ContainsKey(className))
                {
                    classCountMap[className] = new TeamClassComposition.ClassCount(className, 0);
                }

                var classCount = classCountMap[className];
                classCount.count++;

                classCount.unitData.Add(new UnitStartingData(
                    Vector2.zero,
                    teamId,
                    randomClass,
                    randomCharacter.name,
                    1.0f
                ));
            }
        }

        // Add all class counts to composition
        foreach (var kvp in classCountMap)
        {
            if (kvp.Value.count > 0)
            {
                composition.classDistribution.Add(kvp.Value);
            }
        }

        return new RandomCompositionResult(composition, armyName);
    }

    /// <summary>
    /// Generate a same-class themed composition
    /// </summary>
    private static RandomCompositionResult GenerateSameClassComposition(int teamId, int unitCount, System.Random rng)
    {
        // Get a random class
        int classCount = ClassDataManager.Instance.GetClassCount();
        if (classCount == 0)
        {
            Debug.LogWarning("No classes available, using default composition");
            return new RandomCompositionResult(CreateDefaultComposition(teamId, unitCount), "Default");
        }

        int randomIndex = rng.Next(classCount);
        UnitClass selectedClass = ClassDataManager.Instance.GetClassDataByIndex(randomIndex);

        if (selectedClass == null)
        {
            return new RandomCompositionResult(CreateDefaultComposition(teamId, unitCount), "Default");
        }

        string armyName = ThemeNamingManager.Instance.GetThemeName(selectedClass.name);

        // Determine if using default class or forcing this class
        bool useDefaultClass = rng.NextDouble() < DEFAULT_CLASS_PROBABILITY;

        TeamClassComposition.TeamComposition composition = new TeamClassComposition.TeamComposition { teamId = teamId };

        if (useDefaultClass)
        {
            // Only use characters whose default class matches
            var matchingCharacters = GetCharactersWithDefaultClass(selectedClass.name);

            if (matchingCharacters.Count == 0)
            {
                Debug.Log($"No characters found with default class {selectedClass.name}, using random characters");
                // Fall back to random characters with forced class
                return GenerateSameClassWithRandomCharacters(teamId, unitCount, selectedClass, armyName, rng);
            }

            // Apply unique filter if needed
            bool excludeNonUnique = rng.NextDouble() < EXCLUDE_NON_UNIQUE_PROBABILITY;
            if (excludeNonUnique)
            {
                var uniqueCharacters = matchingCharacters.Where(c => c.unique).ToList();
                if (uniqueCharacters.Count > 0)
                {
                    matchingCharacters = uniqueCharacters;
                }
            }

            TeamClassComposition.ClassCount classCounts = new TeamClassComposition.ClassCount(selectedClass.name, unitCount);

            for (int i = 0; i < unitCount; i++)
            {
                var randomCharacter = matchingCharacters[rng.Next(matchingCharacters.Count)];

                classCounts.unitData.Add(new UnitStartingData(
                    Vector2.zero,
                    teamId,
                    selectedClass,
                    randomCharacter.name,
                    1.0f
                ));
            }

            composition.classDistribution.Add(classCounts);
        } else
        {
            // Use random characters but force them to use this class
            return GenerateSameClassWithRandomCharacters(teamId, unitCount, selectedClass, armyName, rng);
        }

        return new RandomCompositionResult(composition, armyName);
    }

    /// <summary>
    /// Generate same class composition with random characters
    /// </summary>
    private static RandomCompositionResult GenerateSameClassWithRandomCharacters(int teamId, int unitCount, UnitClass selectedClass, string armyName, System.Random rng)
    {
        TeamClassComposition.TeamComposition composition = new TeamClassComposition.TeamComposition { teamId = teamId };

        // Get filtered character list
        bool excludeNonUnique = rng.NextDouble() < EXCLUDE_NON_UNIQUE_PROBABILITY;
        var characters = GetFilteredCharacters(excludeNonUnique);

        if (characters.Count == 0)
        {
            Debug.LogWarning("No characters available after filtering");
            return new RandomCompositionResult(CreateDefaultComposition(teamId, unitCount), "Default");
        }

        TeamClassComposition.ClassCount classCount = new TeamClassComposition.ClassCount(selectedClass.name, unitCount);

        for (int i = 0; i < unitCount; i++)
        {
            var randomCharacter = characters[rng.Next(characters.Count)];

            classCount.unitData.Add(new UnitStartingData(
                Vector2.zero,
                teamId,
                selectedClass,
                randomCharacter.name,
                1.0f
            ));
        }

        composition.classDistribution.Add(classCount);
        return new RandomCompositionResult(composition, armyName);
    }

    /// <summary>
    /// Generate class flag-based themed composition
    /// </summary>
    private static RandomCompositionResult GenerateClassFlagBasedComposition(int teamId, int unitCount, System.Random rng)
    {
        // Get all available class flags
        var availableFlags = ClassDataManager.Instance.GetAllClassFlags();

        if (availableFlags.Count == 0)
        {
            Debug.LogWarning("No class flags available, falling back to random composition");
            return GenerateCompletelyRandomComposition(teamId, unitCount, rng);
        }

        // Pick a random flag
        string selectedFlag = availableFlags[rng.Next(availableFlags.Count)];

        // Get all classes with this flag
        var classesWithFlag = ClassDataManager.Instance.GetClassesWithFlag(selectedFlag);

        if (classesWithFlag.Count == 0)
        {
            Debug.LogWarning($"No classes found with flag '{selectedFlag}', falling back to random composition");
            return GenerateCompletelyRandomComposition(teamId, unitCount, rng);
        }

        string armyName = ThemeNamingManager.Instance.GetThemeName(selectedFlag);

        // Determine if using default class or forcing matching classes
        bool useDefaultClass = rng.NextDouble() < DEFAULT_CLASS_PROBABILITY;

        TeamClassComposition.TeamComposition composition = new TeamClassComposition.TeamComposition { teamId = teamId };
        Dictionary<string, TeamClassComposition.ClassCount> classCountMap = new Dictionary<string, TeamClassComposition.ClassCount>();

        if (useDefaultClass)
        {
            // Get characters whose default class has the flag
            var matchingCharacters = new List<CharacterAssetLoader.CharacterData>();
            foreach (var unitClass in classesWithFlag)
            {
                matchingCharacters.AddRange(GetCharactersWithDefaultClass(unitClass.name));
            }

            if (matchingCharacters.Count == 0)
            {
                Debug.Log($"No characters found with default classes having flag {selectedFlag}, using random characters");
                // Fall back to random characters with forced matching classes
                useDefaultClass = false;
            } else
            {
                // Apply unique filter if needed
                bool excludeNonUnique = rng.NextDouble() < EXCLUDE_NON_UNIQUE_PROBABILITY;
                if (excludeNonUnique)
                {
                    var uniqueCharacters = matchingCharacters.Where(c => c.unique).ToList();
                    if (uniqueCharacters.Count > 0)
                    {
                        matchingCharacters = uniqueCharacters;
                    }
                }

                for (int i = 0; i < unitCount; i++)
                {
                    var randomCharacter = matchingCharacters[rng.Next(matchingCharacters.Count)];
                    string className = randomCharacter.className;

                    if (!classCountMap.ContainsKey(className))
                    {
                        classCountMap[className] = new TeamClassComposition.ClassCount(className, 0);
                    }

                    var classCount = classCountMap[className];
                    classCount.count++;

                    UnitClass unitClass = ClassDataManager.Instance.GetClassDataByName(className);
                    classCount.unitData.Add(new UnitStartingData(
                        Vector2.zero,
                        teamId,
                        unitClass,
                        randomCharacter.name,
                        1.0f
                    ));
                }
            }
        }

        if (!useDefaultClass)
        {
            // Use random characters but force them to use matching classes
            bool excludeNonUnique = rng.NextDouble() < EXCLUDE_NON_UNIQUE_PROBABILITY;
            var characters = GetFilteredCharacters(excludeNonUnique);

            if (characters.Count == 0)
            {
                return new RandomCompositionResult(CreateDefaultComposition(teamId, unitCount), "Default");
            }

            for (int i = 0; i < unitCount; i++)
            {
                var randomCharacter = characters[rng.Next(characters.Count)];
                var randomClass = classesWithFlag[rng.Next(classesWithFlag.Count)];
                string className = randomClass.name;

                if (!classCountMap.ContainsKey(className))
                {
                    classCountMap[className] = new TeamClassComposition.ClassCount(className, 0);
                }

                var classCount = classCountMap[className];
                classCount.count++;

                classCount.unitData.Add(new UnitStartingData(
                    Vector2.zero,
                    teamId,
                    randomClass,
                    randomCharacter.name,
                    1.0f
                ));
            }
        }

        // Add all class counts to composition
        foreach (var kvp in classCountMap)
        {
            if (kvp.Value.count > 0)
            {
                composition.classDistribution.Add(kvp.Value);
            }
        }

        return new RandomCompositionResult(composition, armyName);
    }

    /// <summary>
    /// Get characters that have a specific default class
    /// </summary>
    private static List<CharacterAssetLoader.CharacterData> GetCharactersWithDefaultClass(string className)
    {
        return CharacterAssetLoader.Instance.GetAllCharacters()
            .Where(c => c.className.Equals(className, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Generate a composition where all units are the same character
    /// </summary>
    // <summary>
    /// Generate a composition where all units are the same character
    /// </summary>
    private static RandomCompositionResult GenerateSameCharacterComposition(int teamId, int unitCount, System.Random rng)
    {
        // Get filtered character list
        bool excludeNonUnique = rng.NextDouble() < EXCLUDE_NON_UNIQUE_PROBABILITY;
        var characters = GetFilteredCharacters(excludeNonUnique);

        if (characters.Count == 0)
        {
            Debug.LogWarning("No characters available after filtering, using default composition");
            return new RandomCompositionResult(CreateDefaultComposition(teamId, unitCount), "Default");
        }

        // Pick a random character for everyone
        var selectedCharacter = characters[rng.Next(characters.Count)];
        string armyName = ThemeNamingManager.Instance.GetThemeName(selectedCharacter.name);

        // Determine if using default class or random classes
        bool useDefaultClass = rng.NextDouble() < DEFAULT_CLASS_PROBABILITY;

        TeamClassComposition.TeamComposition composition = new TeamClassComposition.TeamComposition { teamId = teamId };

        if (useDefaultClass)
        {
            // Everyone gets the same character with their default class
            string className = selectedCharacter.className;
            TeamClassComposition.ClassCount classCount = new TeamClassComposition.ClassCount(className, unitCount);

            // Create unit data for each unit
            for (int i = 0; i < unitCount; i++)
            {
                UnitClass unitClass = ClassDataManager.Instance.GetClassDataByName(className);
                if (unitClass == null)
                {
                    Debug.LogWarning($"Class {className} not found, using Knight");
                    unitClass = ClassDataManager.Instance.GetClassDataByName("Knight");
                }

                classCount.unitData.Add(new UnitStartingData(
                    Vector2.zero,
                    teamId,
                    unitClass,
                    selectedCharacter.name,
                    1.0f
                ));
            }

            composition.classDistribution.Add(classCount);
        } else
        {
            // Same character but random classes
            Dictionary<string, TeamClassComposition.ClassCount> classCountMap = new Dictionary<string, TeamClassComposition.ClassCount>();

            for (int i = 0; i < unitCount; i++)
            {
                // Get a random class
                string randomClassName = GetRandomClassName(rng);

                if (!classCountMap.ContainsKey(randomClassName))
                {
                    classCountMap[randomClassName] = new TeamClassComposition.ClassCount(randomClassName, 0);
                }

                var classCount = classCountMap[randomClassName];
                classCount.count++;

                UnitClass unitClass = ClassDataManager.Instance.GetClassDataByName(randomClassName);
                if (unitClass == null)
                {
                    Debug.LogWarning($"Class {randomClassName} not found, using Knight");
                    unitClass = ClassDataManager.Instance.GetClassDataByName("Knight");
                }

                classCount.unitData.Add(new UnitStartingData(
                    Vector2.zero,
                    teamId,
                    unitClass,
                    selectedCharacter.name,
                    1.0f
                ));
            }

            // Add all class counts to composition
            foreach (var kvp in classCountMap)
            {
                composition.classDistribution.Add(kvp.Value);
            }
        }

        return new RandomCompositionResult(composition, armyName);
    }

    /// <summary>
    /// Generate a flag-based themed composition
    /// </summary>
    private static RandomCompositionResult GenerateFlagBasedComposition(int teamId, int unitCount, System.Random rng)
    {
        // Get all available flags
        var availableFlags = CharacterAssetLoader.Instance.GetAllUniqueFlags();

        if (availableFlags.Count == 0)
        {
            Debug.LogWarning("No flags available, falling back to random composition");
            return GenerateCompletelyRandomComposition(teamId, unitCount, rng);
        }

        // Pick a random flag
        string selectedFlag = availableFlags[rng.Next(availableFlags.Count)];

        // Get all characters with this flag
        var charactersWithFlag = CharacterAssetLoader.Instance.GetCharactersWithFlag(selectedFlag);

        if (charactersWithFlag.Count == 0)
        {
            Debug.LogWarning($"No characters found with flag '{selectedFlag}', falling back to random composition");
            return GenerateCompletelyRandomComposition(teamId, unitCount, rng);
        }

        // Apply unique filter if needed
        bool excludeNonUnique = rng.NextDouble() < EXCLUDE_NON_UNIQUE_PROBABILITY;
        if (excludeNonUnique)
        {
            charactersWithFlag = charactersWithFlag.Where(c => c.unique).ToList();
            if (charactersWithFlag.Count == 0)
            {
                // Fall back to non-unique if no unique characters with this flag
                charactersWithFlag = CharacterAssetLoader.Instance.GetCharactersWithFlag(selectedFlag);
            }
        }

        string armyName = ThemeNamingManager.Instance.GetThemeName(selectedFlag);

        TeamClassComposition.TeamComposition composition = new TeamClassComposition.TeamComposition { teamId = teamId };
        Dictionary<string, TeamClassComposition.ClassCount> classCountMap = new Dictionary<string, TeamClassComposition.ClassCount>();

        for (int i = 0; i < unitCount; i++)
        {
            // Pick a random character from those with the flag
            var randomCharacter = charactersWithFlag[rng.Next(charactersWithFlag.Count)];

            // Decide if using default class or random class
            bool useDefaultClass = rng.NextDouble() < DEFAULT_CLASS_PROBABILITY;
            string className = useDefaultClass ? randomCharacter.className : GetRandomClassName(rng);

            if (!classCountMap.ContainsKey(className))
            {
                classCountMap[className] = new TeamClassComposition.ClassCount(className, 0);
            }

            var classCount = classCountMap[className];
            classCount.count++;

            UnitClass unitClass = ClassDataManager.Instance.GetClassDataByName(className);
            if (unitClass == null)
            {
                Debug.LogWarning($"Class {className} not found, using Knight");
                unitClass = ClassDataManager.Instance.GetClassDataByName("Knight");
            }

            classCount.unitData.Add(new UnitStartingData(
                Vector2.zero,
                teamId,
                unitClass,
                randomCharacter.name,
                1.0f
            ));
        }

        // Add all class counts to composition
        foreach (var kvp in classCountMap)
        {
            composition.classDistribution.Add(kvp.Value);
        }

        return new RandomCompositionResult(composition, armyName);
    }

    /// <summary>
    /// Generate a completely random composition with different characters
    /// </summary>
    private static RandomCompositionResult GenerateCompletelyRandomComposition(int teamId, int unitCount, System.Random rng)
    {
        TeamClassComposition.TeamComposition composition = new TeamClassComposition.TeamComposition { teamId = teamId };

        // Get filtered character list
        bool excludeNonUnique = rng.NextDouble() < EXCLUDE_NON_UNIQUE_PROBABILITY;
        var characters = GetFilteredCharacters(excludeNonUnique);

        if (characters.Count == 0)
        {
            Debug.LogWarning("No characters available after filtering, using default composition");
            return new RandomCompositionResult(CreateDefaultComposition(teamId, unitCount), "Default");
        }

        Dictionary<string, TeamClassComposition.ClassCount> classCountMap = new Dictionary<string, TeamClassComposition.ClassCount>();

        for (int i = 0; i < unitCount; i++)
        {
            // Pick a random character
            var randomCharacter = characters[rng.Next(characters.Count)];

            // Decide if using default class or random class
            bool useDefaultClass = rng.NextDouble() < DEFAULT_CLASS_PROBABILITY;
            string className = useDefaultClass ? randomCharacter.className : GetRandomClassName(rng);

            if (!classCountMap.ContainsKey(className))
            {
                classCountMap[className] = new TeamClassComposition.ClassCount(className, 0);
            }

            var classCount = classCountMap[className];
            classCount.count++;

            UnitClass unitClass = ClassDataManager.Instance.GetClassDataByName(className);
            if (unitClass == null)
            {
                Debug.LogWarning($"Class {className} not found, using Knight");
                unitClass = ClassDataManager.Instance.GetClassDataByName("Knight");
            }

            classCount.unitData.Add(new UnitStartingData(
                Vector2.zero,
                teamId,
                unitClass,
                randomCharacter.name,
                1.0f
            ));
        }

        // Add all class counts to composition
        foreach (var kvp in classCountMap)
        {
            composition.classDistribution.Add(kvp.Value);
        }

        string armyName = ThemeNamingManager.Instance.GetThemeName("Random");

        return new RandomCompositionResult(composition, armyName);
    }

    /// <summary>
    /// Get filtered list of characters based on unique criteria
    /// </summary>
    private static List<CharacterAssetLoader.CharacterData> GetFilteredCharacters(bool excludeNonUnique)
    {
        var characterLoader = CharacterAssetLoader.Instance;

        if (excludeNonUnique)
        {
            // Get only unique characters
            return characterLoader.GetAllCharacters(playableOnly: false, uniqueOnly: true, genericOnly: false);
        } else
        {
            // Get all characters
            return characterLoader.GetAllCharacters(playableOnly: false, uniqueOnly: false, genericOnly: false);
        }
    }

    /// <summary>
    /// Get a random class name from all available classes
    /// </summary>
    private static string GetRandomClassName(System.Random rng)
    {
        int classCount = ClassDataManager.Instance.GetClassCount();
        if (classCount == 0)
        {
            return "Knight";
        }

        int randomIndex = rng.Next(classCount);
        UnitClass randomClass = ClassDataManager.Instance.GetClassDataByIndex(randomIndex);

        return randomClass?.name ?? "Knight";
    }

    /// <summary>
    /// Create a default composition (all Knights) as fallback
    /// </summary>
    private static TeamClassComposition.TeamComposition CreateDefaultComposition(int teamId, int unitCount)
    {
        TeamClassComposition.TeamComposition composition = new TeamClassComposition.TeamComposition { teamId = teamId };

        TeamClassComposition.ClassCount knightCount = new TeamClassComposition.ClassCount("Knight", unitCount);
        UnitClass knightClass = ClassDataManager.Instance.GetClassDataByName("Knight");

        for (int i = 0; i < unitCount; i++)
        {
            knightCount.unitData.Add(new UnitStartingData(
                Vector2.zero,
                teamId,
                knightClass,
                "Unit",
                1.0f
            ));
        }

        composition.classDistribution.Add(knightCount);
        return composition;
    }

    /// <summary>
    /// Generate a composition where all units are corrin
    /// </summary>
    // <summary>
    /// Generate a composition where all units are corrin
    /// </summary>
    public static RandomCompositionResult GenerateCorrinComposition(int teamId, int unitCount, System.Random rng)
    {
        // Pick a random character for everyone
        var selectedCharacter = CharacterAssetLoader.Instance.GetCharacterData("Corrin");
        string armyName = ThemeNamingManager.Instance.GetThemeName(selectedCharacter.name);

        // Determine if using default class or random classes
        bool useDefaultClass = rng.NextDouble() < DEFAULT_CLASS_PROBABILITY;

        TeamClassComposition.TeamComposition composition = new TeamClassComposition.TeamComposition { teamId = teamId };

        // Same character but random classes
        Dictionary<string, TeamClassComposition.ClassCount> classCountMap = new Dictionary<string, TeamClassComposition.ClassCount>();

        for (int i = 0; i < unitCount; i++)
        {
            // Get a random class
            string randomClassName = GetRandomClassName(rng);

            if (!classCountMap.ContainsKey(randomClassName))
            {
                classCountMap[randomClassName] = new TeamClassComposition.ClassCount(randomClassName, 0);
            }

            var classCount = classCountMap[randomClassName];
            classCount.count++;

            UnitClass unitClass = ClassDataManager.Instance.GetClassDataByName(randomClassName);
            if (unitClass == null)
            {
                Debug.LogWarning($"Class {randomClassName} not found, using Knight");
                unitClass = ClassDataManager.Instance.GetClassDataByName("Knight");
            }

            classCount.unitData.Add(new UnitStartingData(
                Vector2.zero,
                teamId,
                unitClass,
                selectedCharacter.name,
                1.0f
            ));
        }

        // Add all class counts to composition
        foreach (var kvp in classCountMap)
        {
            composition.classDistribution.Add(kvp.Value);
        }

        return new RandomCompositionResult(composition, armyName);
    }
}