using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EliminatorData
{
    public string Name;
    public string CharacterName;
    public bool IsBossUnit;
    public bool IsEliteUnit;
    public bool CorrinIsMale;
    public int CorrinBodyType;
    public int CorrinFace;
    public int CorrinHair;
    public int CorrinDetail;
    public Color CorrinHairColor;

    public EliminatorData(Unit unit)
    {
        if (unit != null)
        {
            Name = !string.IsNullOrEmpty(unit.playerName) ? unit.playerName : unit.unitName;
            CharacterName = unit.unitName;
            IsBossUnit = unit.IsBoss();
            IsEliteUnit = unit.IsElite();
            if (CharacterName == "Corrin")
            {
                CorrinIsMale = unit.corrinIsMale;
                CorrinBodyType = unit.corrinBodyType;
                CorrinFace = unit.corrinFace;
                CorrinHair = unit.corrinHair;
                CorrinDetail = unit.corrinDetail;
                CorrinHairColor = unit.corrinHairColor;
            }
        }
    }
}

[System.Serializable]
public class EnemyDefeatData
{
    public string Name;
    public string CharacterName;
    public float ScaleFactor;
    public bool IsBossUnit;
    public bool IsEliteUnit;
    public bool CorrinIsMale;
    public int CorrinBodyType;
    public int CorrinFace;
    public int CorrinHair;
    public int CorrinDetail;
    public Color CorrinHairColor;

    public EnemyDefeatData(Unit unit)
    {
        if (unit != null)
        {
            Name = !string.IsNullOrEmpty(unit.playerName) ? unit.playerName : unit.unitName;
            CharacterName = unit.unitName;
            ScaleFactor = unit.transform.localScale.x; // Assuming uniform scale
            IsBossUnit = unit.IsBoss();
            IsEliteUnit = unit.IsElite();
            if (CharacterName == "Corrin")
            {
                CorrinIsMale = unit.corrinIsMale;
                CorrinBodyType = unit.corrinBodyType;
                CorrinFace = unit.corrinFace;
                CorrinHair = unit.corrinHair;
                CorrinDetail = unit.corrinDetail;
                CorrinHairColor = unit.corrinHairColor;
            }
        }
    }
}

public class LeaderboardData
{
    private static LeaderboardData _instance;
    public static LeaderboardData Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new LeaderboardData();
            }
            return _instance;
        }
    }

    public List<Unit> RankedUnits { get; private set; } = new List<Unit>();
    public Dictionary<int, LeaderboardBase.UnitScore> Scores { get; private set; } =
        new Dictionary<int, LeaderboardBase.UnitScore>();
    public Dictionary<int, List<EnemyDefeatData>> DefeatedEnemiesTracker { get; private set; } =
        new Dictionary<int, List<EnemyDefeatData>>();

    // Changed to use unitID instead of GetInstanceID and store EliminatorData
    public Dictionary<int, EliminatorData> EliminatorTracker { get; private set; } =
        new Dictionary<int, EliminatorData>();

    public bool HasData => RankedUnits != null && RankedUnits.Count > 0;

    public int NumberOfWinners { get; set; } = 1;

    public void SetLeaderboardData(
        List<Unit> rankedUnits,
        Dictionary<int, LeaderboardBase.UnitScore> scores,
        int numWinners = 1,
        Dictionary<int, EliminatorData> eliminators = null,
        Dictionary<int, List<EnemyDefeatData>> defeatedEnemies = null) // Add this parameter
    {
        RankedUnits = new List<Unit>(rankedUnits);
        Scores = new Dictionary<int, LeaderboardBase.UnitScore>(scores);
        NumberOfWinners = numWinners;

        // Log score data
        Debug.Log($"Setting leaderboard data with {scores.Count} scores");
        foreach (var scorePair in scores)
        {
            Debug.Log($"Score entry - ID: {scorePair.Key}, Score: {scorePair.Value.TotalScore}");
        }

        // Clear existing eliminator data
        EliminatorTracker.Clear();

        // Add new eliminator data if provided
        if (eliminators != null && eliminators.Count > 0)
        {
            Debug.Log($"Setting elimination data with {eliminators.Count} entries");
            foreach (var pair in eliminators)
            {
                EliminatorTracker[pair.Key] = pair.Value;
                Debug.Log($"Added elimination data: Unit ID {pair.Key} eliminated by {pair.Value.Name}");
            }
        } else
        {
            Debug.LogWarning("No elimination data provided to LeaderboardData");
        }

        DefeatedEnemiesTracker.Clear();

        // Add new defeated enemies data if provided
        if (defeatedEnemies != null && defeatedEnemies.Count > 0)
        {
            Debug.Log($"Setting defeated enemies data with {defeatedEnemies.Count} entries");
            foreach (var pair in defeatedEnemies)
            {
                DefeatedEnemiesTracker[pair.Key] = new List<EnemyDefeatData>(pair.Value);
                Debug.Log($"Unit ID {pair.Key} defeated {pair.Value.Count} enemies");
            }
        } else
        {
            Debug.LogWarning("No defeated enemies data provided to LeaderboardData");
        }
    }

    public void Reset()
    {
        RankedUnits.Clear();
        Scores.Clear();
        EliminatorTracker.Clear();
        DefeatedEnemiesTracker.Clear(); // Add this
    }
}