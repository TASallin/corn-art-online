using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MenuSettings : MonoBehaviour
{
    private static MenuSettings _instance;
    public static MenuSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("MenuSettings");
                _instance = go.AddComponent<MenuSettings>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // Current settings
    [Header("Game Settings")]
    public string selectedLevel = "";
    public string selectedGameMode = ""; // This is the actual game mode (Route, Survive, etc.)
    public string streamMode = "Recruit"; // "Recruit" or "WheelOfDeath"

    [Header("Music Settings")]
    public string levelMusic = ""; // Music determined by level/composition (used for 'default' battle music)
    public string selectedBattleMusic = "default"; // User's choice from battle music dropdown
    public string selectedMenuMusic = "random"; // User's choice from menu music dropdown

    public int numberOfWinners = 1; // Always stores winners (for WheelOfDeath, this is players - deaths)
    public int numberOfDeaths = 1; // Used only for UI display in WheelOfDeath
    public string[] playerNames = new string[0];

    // Track winners and losers
    public List<string> winners = new List<string>();
    public List<string> eliminated = new List<string>();

    // You can add more settings here as needed
    [Header("Team Settings")]
    public int team1Count = 10;
    public int team2Count = 10;
    public bool randomPlayerTeam = true;
    public bool corrinPlayerTeam = false;
    public bool randomWinConditions = false;
    public bool randomEnemyTeam = false;

    [Header("Level Info")]
    public string armyName = ""; // New field for army name

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetSelectedLevel(string levelName)
    {
        selectedLevel = levelName;

        // For Recruit mode, load game mode from TeamClassComposition
        if (streamMode == "Recruit")
        {
            if (TeamClassComposition.Instance.CompositionExists(levelName))
            {
                TeamClassComposition.Instance.LoadCompositionByName(levelName);
                selectedGameMode = TeamClassComposition.Instance.GetCurrentGameMode();
                armyName = TeamClassComposition.Instance.GetCurrentArmyName();
                levelMusic = TeamClassComposition.Instance.GetCurrentMusicFile();
            }
        }
        // For WheelOfDeath mode, determine game mode based on player count and deaths
        else if (streamMode == "WheelOfDeath")
        {
            selectedGameMode = DetermineWheelOfDeathGameMode();
            // You might want to set music differently for WheelOfDeath
            if (TeamClassComposition.Instance.CompositionExists(levelName))
            {
                TeamClassComposition.Instance.LoadCompositionByName(levelName);
                levelMusic = TeamClassComposition.Instance.GetCurrentMusicFile();
                armyName = TeamClassComposition.Instance.GetCurrentArmyName(); //TODO Delete once randomized player armies are implemented
            }
        }

        Debug.Log($"Selected Level: {selectedLevel}, Game Mode: {selectedGameMode}, Stream Mode: {streamMode}, Level Music: {levelMusic}");
    }

    public void CheckRandomWinCondition()
    {
        if (streamMode == "Recruit" && randomWinConditions)
        {
            string[] availableGameModes = { "Defeat Boss", "Route", "Seize", "Survive" };
            selectedGameMode = availableGameModes[GameManager.GetInstance().rng.Next(availableGameModes.Length)];
        }
    }

    private string DetermineWheelOfDeathGameMode()
    {
        int playerCount = playerNames.Length;
        var rng = new System.Random();
        var availableModes = new List<(string mode, float weight)>();

        // Calculate winner ratio
        float winnerRatio = (float)numberOfWinners / playerCount;

        // Check if Team Battle is possible
        int numberOfTeams = TeamBattleUtility.DetermineNumberOfTeams(playerCount, numberOfWinners);
        bool teamBattlePossible = numberOfTeams != -1;

        // Team Battle - highest weight when available
        if (teamBattlePossible)
        {
            availableModes.Add(("Team Battle", 40f));
        }

        // Hot Potato - better for high winner ratios (many winners)
        if (winnerRatio >= 0.3f) // 30% or more winners
        {
            float hotPotatoWeight = 20f + (winnerRatio * 30f); // Weight increases with winner ratio
            availableModes.Add(("Hot Potato", hotPotatoWeight));
        } else
        {
            // Still available but lower weight for low winner counts
            availableModes.Add(("Hot Potato", 10f));
        }

        // Battle Royale - better for low winner ratios
        if (winnerRatio <= 0.5f) // 50% or fewer winners
        {
            float battleRoyaleWeight = 30f + ((1f - winnerRatio) * 20f); // Weight increases as winner ratio decreases
            availableModes.Add(("Battle Royale", battleRoyaleWeight));
        } else
        {
            // Still available but lower weight
            availableModes.Add(("Battle Royale", 15f));
        }

        // Survive - works well for low to medium winner ratios
        if (winnerRatio <= 0.6f) // 60% or fewer winners
        {
            float surviveWeight = 25f + ((1f - winnerRatio) * 15f);
            availableModes.Add(("Survive", surviveWeight));
        } else
        {
            availableModes.Add(("Survive", 10f));
        }

        // Normalize weights and select
        float totalWeight = availableModes.Sum(m => m.weight);
        float randomValue = (float)rng.NextDouble() * totalWeight;
        float cumulativeWeight = 0f;

        foreach (var (mode, weight) in availableModes)
        {
            cumulativeWeight += weight;
            if (randomValue <= cumulativeWeight)
            {
                Debug.Log($"Selected {mode} for {playerCount} players with {numberOfWinners} winners (weight: {weight:F1}/{totalWeight:F1})");
                return mode;
            }
        }

        // Fallback (should never reach here)
        return "Battle Royale";
    }

    public static void LogGameModeWeights(int playerCount, int winnerCount)
    {
        Debug.Log($"=== Game Mode Weights for {playerCount} players, {winnerCount} winners ===");

        float winnerRatio = (float)winnerCount / playerCount;
        int numberOfTeams = TeamBattleUtility.DetermineNumberOfTeams(playerCount, winnerCount);

        Debug.Log($"Winner Ratio: {winnerRatio:P}");
        Debug.Log($"Team Battle Possible: {numberOfTeams != -1}");

        if (numberOfTeams != -1)
        {
            Debug.Log($"  Team Battle: 40.0 (available with {numberOfTeams} teams)");
        } else
        {
            Debug.Log($"  Team Battle: N/A (no valid configuration)");
        }

        float hotPotatoWeight = winnerRatio >= 0.3f ? 20f + (winnerRatio * 30f) : 10f;
        Debug.Log($"  Hot Potato: {hotPotatoWeight:F1}");

        float battleRoyaleWeight = winnerRatio <= 0.5f ? 30f + ((1f - winnerRatio) * 20f) : 15f;
        Debug.Log($"  Battle Royale: {battleRoyaleWeight:F1}");

        float surviveWeight = winnerRatio <= 0.6f ? 25f + ((1f - winnerRatio) * 15f) : 10f;
        Debug.Log($"  Survive: {surviveWeight:F1}");
    }

    public void UpdateWheelOfDeathWinners(int deaths)
    {
        numberOfDeaths = deaths;
        // Convert deaths to winners (survivors)
        numberOfWinners = Mathf.Max(1, playerNames.Length - deaths);

        // Recalculate game mode when deaths change
        if (streamMode == "WheelOfDeath")
        {
            selectedGameMode = DetermineWheelOfDeathGameMode();
        }
    }

    public void NormalizeWinnersToDeaths()
    {
        if (streamMode == "WheelOfDeath")
        {
            UpdateWheelOfDeathWinners(numberOfDeaths);
        }
    }

    public int GetDeathsFromWinners()
    {
        if (IsWheelOfDeathMode() && playerNames != null && playerNames.Length > 0)
        {
            return Mathf.Max(1, playerNames.Length - numberOfWinners);
        }
        return 1;
    }

    public void SetBattleMusic(string musicName)
    {
        selectedBattleMusic = musicName;

        // Update MusicManager if it exists
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetBattleTrack(musicName);
        }
    }

    public void SetMenuMusic(string musicName)
    {
        selectedMenuMusic = musicName;

        // Update MusicManager if it exists
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetMenuTrack(musicName);
        }
    }

    public void Reset()
    {
        selectedLevel = "";
        selectedGameMode = "";
        streamMode = "Recruit"; // Default back to Recruit
        levelMusic = "";
        armyName = "";
        selectedBattleMusic = "default";
        selectedMenuMusic = "random";
        team1Count = 10;
        team2Count = 10;
        randomPlayerTeam = true; // Reset to default
        corrinPlayerTeam = false;
        randomWinConditions = false;
        randomEnemyTeam = false;
        numberOfWinners = 1;
        numberOfDeaths = 1;
        playerNames = new string[0];
        winners.Clear();
        eliminated.Clear();
    }

    public bool IsWheelOfDeathMode()
    {
        return streamMode == "WheelOfDeath";
    }

    public bool IsRecruitMode()
    {
        return streamMode == "Recruit";
    }
}