using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public static class RandomEnemyCompositionGenerator
{
    private const float FLAG_BASED_WEIGHT = 40f;
    private const float SAME_CLASS_WEIGHT = 4f;
    private const float CLASS_FLAG_BASED_WEIGHT = 40f;
    private const float WEAPON_BASED_WEIGHT = 5f;
    private const float STAT_BASED_WEIGHT = 5f;
    private const float RANDOM_WEIGHT = 20f;
    private static readonly string[] INVALID_UNITS = { "CorrinF", "CorrinM" };

    private const float SINGLE_BOSS_CHANCE = 0.4f;
    private const float SMALL_BOSS_GROUP_CHANCE = 0.8f; //2-5 bosses, large group is 2-10
    private const float NORMAL_BOSS_STRENGTH_CHANCE = 0.5f;
    private const float HARD_BOSS_STRENGTH_CHANCE = 0.9f; // Most bosses 1.5 or 2.0 size, rarely 2.5 size
    private const float STRONG_ENEMY_CHANCE = 0.2f; // 1.0 size instead of 0.7
    private const float NORMAL_MINIBOSS_CHANCE = 0.7f; // 1.2 vs 1.5
    private const float EXCLUDE_GENERIC_BOSS_CHANCE = 0.8f;
    private const float EXCLUDE_GENERIC_MINIBOSS_CHANCE = 0.4f;

    /// <summary>
    /// Select a theme based on weighted probability
    /// </summary>
    private static RandomCompositionGenerator.CompositionTheme SelectTheme(System.Random rng)
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
        var themes = new List<(RandomCompositionGenerator.CompositionTheme theme, float weight)>();
        themes.Add((RandomCompositionGenerator.CompositionTheme.SameClass, SAME_CLASS_WEIGHT));
        themes.Add((RandomCompositionGenerator.CompositionTheme.Random, RANDOM_WEIGHT));

        if (characterFlagThemePossible)
        {
            themes.Add((RandomCompositionGenerator.CompositionTheme.FlagBased, FLAG_BASED_WEIGHT));
        }

        if (classFlagThemePossible)
        {
            themes.Add((RandomCompositionGenerator.CompositionTheme.ClassFlagBased, CLASS_FLAG_BASED_WEIGHT));
        }

        if (weaponThemePossible)
        {
            themes.Add((RandomCompositionGenerator.CompositionTheme.WeaponBased, WEAPON_BASED_WEIGHT));
        }

        if (statThemePossible)
        {
            themes.Add((RandomCompositionGenerator.CompositionTheme.StatBased, STAT_BASED_WEIGHT));
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

        return RandomCompositionGenerator.CompositionTheme.Random; // Fallback
    }

    /// <summary>
    /// Generate a random composition for single team modes
    /// </summary>
    public static RandomCompositionResult GenerateRandomComposition(int teamId, int unitCount, float relativeEnemyPower)
    {
        System.Random rng = GameManager.GetInstance().rng;

        // Choose between available themes
        RandomCompositionGenerator.CompositionTheme theme = SelectTheme(rng);

        switch (theme)
        {
            case RandomCompositionGenerator.CompositionTheme.FlagBased:
                return GenerateFlagBasedComposition(teamId, unitCount, relativeEnemyPower, rng);
            case RandomCompositionGenerator.CompositionTheme.SameClass:
                return GenerateSameClassComposition(teamId, unitCount, relativeEnemyPower, rng);
            case RandomCompositionGenerator.CompositionTheme.ClassFlagBased:
                return GenerateClassFlagBasedComposition(teamId, unitCount, relativeEnemyPower, rng);
            case RandomCompositionGenerator.CompositionTheme.WeaponBased:
                return GenerateWeaponBasedComposition(teamId, unitCount, relativeEnemyPower, rng);
            case RandomCompositionGenerator.CompositionTheme.StatBased:
                return GenerateStatBasedComposition(teamId, unitCount, relativeEnemyPower, rng);
            default:
                return GenerateCompletelyRandomComposition(teamId, unitCount, relativeEnemyPower, rng);
        }
    }

    /// <summary>
    /// Helper method to generate composition from a list of classes
    /// </summary>
    private static RandomCompositionResult GenerateCompositionFromClassList(int teamId, int playerCount, float relativeEnemyStrength, List<UnitClass> availableClasses, string armyName, System.Random rng)
    {
        // Determine if using default class or forcing matching classes
        bool useDefaultClass = true;

        TeamClassComposition.TeamComposition composition = new TeamClassComposition.TeamComposition { teamId = teamId };
        Dictionary<string, TeamClassComposition.ClassCount> classCountMap = new Dictionary<string, TeamClassComposition.ClassCount>();
        List<CharacterAssetLoader.CharacterData> allMatchingChars = new List<CharacterAssetLoader.CharacterData>();
        List<CharacterAssetLoader.CharacterData> genericMatchingChars = new List<CharacterAssetLoader.CharacterData>();
        List<CharacterAssetLoader.CharacterData> uniqueMatchingChars = new List<CharacterAssetLoader.CharacterData>();

        // Get characters whose default class is in our available classes
        var availableClassNames = availableClasses.Select(c => c.name).ToHashSet();

        foreach (string className in availableClassNames)
        {
            allMatchingChars.AddRange(GetCharactersWithDefaultClass(className));
        }

        uniqueMatchingChars = allMatchingChars.Where(c => c.unique).ToList();
        genericMatchingChars = allMatchingChars.Where(c => !c.unique).ToList();

        float enemyPower = 0f;
        string bossName = "";
        GenerateRandomBosses(teamId, classCountMap, uniqueMatchingChars, allMatchingChars, rng, out enemyPower, out bossName);
        if (armyName == "")
        {
            armyName = bossName;
        }

        Debug.Log("Boss Power is " + enemyPower);

        if (genericMatchingChars.Count == 0)
        {
            genericMatchingChars = GetFilteredCharacters(false, true);
        }

        int maxClasses = 2 + rng.Next(5) + rng.Next(5);

        while (genericMatchingChars.Count > maxClasses)
        {
            genericMatchingChars.RemoveAt(rng.Next(genericMatchingChars.Count));
        }

        while (enemyPower < playerCount * relativeEnemyStrength)
        {
            var randomCharacter = genericMatchingChars[rng.Next(genericMatchingChars.Count)];
            string className = randomCharacter.className;

            if (!classCountMap.ContainsKey(className))
            {
                classCountMap[className] = new TeamClassComposition.ClassCount(className, 0);
            }

            var classCount = classCountMap[className];
            classCount.count++;

            float enemyScaleFactor = 0.7f;
            if (rng.NextDouble() < STRONG_ENEMY_CHANCE)
            {
                enemyScaleFactor = 1.0f;
            }

            enemyPower += TeamClassComposition.Instance.CalculateUnitPower(enemyScaleFactor);

            UnitClass unitClass = ClassDataManager.Instance.GetClassDataByName(className);
            classCount.unitData.Add(new UnitStartingData(
                Vector2.zero,
                teamId,
                unitClass,
                randomCharacter.name,
                enemyScaleFactor
            ));
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

    private static void GenerateRandomBosses(int teamId, Dictionary<string, TeamClassComposition.ClassCount> classCountMap, List<CharacterAssetLoader.CharacterData> uniqueMatchingChars,
        List<CharacterAssetLoader.CharacterData> allMatchingChars, System.Random rng, out float enemyPower, out string bossName)
    {
        List<CharacterAssetLoader.CharacterData> bossPool = uniqueMatchingChars;
        if (bossPool.Count <= 0)
        {
            bossPool = allMatchingChars;
            if (bossPool.Count <= 0 || rng.Next(2) == 1)
            {
                bossPool = GetFilteredCharacters(true, false);
            }
        } else if (rng.NextDouble() < EXCLUDE_GENERIC_BOSS_CHANCE)
        {
            bossPool = allMatchingChars;
            if (bossPool.Count <= 0)
            {
                bossPool = GetFilteredCharacters(false, false);
            }
        }

        float bossScaleFactor = 1.5f;
        if (rng.NextDouble() > NORMAL_BOSS_STRENGTH_CHANCE)
        {
            bossScaleFactor = 2.0f;
            if (rng.NextDouble() > HARD_BOSS_STRENGTH_CHANCE)
            {
                bossScaleFactor = 2.5f;
            }
        }
        enemyPower = TeamClassComposition.Instance.CalculateUnitPower(bossScaleFactor);

        CharacterAssetLoader.CharacterData chosenBoss = bossPool[rng.Next(bossPool.Count)];
        string className = chosenBoss.className;

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
            chosenBoss.name,
            bossScaleFactor
        ));

        bossName = chosenBoss.name;

        if (rng.NextDouble() < SINGLE_BOSS_CHANCE)
        {
            return;
        }
        int maxMinibosses = 4;
        if (rng.NextDouble() > SMALL_BOSS_GROUP_CHANCE)
        {
            maxMinibosses = 10;
        }
        int numMinibosses = rng.Next(maxMinibosses) + 1;
        int currentMinibosses = 0;

        bossPool = uniqueMatchingChars;
        try
        {
            bossPool.Remove(chosenBoss);
        } finally {

        }
        if (bossPool.Count <= 0)
        {
            bossPool = allMatchingChars;
            try
            {
                bossPool.Remove(chosenBoss);
            }
            finally
            {

            }
            if (bossPool.Count <= 0 || rng.Next(2) == 1)
            {
                bossPool = GetFilteredCharacters(true, false);
            }
        }
        else if (rng.NextDouble() < EXCLUDE_GENERIC_BOSS_CHANCE)
        {
            bossPool = allMatchingChars;
            try
            {
                bossPool.Remove(chosenBoss);
            }
            finally
            {

            }
            if (bossPool.Count <= 0)
            {
                bossPool = GetFilteredCharacters(false, false);
            }
        }
        try
        {
            bossPool.Remove(chosenBoss);
        }
        finally
        {

        }

        while (currentMinibosses < numMinibosses)
        {
            if (bossPool.Count <= 0)
            {
                return;
            }
            chosenBoss = bossPool[rng.Next(bossPool.Count)];
            bossPool.Remove(chosenBoss);

            int minibossCount = 1;
            if (!chosenBoss.unique && currentMinibosses < numMinibosses - 1)
            {
                minibossCount = rng.Next(numMinibosses - currentMinibosses) + 1;
            }

            bossScaleFactor = 1.2f;
            if (rng.NextDouble() > NORMAL_MINIBOSS_CHANCE)
            {
                bossScaleFactor = 1.5f;
            }

            className = chosenBoss.className;

            if (!classCountMap.ContainsKey(className))
            {
                classCountMap[className] = new TeamClassComposition.ClassCount(className, 0);
            }

            classCount = classCountMap[className];
            classCount.count++;

            unitClass = ClassDataManager.Instance.GetClassDataByName(className);
            for (int i = 0; i < minibossCount; i++)
            {
                enemyPower += TeamClassComposition.Instance.CalculateUnitPower(bossScaleFactor);
                classCount.unitData.Add(new UnitStartingData(
                                Vector2.zero,
                                teamId,
                                unitClass,
                                chosenBoss.name,
                                bossScaleFactor
                            ));
            }
            currentMinibosses += minibossCount;
        }
        return;
    }

    /// <summary>
    /// Get filtered list of characters based on unique criteria
    /// </summary>
    private static List<CharacterAssetLoader.CharacterData> GetFilteredCharacters(bool excludeNonUnique, bool excludeUnique)
    {
        var characterLoader = CharacterAssetLoader.Instance;

        if (excludeNonUnique)
        {
            // Get only unique characters
            return characterLoader.GetAllCharacters(playableOnly: false, uniqueOnly: true, genericOnly: false);
        } else if (excludeUnique)
        {
            return characterLoader.GetAllCharacters(playableOnly: false, uniqueOnly: false, genericOnly: true);
        }
        else
        {
            // Get all characters
            return characterLoader.GetAllCharacters(playableOnly: false, uniqueOnly: false, genericOnly: false);
        }
    }

    private static List<CharacterAssetLoader.CharacterData> GetCharactersWithDefaultClass(string className)
    {
        return CharacterAssetLoader.Instance.GetAllCharacters()
            .Where(c => c.className.Equals(className, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

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
    /// Generate weapon-based themed composition
    /// </summary>
    private static RandomCompositionResult GenerateWeaponBasedComposition(int teamId, int playerCount, float relativeEnemyStrength, System.Random rng)
    {
        var availableWeapons = ClassDataManager.Instance.GetAllPreferredWeapons();

        if (availableWeapons.Count == 0)
        {
            Debug.LogWarning("No weapon types found, falling back to random");
            return GenerateCompletelyRandomComposition(teamId, playerCount, relativeEnemyStrength, rng);
        }

        // Pick a random weapon type
        WeaponEnum selectedWeapon = availableWeapons[rng.Next(availableWeapons.Count)];

        var classesWithWeapon = ClassDataManager.Instance.GetClassesByPreferredWeapon(selectedWeapon);

        if (classesWithWeapon.Count == 0)
        {
            Debug.LogWarning($"No classes found with weapon {selectedWeapon}, falling back to random");
            return GenerateCompletelyRandomComposition(teamId, playerCount, relativeEnemyStrength, rng);
        }

        string armyName = ThemeNamingManager.Instance.GetEnemyThemeName(selectedWeapon.ToString());

        return GenerateCompositionFromClassList(teamId, playerCount, relativeEnemyStrength, classesWithWeapon, armyName, rng);
    }

    private static RandomCompositionResult GenerateStatBasedComposition(int teamId, int playerCount, float relativeEnemyStrength, System.Random rng)
    {
        var availableStats = ClassDataManager.Instance.GetAvailableStatTypes();

        if (availableStats.Count == 0)
        {
            Debug.LogWarning("No stats available, falling back to random");
            return GenerateCompletelyRandomComposition(teamId, playerCount, relativeEnemyStrength, rng);
        }

        // Pick a random stat
        ClassDataManager.StatType selectedStat = availableStats[rng.Next(availableStats.Count)];

        var classesWithHighStat = ClassDataManager.Instance.GetClassesWithHighBaseStat(selectedStat);

        if (classesWithHighStat.Count == 0)
        {
            Debug.LogWarning($"No classes found with high {selectedStat}, falling back to random");
            return GenerateCompletelyRandomComposition(teamId, playerCount, relativeEnemyStrength, rng);
        }

        string armyName = ThemeNamingManager.Instance.GetEnemyThemeName(selectedStat.ToString());

        return GenerateCompositionFromClassList(teamId, playerCount, relativeEnemyStrength, classesWithHighStat, armyName, rng);
    }

    private static RandomCompositionResult GenerateSameClassComposition(int teamId, int playerCount, float relativeEnemyStrength, System.Random rng)
    {
        // Get a random class
        int classCount = ClassDataManager.Instance.GetClassCount();

        int randomIndex = rng.Next(classCount);
        UnitClass selectedClass = ClassDataManager.Instance.GetClassDataByIndex(randomIndex);

        string armyName = ThemeNamingManager.Instance.GetEnemyThemeName(selectedClass.name);
        List<UnitClass> chosenClass = new List<UnitClass>();
        chosenClass.Add(selectedClass);

        return GenerateCompositionFromClassList(teamId, playerCount, relativeEnemyStrength, chosenClass, armyName, rng);
    }

    private static RandomCompositionResult GenerateClassFlagBasedComposition(int teamId, int playerCount, float relativeEnemyStrength, System.Random rng)
    {
        // Get all available class flags
        var availableFlags = ClassDataManager.Instance.GetAllClassFlags();

        // Pick a random flag
        string selectedFlag = availableFlags[rng.Next(availableFlags.Count)];

        // Get all classes with this flag
        var classesWithFlag = ClassDataManager.Instance.GetClassesWithFlag(selectedFlag);

        if (classesWithFlag.Count == 0)
        {
            Debug.LogWarning($"No classes found with flag '{selectedFlag}', falling back to random composition");
            return GenerateCompletelyRandomComposition(teamId, playerCount, relativeEnemyStrength, rng);
        }

        string armyName = ThemeNamingManager.Instance.GetEnemyThemeName(selectedFlag);

        return GenerateCompositionFromClassList(teamId, playerCount, relativeEnemyStrength, classesWithFlag, armyName, rng);
    }

    private static RandomCompositionResult GenerateFlagBasedComposition(int teamId, int playerCount, float relativeEnemyStrength, System.Random rng)
    {
        // Get all available flags
        var availableFlags = CharacterAssetLoader.Instance.GetAllUniqueFlags();

        // Pick a random flag
        string selectedFlag = availableFlags[rng.Next(availableFlags.Count)];

        // Get all characters with this flag
        var charactersWithFlag = CharacterAssetLoader.Instance.GetCharactersWithFlag(selectedFlag);
        var uniqueCharactersWithFlag = charactersWithFlag.Where(c => c.unique).ToList();

        if (charactersWithFlag.Count == 0)
        {
            Debug.LogWarning($"No characters found with flag '{selectedFlag}', falling back to random composition");
            return GenerateCompletelyRandomComposition(teamId, playerCount, relativeEnemyStrength, rng);
        }

        string armyName = ThemeNamingManager.Instance.GetEnemyThemeName(selectedFlag);
        string bossName = "";

        TeamClassComposition.TeamComposition composition = new TeamClassComposition.TeamComposition { teamId = teamId };
        Dictionary<string, TeamClassComposition.ClassCount> classCountMap = new Dictionary<string, TeamClassComposition.ClassCount>();

        float enemyPower = 0f;
        GenerateRandomBosses(teamId, classCountMap, uniqueCharactersWithFlag, charactersWithFlag, rng, out enemyPower, out bossName);
        if (armyName == "")
        {
            armyName = bossName;
        }

        List<string> availableClasses = new List<string>();
        int maxClasses = 2 + rng.Next(5) + rng.Next(5);
        string currentClass = "";
        for (int i = 0; i < maxClasses; i++)
        {
            do
            {
                currentClass = GetRandomClassName(rng);
            } while (availableClasses.Contains(currentClass));
            availableClasses.Add(currentClass);
        }

        List<CharacterAssetLoader.CharacterData> genericMatchingChars = new List<CharacterAssetLoader.CharacterData>();
        foreach (string className in availableClasses)
        {
            genericMatchingChars.AddRange(GetCharactersWithDefaultClass(className));
        }
        genericMatchingChars = genericMatchingChars.Where(c => !c.unique).ToList();

        while (enemyPower < playerCount * relativeEnemyStrength)
        {
            var randomCharacter = genericMatchingChars[rng.Next(genericMatchingChars.Count)];
            string className = randomCharacter.className;

            if (!classCountMap.ContainsKey(className))
            {
                classCountMap[className] = new TeamClassComposition.ClassCount(className, 0);
            }

            var classCount = classCountMap[className];
            classCount.count++;

            float enemyScaleFactor = 0.7f;
            if (rng.NextDouble() < STRONG_ENEMY_CHANCE)
            {
                enemyScaleFactor = 1.0f;
            }

            enemyPower += TeamClassComposition.Instance.CalculateUnitPower(enemyScaleFactor);

            UnitClass unitClass = ClassDataManager.Instance.GetClassDataByName(className);
            classCount.unitData.Add(new UnitStartingData(
                Vector2.zero,
                teamId,
                unitClass,
                randomCharacter.name,
                enemyScaleFactor
            ));
        }

        // Add all class counts to composition
        foreach (var kvp in classCountMap)
        {
            composition.classDistribution.Add(kvp.Value);
        }

        return new RandomCompositionResult(composition, armyName);
    }

    private static RandomCompositionResult GenerateCompletelyRandomComposition(int teamId, int playerCount, float relativeEnemyStrength, System.Random rng)
    {
        TeamClassComposition.TeamComposition composition = new TeamClassComposition.TeamComposition { teamId = teamId };

        // Get filtered character list
        var allCharacters = GetFilteredCharacters(false, false);
        var uniqueCharacters = GetFilteredCharacters(true, false);
        var genericCharacters = GetFilteredCharacters(false, true);

        Dictionary<string, TeamClassComposition.ClassCount> classCountMap = new Dictionary<string, TeamClassComposition.ClassCount>();

        float enemyPower = 0f;
        string armyName = "";
        GenerateRandomBosses(teamId, classCountMap, uniqueCharacters, allCharacters, rng, out enemyPower, out armyName);

        List<string> availableClasses = new List<string>();
        int maxClasses = 2 + rng.Next(5) + rng.Next(5);
        string currentClass = "";
        for (int i = 0; i < maxClasses; i++)
        {
            do
            {
                currentClass = GetRandomClassName(rng);
            } while (availableClasses.Contains(currentClass));
            availableClasses.Add(currentClass);
        }

        List<CharacterAssetLoader.CharacterData> genericMatchingChars = new List<CharacterAssetLoader.CharacterData>();
        foreach (string className in availableClasses)
        {
            genericMatchingChars.AddRange(GetCharactersWithDefaultClass(className));
        }
        genericMatchingChars = genericMatchingChars.Where(c => !c.unique).ToList();

        while (enemyPower < playerCount * relativeEnemyStrength)
        {
            var randomCharacter = genericMatchingChars[rng.Next(genericMatchingChars.Count)];
            string className = randomCharacter.className;

            if (!classCountMap.ContainsKey(className))
            {
                classCountMap[className] = new TeamClassComposition.ClassCount(className, 0);
            }

            var classCount = classCountMap[className];
            classCount.count++;

            float enemyScaleFactor = 0.7f;
            if (rng.NextDouble() < STRONG_ENEMY_CHANCE)
            {
                enemyScaleFactor = 1.0f;
            }

            enemyPower += TeamClassComposition.Instance.CalculateUnitPower(enemyScaleFactor);

            UnitClass unitClass = ClassDataManager.Instance.GetClassDataByName(className);
            classCount.unitData.Add(new UnitStartingData(
                Vector2.zero,
                teamId,
                unitClass,
                randomCharacter.name,
                enemyScaleFactor
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
