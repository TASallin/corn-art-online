using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SurviveReinforcements : MonoBehaviour
{
    private float spawnInterval = 5f;
    private float nextSpawnTime;
    private ArmyManager armyManager;
    private GameObject testUnitPrefab;
    private GameObject deathBehaviorPrefab;
    private List<Color> teamColors;
    private int currentUnitID;

    public void Initialize(
        ArmyManager army,
        GameObject unitPrefab,
        GameObject deathBehavior,
        List<Color> colors,
        int startingUnitID)
    {
        armyManager = army;
        testUnitPrefab = unitPrefab;
        deathBehaviorPrefab = deathBehavior;
        teamColors = colors;
        currentUnitID = startingUnitID;
        nextSpawnTime = Time.time + spawnInterval;
        StartCoroutine(SpawnReinforcementsRoutine());
    }

    private IEnumerator SpawnReinforcementsRoutine()
    {
        while (true)
        {
            if (Time.time >= nextSpawnTime)
            {
                SpawnReinforcements();
                SpawnReinforcements();
                nextSpawnTime = Time.time + spawnInterval;
            }
            yield return new WaitForSeconds(1f); // Check every second
        }
    }

    private void SpawnReinforcements()
    {
        // Randomly determine number of units (2-10)
        int unitCount = Random.Range(2, 11);

        // Generate only random units for team 2
        TeamClassComposition.TeamComposition reinforcementComp =
            TeamClassComposition.Instance.GenerateRandomOnlyComposition(2, unitCount);

        // Choose a random edge of the map
        Vector2 spawnEdge = ChooseSpawnEdge();

        // Generate positions for the units using mini formations
        List<UnitStartingData> reinforcementUnits = new List<UnitStartingData>();

        // Flatten all units for mini formations
        foreach (var classCount in reinforcementComp.classDistribution)
        {
            reinforcementUnits.AddRange(classCount.unitData);
        }

        // Calculate spawn area based on chosen edge
        float minX, maxX, minY, maxY;
        float spawnAreaSize = 5f; // Size of the spawn area along the edge
        CalculateSpawnArea(spawnEdge, spawnAreaSize, out minX, out maxX, out minY, out maxY);

        // Generate formation positions
        List<UnitStartingData> positionedUnits = StartingPositionsMiniFormations.GenerateMiniFormations(
            reinforcementUnits,
            minX, maxX, minY, maxY,
            0.63f * 2 * 1.1f, // minDistance
            false // facing left/toward center
        );

        // Spawn the units
        foreach (UnitStartingData unitData in positionedUnits)
        {
            SpawnUnit(unitData);
        }
    }

    private Vector2 ChooseSpawnEdge()
    {
        GameManager gm = GameManager.GetInstance();
        float xBound = gm.xBound;
        float yBound = gm.yBound;

        int edge = Random.Range(0, 4);
        switch (edge)
        {
            case 0: return new Vector2(xBound, 0); // Right
            case 1: return new Vector2(-xBound, 0); // Left
            case 2: return new Vector2(0, yBound); // Top
            default: return new Vector2(0, -yBound); // Bottom
        }
    }

    private void CalculateSpawnArea(Vector2 spawnEdge, float areaSize, out float minX, out float maxX, out float minY, out float maxY)
    {
        GameManager gm = GameManager.GetInstance();
        float xBound = gm.xBound;
        float yBound = gm.yBound;

        if (Mathf.Abs(spawnEdge.x) == xBound) // Spawning from left or right
        {
            minX = spawnEdge.x - (spawnEdge.x > 0 ? areaSize : 0);
            maxX = spawnEdge.x + (spawnEdge.x < 0 ? areaSize : 0);
            minY = -areaSize / 2;
            maxY = areaSize / 2;
        } else // Spawning from top or bottom
        {
            minX = -areaSize / 2;
            maxX = areaSize / 2;
            minY = spawnEdge.y - (spawnEdge.y > 0 ? areaSize : 0);
            maxY = spawnEdge.y + (spawnEdge.y < 0 ? areaSize : 0);
        }
    }

    private void SpawnUnit(UnitStartingData unitData)
    {
        Vector3 pos = Linalg.Vector2ToVector3(unitData.Position);
        Unit unit = Instantiate(testUnitPrefab, pos, Quaternion.identity).GetComponent<Unit>();

        unit.teamID = unitData.TeamId;
        unit.unitID = currentUnitID++;
        unit.unitName = unitData.UnitName;
        unit.scaleFactor = unitData.ScaleFactor;

        var deathBehavior = Instantiate(deathBehaviorPrefab, unit.transform).GetComponent<UnitDeathBehavior>();
        unit.deathBehavior = deathBehavior;

        ClassDataManager.Instance.ApplyClassAndLevel(unit, (UnitClass)unitData.UnitClass, 20);
        unit.spriteManager.weaponFactory.SwitchWeapon(unit, unit.unitClass.preferredWeapon);
        unit.transform.localScale = Vector3.one * unit.scaleFactor;

        if (unit.teamID < teamColors.Count)
        {
            unit.spriteManager.unitOutline.color = teamColors[unit.teamID];
        } else
        {
            System.Random rng = GameManager.GetInstance().rng;
            unit.spriteManager.unitOutline.color = new Color((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble(), 1);
        }

        unit.gameObject.AddComponent<DefaultUnitAI>();
        CharacterAssetLoader.Instance.SetupCharacterAssets(unit.gameObject, unitData.UnitName);
        armyManager.AddUnit(unit);
    }
}