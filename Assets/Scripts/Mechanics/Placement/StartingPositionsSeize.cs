using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class StartingPositionsSeize
{
    public static UnitStartingData[] CalculateSeizePositions(TeamClassComposition.TeamComposition[] teamCompositions)
    {
        // Get the target point from the SeizeGameOverCondition
        Vector2 targetPoint = GetSeizeTargetPoint();

        List<UnitStartingData> allUnits = new List<UnitStartingData>();

        // Process Team 1 (player team) - use left half of map
        List<UnitStartingData> team1Units = GenerateTeam1Positions(teamCompositions[0]);
        allUnits.AddRange(team1Units);

        // Process Team 2 (enemy team) with boss at target
        List<UnitStartingData> team2Units = GenerateTeam2PositionsWithBoss(teamCompositions[1], targetPoint);
        allUnits.AddRange(team2Units);

        return allUnits.ToArray();
    }

    private static Vector2 GetSeizeTargetPoint()
    {
        // Get the SeizeGameOverCondition and its target point
        var seizeCondition = GameObject.FindObjectOfType<SeizeGameOverCondition>();
        if (seizeCondition != null)
        {
            return seizeCondition.GetTargetPoint();
        }

        // Fallback if we can't find the condition
        GameManager gm = GameManager.GetInstance();
        float xPos = gm.xBound * UnityEngine.Random.Range(0.5f, 1.0f);
        float yPos = UnityEngine.Random.Range(-gm.yBound * 0.8f, gm.yBound * 0.8f);
        return new Vector2(xPos, yPos);
    }

    private static List<UnitStartingData> GenerateTeam1Positions(TeamClassComposition.TeamComposition teamComp)
    {
        GameManager gm = GameManager.GetInstance();
        float minDistance = 0.63f * 2 * 1.1f;

        // Team 1 uses left half of the map with individual roll for mini formations
        return StartingPositionsCommon.GenerateFormationForSingleTeam(
            teamComp,
            -gm.xBound,
            0,
            -gm.yBound,
            gm.yBound,
            minDistance,
            true // facing right
        );
    }

    private static List<UnitStartingData> GenerateTeam2PositionsWithBoss(
        TeamClassComposition.TeamComposition teamComp,
        Vector2 targetPoint)
    {
        List<UnitStartingData> allUnits = new List<UnitStartingData>();

        // Find the boss unit (first unit with scale factor >= 1.5)
        UnitStartingData bossUnit = null;
        List<UnitStartingData> regularUnits = new List<UnitStartingData>();

        foreach (var classCount in teamComp.classDistribution)
        {
            foreach (var unitData in classCount.unitData)
            {
                if (bossUnit == null && unitData.ScaleFactor >= 1.5f)
                {
                    // Place boss at target point
                    bossUnit = new UnitStartingData(
                        targetPoint,
                        unitData.TeamId,
                        unitData.UnitClass,
                        unitData.UnitName,
                        unitData.ScaleFactor
                    );
                } else
                {
                    regularUnits.Add(unitData);
                }
            }
        }

        // Add boss unit first
        if (bossUnit != null)
        {
            allUnits.Add(bossUnit);
        }

        // Generate positions for remaining units
        if (regularUnits.Count > 0)
        {
            List<UnitStartingData> positionedUnits;

            // 50% chance to use mixed formation
            if (UnityEngine.Random.value < 0.5f)
            {
                positionedUnits = GenerateMixedFormation(regularUnits, targetPoint);
            } else
            {
                positionedUnits = GenerateFullDefensiveFormation(regularUnits, targetPoint);
            }

            allUnits.AddRange(positionedUnits);
        }

        return allUnits;
    }

    private static List<UnitStartingData> GenerateMixedFormation(
    List<UnitStartingData> units,
    Vector2 targetPoint)
    {
        var rng = GameManager.GetInstance().rng;

        // Split units between defensive formation and mini formations
        // Random percentage between 30% and 70% in defensive formation
        float defensivePercentage = (float)(rng.NextDouble() * 0.4 + 0.3);
        int defensiveCount = Mathf.RoundToInt(units.Count * defensivePercentage);

        List<UnitStartingData> defensiveUnits = units.Take(defensiveCount).ToList();
        List<UnitStartingData> miniFormationUnits = units.Skip(defensiveCount).ToList();

        List<UnitStartingData> allPositioned = new List<UnitStartingData>();

        // Place defensive units around target
        if (defensiveUnits.Count > 0)
        {
            allPositioned.AddRange(GenerateDefensiveFormationUnits(defensiveUnits, targetPoint));
        }

        // Use mini formations for remaining units
        if (miniFormationUnits.Count > 0)
        {
            GameManager gm = GameManager.GetInstance();
            // Team 2's area but avoiding the defensive formation area
            float minDistance = 0.63f * 2 * 1.1f;
            List<UnitStartingData> miniPositions = StartingPositionsMiniFormations.GenerateMiniFormations(
                miniFormationUnits,
                0, // Team 2's left boundary
                gm.xBound,
                -gm.yBound,
                gm.yBound,
                minDistance,
                false // facing left
            );
            allPositioned.AddRange(miniPositions);
        }

        return allPositioned;
    }

    private static List<UnitStartingData> GenerateFullDefensiveFormation(
        List<UnitStartingData> units,
        Vector2 targetPoint)
    {
        return GenerateDefensiveFormationUnits(units, targetPoint);
    }

    private static List<UnitStartingData> GenerateDefensiveFormationUnits(
        List<UnitStartingData> units,
        Vector2 targetPoint)
    {
        List<UnitStartingData> positionedUnits = new List<UnitStartingData>();
        GameManager gm = GameManager.GetInstance();

        float minDistance = 0.63f * 2 * 1.1f;
        float areaRadius = Mathf.Min(gm.xBound, gm.yBound) * 0.4f;

        // Generate defensive positions
        List<Vector2> positions = GenerateDefensivePositions(
            units.Count,
            targetPoint,
            areaRadius,
            minDistance
        );

        for (int i = 0; i < Mathf.Min(units.Count, positions.Count); i++)
        {
            var unitData = units[i];
            positionedUnits.Add(new UnitStartingData(
                positions[i],
                unitData.TeamId,
                unitData.UnitClass,
                unitData.UnitName,
                unitData.ScaleFactor
            ));
        }

        return positionedUnits;
    }

    private static List<UnitStartingData> GenerateScatteredUnits(
        List<UnitStartingData> units,
        Vector2 targetPoint)
    {
        List<UnitStartingData> positionedUnits = new List<UnitStartingData>();
        GameManager gm = GameManager.GetInstance();

        // Define scatter area (team 2's side, avoiding target area)
        float minDistance = 0.63f * 2 * 1.1f;
        float targetClearRadius = minDistance * 4; // Keep some space around target

        // Team 2's area is right half of map
        float minX = 0;
        float maxX = gm.xBound;
        float minY = -gm.yBound;
        float maxY = gm.yBound;

        // Generate scattered positions
        List<Vector2> positions = GenerateScatteredPositions(
            units.Count,
            minX, maxX, minY, maxY,
            minDistance,
            targetPoint,
            targetClearRadius
        );

        for (int i = 0; i < Mathf.Min(units.Count, positions.Count); i++)
        {
            var unitData = units[i];
            positionedUnits.Add(new UnitStartingData(
                positions[i],
                unitData.TeamId,
                unitData.UnitClass,
                unitData.UnitName,
                unitData.ScaleFactor
            ));
        }

        return positionedUnits;
    }

    private static List<Vector2> GenerateDefensivePositions(
        int unitCount,
        Vector2 centerPoint,
        float maxRadius,
        float minDistance)
    {
        List<Vector2> positions = new List<Vector2>();

        int placed = 0;
        float currentRadius = minDistance * 2;

        while (placed < unitCount && currentRadius <= maxRadius)
        {
            float circumference = 2 * Mathf.PI * currentRadius;
            int unitsInCircle = Mathf.FloorToInt(circumference / minDistance);
            unitsInCircle = Mathf.Min(unitsInCircle, unitCount - placed);

            for (int i = 0; i < unitsInCircle; i++)
            {
                float angle = (i * 2f * Mathf.PI) / unitsInCircle;
                float x = centerPoint.x + currentRadius * Mathf.Cos(angle);
                float y = centerPoint.y + currentRadius * Mathf.Sin(angle);

                GameManager gm = GameManager.GetInstance();
                x = Mathf.Clamp(x, -gm.xBound + minDistance / 2, gm.xBound - minDistance / 2);
                y = Mathf.Clamp(y, -gm.yBound + minDistance / 2, gm.yBound - minDistance / 2);

                positions.Add(new Vector2(x, y));
                placed++;
            }

            currentRadius += minDistance * 1.5f;
        }

        return positions;
    }

    private static List<Vector2> GenerateScatteredPositions(
        int unitCount,
        float minX, float maxX, float minY, float maxY,
        float minDistance,
        Vector2 avoidPoint,
        float avoidRadius)
    {
        List<Vector2> positions = new List<Vector2>();
        int maxAttempts = unitCount * 50;
        int attempts = 0;

        while (positions.Count < unitCount && attempts < maxAttempts)
        {
            float x = UnityEngine.Random.Range(minX + minDistance / 2, maxX - minDistance / 2);
            float y = UnityEngine.Random.Range(minY + minDistance / 2, maxY - minDistance / 2);
            Vector2 candidate = new Vector2(x, y);

            // Check if too close to avoid point
            if (Vector2.Distance(candidate, avoidPoint) < avoidRadius)
            {
                attempts++;
                continue;
            }

            // Check distance from existing positions
            bool tooClose = false;
            foreach (var pos in positions)
            {
                if (Vector2.Distance(candidate, pos) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                positions.Add(candidate);
            }

            attempts++;
        }

        return positions;
    }

    private static List<UnitStartingData> GenerateTeamPositionsInArea(
        TeamClassComposition.TeamComposition teamComp,
        float minX, float maxX, float minY, float maxY,
        float minDistance)
    {
        // Create a temporary team composition array for the existing method
        var tempComps = new TeamClassComposition.TeamComposition[] { teamComp };

        // Use the common method but with our specific area bounds
        List<UnitStartingData> units = new List<UnitStartingData>();

        foreach (var classCount in teamComp.classDistribution)
        {
            foreach (var unitData in classCount.unitData)
            {
                units.Add(unitData);
            }
        }

        // Generate positions using common methods
        List<Vector2> positions = StartingPositionsCommon.GenerateTeamPositions(
            units.Count, minX, maxX, minY, maxY, minDistance);

        List<UnitStartingData> positionedUnits = new List<UnitStartingData>();
        for (int i = 0; i < Mathf.Min(units.Count, positions.Count); i++)
        {
            var unitData = units[i];
            positionedUnits.Add(new UnitStartingData(
                positions[i],
                unitData.TeamId,
                unitData.UnitClass,
                unitData.UnitName,
                unitData.ScaleFactor
            ));
        }

        return positionedUnits;
    }
}