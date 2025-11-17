using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class UnitPlacer : MonoBehaviour
{
    public GameObject testUnitPrefab;
    public GameObject deathBehaviorPrefab;
    public GameObject respawnBehaviorPrefab;
    public GameObject stunBehaviorPrefab;
    public List<Vector3> startingPositions;
    public ArmyManager armyManager;
    public int maxSpawn;
    public int team2Count;
    public List<Color> teamColors;
    public float surviveEnemyBonus = 0.5f;
    private int numberOfTeams;
    private int[] validTeamIds;
    private bool isTeamBattleMode;

    private readonly HashSet<string> singleTeamGameModes = new HashSet<string>
    {
        "battle royale",
        "battleroyale",
        "hot potato"
    };

    private readonly HashSet<string> hpScaledGameModes = new HashSet<string>
    {
        "battle royale",
        "team battle",
        "survive"
    };

    void Start()
    {
        TestUnitLoad();
        Application.targetFrameRate = 60;
    }

    void Update()
    {

    }

    public void TestUnitLoad()
    {
        float spawnCount = 0;
        int currentID = 0;

        // Get settings from MenuSettings
        int team1Size = MenuSettings.Instance.playerNames.Length;
        armyManager.CreateRuleset();
        string ruleString = MenuSettings.Instance.selectedGameMode;

        // Shuffle player names to avoid position advantages
        string[] playerNames = (string[])MenuSettings.Instance.playerNames.Clone();
        ShuffleArray(playerNames);

        bool isSeizeMode = ruleString == "Seize";
        bool isSurviveMode = ruleString == "Survive";
        bool isSingleTeamMode = IsSingleTeamMode(ruleString);
        bool isHPScaledMode = IsHPScaledMode(ruleString);
        bool isHotPotatoMode = ruleString == "Hot Potato";
        bool isTeamBattleMode = ruleString == "Team Battle";
        bool bossPlaced = false;

        // Determine number of teams and team IDs for Team Battle mode
        int numberOfTeams = 0;
        int[] validTeamIds = null;

        if (isTeamBattleMode)
        {
            numberOfTeams = TeamBattleUtility.DetermineNumberOfTeams(
                team1Size,
                MenuSettings.Instance.numberOfWinners
            );

            if (numberOfTeams == -1)
            {
                Debug.LogError("Invalid team configuration for Team Battle mode. Falling back to free-for-all.");
                isTeamBattleMode = false;
                isSingleTeamMode = true;
            } else
            {
                validTeamIds = TeamBattleUtility.GetValidTeamIds(numberOfTeams);
                Debug.Log($"Team Battle using team IDs: {string.Join(", ", validTeamIds)}");
            }
        }
        this.numberOfTeams = numberOfTeams;
        this.validTeamIds = validTeamIds;
        this.isTeamBattleMode = isTeamBattleMode;

        // Load the composition from MenuSettings
        string selectedLevel = MenuSettings.Instance.selectedLevel;
        if (!string.IsNullOrEmpty(selectedLevel))
        {
            if (!TeamClassComposition.Instance.LoadCompositionByName(selectedLevel))
            {
                Debug.LogWarning($"Failed to load level '{selectedLevel}', falling back to test distribution");
                TeamClassComposition.Instance.SetTestDistribution();
            }
        } else
        {
            Debug.Log("No level selected, using test distribution");
            TeamClassComposition.Instance.SetTestDistribution();
        }

        TeamClassComposition.TeamComposition[] teamComps;

        if (isSingleTeamMode || isTeamBattleMode)
        {
            // For single team modes and team battle, only create one composition
            teamComps = new TeamClassComposition.TeamComposition[1];
            string randomArmyName;
            teamComps[0] = TeamClassComposition.Instance.GetTeamCompositionWithRandomFallback(1, team1Size, ruleString, out randomArmyName);

            // For random composition game modes, always use the random army name
            if (!string.IsNullOrEmpty(randomArmyName))
            {
                MenuSettings.Instance.armyName = randomArmyName;
                Debug.Log($"Using random army name: {randomArmyName} for {ruleString} mode");
            }
        } else
        {
            // Calculate team 2 relative strength for two-team modes
            float team2RelativeStrength = 1.0f;
            if (maxSpawn > 0 && team2Count > 0)
            {
                team2RelativeStrength = (float)team2Count / maxSpawn;
            }
            if (isSurviveMode)
            {
                team2RelativeStrength += surviveEnemyBonus;
            }

            // Use the new conditional random method for both teams
            teamComps = new TeamClassComposition.TeamComposition[2];

            // Team 1 (player team) - might use random if randomPlayerTeam is true
            string team1ArmyName;
            teamComps[0] = TeamClassComposition.Instance.GetTeamCompositionWithConditionalRandom(1, team1Size, team2RelativeStrength, ruleString, out team1ArmyName);

            // Team 2 (enemy team) - always uses level-based composition
            string team2ArmyName;
            teamComps[1] = TeamClassComposition.Instance.GetTeamCompositionWithConditionalRandom(2, team1Size, team2RelativeStrength, ruleString, out team2ArmyName);

            // Only override army name for modes that always use random (already handled in GetTeamCompositionWithConditionalRandom)
            bool alwaysRandomMode = ruleString.ToLower() == "battle royale" ||
                                   ruleString.ToLower() == "battleroyale" ||
                                   ruleString.ToLower() == "hot potato" ||
                                   ruleString.ToLower() == "team battle";

            if (alwaysRandomMode && !string.IsNullOrEmpty(team1ArmyName))
            {
                MenuSettings.Instance.armyName = team1ArmyName;
                Debug.Log($"Using random army name: {team1ArmyName} for {ruleString} mode");
            }

            if (MenuSettings.Instance.streamMode == "Recruit" && MenuSettings.Instance.randomEnemyTeam)
            {
                MenuSettings.Instance.armyName = team2ArmyName;
            }
        }

        // Calculate positions for all units with class information
        UnitStartingData[] startingData = GetPositionsForGameMode(ruleString, teamComps);

        int playerNameIndex = 0;
        int playersPerTeam = isTeamBattleMode ? team1Size / numberOfTeams : 0;

        foreach (UnitStartingData startData in startingData)
        {
            Vector3 pos = Linalg.Vector2ToVector3(startData.Position);
            Unit unit = Instantiate(testUnitPrefab, pos, Quaternion.identity).GetComponent<Unit>();

            // Handle team assignment
            if (isSingleTeamMode && !isTeamBattleMode)
            {
                unit.teamID = 0;

                // Assign player name to all units in single team modes
                if (playerNameIndex < playerNames.Length)
                {
                    unit.SetPlayerName(playerNames[playerNameIndex]);
                    playerNameIndex++;
                }
            } else if (isTeamBattleMode)
            {
                // Assign teams using valid team IDs
                int teamIndex = playerNameIndex / playersPerTeam;
                if (teamIndex < validTeamIds.Length)
                {
                    unit.teamID = validTeamIds[teamIndex];
                } else
                {
                    // Handle edge case if player distribution is uneven
                    Debug.LogError($"Team index {teamIndex} exceeds valid team count. Assigning to last team.");
                    unit.teamID = validTeamIds[validTeamIds.Length - 1];
                }

                if (playerNameIndex < playerNames.Length)
                {
                    unit.SetPlayerName(playerNames[playerNameIndex]);
                    playerNameIndex++;
                }
            } else
            {
                unit.teamID = startData.TeamId;

                // Assign player name to team 1 units only in multi-team modes
                if (startData.TeamId == 1 && playerNameIndex < playerNames.Length)
                {
                    unit.SetPlayerName(playerNames[playerNameIndex]);
                    playerNameIndex++;
                }
            }

            unit.unitID = currentID;
            unit.unitName = startData.UnitName;
            unit.scaleFactor = startData.ScaleFactor;

            UnitDeathBehavior deathBehavior;
            if (unit.teamID == 1 && !isSurviveMode && !isSingleTeamMode && !isTeamBattleMode)
            {
                deathBehavior = Instantiate(respawnBehaviorPrefab, unit.transform).GetComponent<UnitDeathBehavior>();
            } else if (isHotPotatoMode)
            {
                deathBehavior = Instantiate(stunBehaviorPrefab, unit.transform).GetComponent<UnitDeathBehavior>();
            } else
            {
                deathBehavior = Instantiate(deathBehaviorPrefab, unit.transform).GetComponent<UnitDeathBehavior>();
            }
            unit.deathBehavior = deathBehavior;

            ClassDataManager.Instance.ApplyClassAndLevel(unit, (UnitClass)startData.UnitClass, TeamClassComposition.Instance.GetCurrentLevel());
            currentID++;
            unit.spriteManager.weaponFactory.SwitchWeapon(unit, unit.unitClass.preferredWeapon);
            unit.transform.localScale = Vector3.one * unit.scaleFactor;

            // Set team colors
            if (isTeamBattleMode && unit.teamID > 0)
            {
                // For team battle, check if we have a predefined color for this team ID
                if (unit.teamID < teamColors.Count)
                {
                    // Use predefined color
                    unit.spriteManager.unitOutline.color = teamColors[unit.teamID];
                } else
                {
                    // Generate color only for teams outside the predefined list
                    // Find the index of this team ID in the valid team IDs array
                    int colorIndex = System.Array.IndexOf(validTeamIds, unit.teamID);
                    if (colorIndex >= 0)
                    {
                        // Generate a color based on position in the valid teams array
                        // Offset by the number of predefined colors to avoid similar hues
                        float hueOffset = (float)teamColors.Count / numberOfTeams;
                        float hue = hueOffset + (float)colorIndex / numberOfTeams;
                        hue = hue % 1.0f; // Wrap around if necessary
                        unit.spriteManager.unitOutline.color = Color.HSVToRGB(hue, 0.8f, 1f);
                    } else
                    {
                        // Fallback color
                        unit.spriteManager.unitOutline.color = Color.white;
                    }
                }
            } else if (unit.teamID < teamColors.Count)
            {
                // For non-team battle modes, use predefined colors
                unit.spriteManager.unitOutline.color = teamColors[unit.teamID];
            } else
            {
                // For team 0 (no team) or teams beyond the color list, give each unit a unique random color
                System.Random rng = GameManager.GetInstance().rng;
                unit.spriteManager.unitOutline.color = new Color(
                    (float)rng.NextDouble(),
                    (float)rng.NextDouble(),
                    (float)rng.NextDouble(),
                    1);
            }

            // Add appropriate AI script based on unit type and game mode
            if (isSingleTeamMode || isTeamBattleMode)
            {
                // For Battle Royale and Team Battle, all units get default AI
                unit.gameObject.AddComponent<DefaultUnitAI>();
            } else if (isSeizeMode && startData.TeamId == 1)
            {
                unit.gameObject.AddComponent<SeizeUnitAI>();
            } else if (isSeizeMode && !bossPlaced && unit.teamID == 2 && unit.scaleFactor >= 1.5f)
            {
                // This is a boss unit in seize mode - make it stationary
                if (unit.velocityManager != null && unit.velocityManager.rb != null)
                {
                    unit.velocityManager.rb.constraints = RigidbodyConstraints2D.FreezePosition;
                    Debug.Log($"Locked position for boss unit: {unit.unitName} at {pos}");
                }
                unit.gameObject.AddComponent<StationaryUnitAI>();
                bossPlaced = true;
            } else
            {
                unit.gameObject.AddComponent<DefaultUnitAI>();
            }
            unit.aiScript.state = AIState.Idle;
            if (isHPScaledMode)
            {
                ApplyHPScaling(unit);
            }

            CharacterAssetLoader.Instance.SetupCharacterAssets(unit.gameObject, startData.UnitName);
            
            armyManager.AddUnit(unit);
            spawnCount++;
        }

        if (isSurviveMode)
        {
            GameObject reinforcementsObj = new GameObject("SurviveReinforcements");
            var reinforcements = reinforcementsObj.AddComponent<SurviveReinforcements>();
            reinforcements.Initialize(
                armyManager,
                testUnitPrefab,
                deathBehaviorPrefab,
                teamColors,
                currentID
            );
        }

        DeathBehaviorRespawn.ClearRespawnQueue();
    }

    public void CreateTestUnit(Unit unit)
    {
        if (new System.Random().Next(3) == 1)
        {
            ClassDataManager.Instance.ApplyClassAndLevel(unit, "Bow Knight", 40);
        } else if (new System.Random().Next(2) == 1)
        {
            ClassDataManager.Instance.ApplyClassAndLevel(unit, "Knight", 10);
        } else
        {
            ClassDataManager.Instance.ApplyClassAndLevel(unit, "Dark Falcon", 20);
        }

        unit.UpdateMovementParameters();
    }

    private void ShuffleArray<T>(T[] array)
    {
        System.Random rng = new System.Random();
        int n = array.Length;

        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            T temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }

    private bool IsSingleTeamMode(string gameMode)
    {
        return singleTeamGameModes.Contains(gameMode.ToLower());
    }

    private bool IsHPScaledMode(string gameMode)
    {
        return hpScaledGameModes.Contains(gameMode.ToLower());
    }

    private void ApplyHPScaling(Unit unit)
    {
        float scaleFactor = 1f;
        float playerCount = (float)MenuSettings.Instance.playerNames.Length;
        float numberOfWinners = (float)MenuSettings.Instance.numberOfWinners;
        if (numberOfWinners >= playerCount / 2)
        {
            scaleFactor = 4f * (numberOfWinners / playerCount) - 1;
        }
        unit.ScaleHP(scaleFactor);
    }

    public UnitStartingData[] GetPositionsForGameMode(string gameMode, TeamClassComposition.TeamComposition[] teamComps)
    {
        switch (gameMode.ToLower())
        {
            case "seize":
                return StartingPositionsSeize.CalculateSeizePositions(teamComps);
            case "battle royale":
            case "battleroyale":
            case "hot potato":
                return StartingPositionsCommon.CalculateBattleRoyalePositions(teamComps[0]);
            case "team battle":
                if (this.isTeamBattleMode && this.numberOfTeams > 0 && this.validTeamIds != null)
                {
                    return StartingPositionsTeams.CalculateTeamBattlePositions(
                        teamComps[0],
                        this.numberOfTeams,
                        this.validTeamIds
                    );
                } else
                {
                    return StartingPositionsCommon.CalculateBattleRoyalePositions(teamComps[0]);
                }
            case "survive":
                return StartingPositionsSurvive.CalculateSurvivePositions(teamComps);
            default:
                return StartingPositionsCommon.CalculateTeamBattlePositions(teamComps);
        }
    }
}