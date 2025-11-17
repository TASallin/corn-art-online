using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class StartingPositionsSurvive
{
    public static UnitStartingData[] CalculateSurvivePositions(TeamClassComposition.TeamComposition[] teamCompositions)
    {
        // Get total counts for each team
        int team1Count = teamCompositions[0].classDistribution.Sum(cd => cd.count);
        int team2Count = teamCompositions[1].classDistribution.Sum(cd => cd.count);

        // Get map bounds from GameManager
        float xBound = GameManager.GetInstance().xBound;
        float yBound = GameManager.GetInstance().yBound;

        // Minimum distance between units
        float minDistance = 0.63f * 2 * 1.1f;

        List<UnitStartingData> allUnits = new List<UnitStartingData>();

        // Calculate center area for team 1 based on their unit count
        float centerAreaRatio = CalculateCenterAreaRatio(team1Count, team2Count);
        float centerRadius = Mathf.Min(xBound, yBound) * centerAreaRatio;

        // Generate positions for Team 1 (survivors - in center)
        List<UnitStartingData> team1Units = GenerateTeam1CenterPositions(
            teamCompositions[0],
            centerRadius,
            minDistance
        );

        // Generate positions for Team 2 (attackers - surrounding the center)
        List<UnitStartingData> team2Units = GenerateTeam2SurroundingPositions(
            teamCompositions[1],
            centerRadius,
            xBound,
            yBound,
            minDistance
        );

        // Combine both teams
        allUnits.AddRange(team1Units);
        allUnits.AddRange(team2Units);

        return allUnits.ToArray();
    }

    private static float CalculateCenterAreaRatio(int team1Count, int team2Count)
    {
        // Improved ratio calculation that considers team sizes and map bounds
        GameManager gm = GameManager.GetInstance();
        float xBound = gm.xBound;
        float yBound = gm.yBound;
        float minDistance = 0.63f * 2 * 1.1f;

        // Calculate rough area needed for team 1
        float team1Area = team1Count * minDistance * minDistance * 2f;
        float team1Radius = Mathf.Sqrt(team1Area / Mathf.PI);

        // Base ratio starts at 0.25 (25% of map)
        float baseRatio = 0.25f;

        // Adjust based on team 1 size - ensure they have enough space
        if (team1Count > 20) baseRatio += 0.05f;
        if (team1Count > 40) baseRatio += 0.05f;
        if (team1Count > 60) baseRatio += 0.1f;

        // Ensure the center area doesn't take up too much of the map
        // Leave enough space for team 2 formations around the edges
        float maxRatio = 0.4f;

        // Also ensure team 1 has enough space for their formation
        float minRequiredRatio = team1Radius / Mathf.Min(xBound, yBound);

        return Mathf.Clamp(baseRatio, minRequiredRatio, maxRatio);
    }

    private static List<UnitStartingData> GenerateTeam1CenterPositions(
        TeamClassComposition.TeamComposition teamComp,
        float centerRadius,
        float minDistance)
    {
        var rng = GameManager.GetInstance().rng;

        // 75% chance to use mini formations, 25% for standard formation
        if (rng.NextDouble() < 0.75)
        {
            // Flatten all units for mini formations
            List<UnitStartingData> allUnits = new List<UnitStartingData>();
            foreach (var classCount in teamComp.classDistribution)
            {
                allUnits.AddRange(classCount.unitData);
            }

            return StartingPositionsMiniFormations.GenerateMiniFormations(
                allUnits,
                -centerRadius, centerRadius,
                -centerRadius, centerRadius,
                minDistance,
                true // Team 1 faces right
            );
        } else
        {
            // Use standard formation for center area
            return StartingPositionsCommon.GenerateFormationForSingleTeam(
                teamComp,
                -centerRadius,
                centerRadius,
                -centerRadius,
                centerRadius,
                minDistance,
                true // Team 1 faces right
            );
        }
    }

    private static List<UnitStartingData> GenerateTeam2SurroundingPositions(
        TeamClassComposition.TeamComposition teamComp,
        float centerRadius,
        float xBound,
        float yBound,
        float minDistance)
    {
        var rng = GameManager.GetInstance().rng;

        // Flatten all units
        List<UnitStartingData> allUnits = new List<UnitStartingData>();
        foreach (var classCount in teamComp.classDistribution)
        {
            allUnits.AddRange(classCount.unitData);
        }

        // Choose formation strategy
        float formationChoice = (float)rng.NextDouble();

        if (formationChoice < 0.4f)
        {
            // Ring formation - units arranged in concentric circles around the center
            return GenerateRingFormation(allUnits, centerRadius, xBound, yBound, minDistance);
        } else if (formationChoice < 0.7f)
        {
            // Sector formation - divide outer area into sectors
            return GenerateSectorFormation(allUnits, centerRadius, xBound, yBound, minDistance);
        } else
        {
            // Mixed formation - combination of rings and scattered units
            return GenerateMixedSurroundingFormation(allUnits, centerRadius, xBound, yBound, minDistance);
        }
    }

    private static List<UnitStartingData> GenerateRingFormation(
    List<UnitStartingData> units,
    float centerRadius,
    float xBound,
    float yBound,
    float minDistance)
    {
        List<UnitStartingData> positionedUnits = new List<UnitStartingData>();
        Vector2 center = Vector2.zero;

        // Calculate spacing between rings - ensure good separation
        float ringSpacing = Mathf.Max(minDistance * 2.5f, 3f);

        // Calculate number of concentric rings needed
        float maxRadius = Mathf.Min(xBound, yBound) * 0.9f; // Leave 10% margin

        // Ensure first ring is far enough from center
        float firstRingRadius = centerRadius + ringSpacing;
        int maxRings = Mathf.FloorToInt((maxRadius - firstRingRadius) / ringSpacing) + 1;
        maxRings = Mathf.Max(1, maxRings);

        // Distribute units among rings
        int[] unitsPerRing = DistributeUnitsAmongRings(units.Count, maxRings);

        int unitIndex = 0;
        for (int ring = 0; ring < maxRings && unitIndex < units.Count; ring++)
        {
            float ringRadius = firstRingRadius + (ring * ringSpacing);
            if (ringRadius > maxRadius) break;

            int unitsInThisRing = unitsPerRing[ring];
            if (unitsInThisRing == 0) continue;

            List<Vector2> ringPositions = GenerateRingPositions(
                center,
                ringRadius,
                unitsInThisRing,
                xBound,
                yBound,
                minDistance
            );

            // Assign units to positions
            for (int i = 0; i < Mathf.Min(unitsInThisRing, ringPositions.Count) && unitIndex < units.Count; i++)
            {
                var unitData = units[unitIndex];
                positionedUnits.Add(new UnitStartingData(
                    ringPositions[i],
                    unitData.TeamId,
                    unitData.UnitClass,
                    unitData.UnitName,
                    unitData.ScaleFactor
                ));
                unitIndex++;
            }
        }

        return positionedUnits;
    }

    private static List<UnitStartingData> GenerateSectorFormation(
        List<UnitStartingData> units,
        float centerRadius,
        float xBound,
        float yBound,
        float minDistance)
    {
        List<UnitStartingData> positionedUnits = new List<UnitStartingData>();
        var rng = GameManager.GetInstance().rng;

        // Divide the area into 4-8 sectors
        int sectorCount = rng.Next(4, 9);
        float sectorAngle = 2f * Mathf.PI / sectorCount;

        // Distribute units among sectors
        int[] unitsPerSector = DistributeUnitsAmongSectors(units.Count, sectorCount);

        int unitIndex = 0;
        for (int sector = 0; sector < sectorCount && unitIndex < units.Count; sector++)
        {
            float startAngle = sector * sectorAngle;
            float endAngle = (sector + 1) * sectorAngle;

            int unitsInSector = unitsPerSector[sector];
            if (unitsInSector == 0) continue;

            // Generate positions for this sector
            List<Vector2> sectorPositions = GenerateSectorPositions(
                startAngle,
                endAngle,
                centerRadius,
                xBound,
                yBound,
                unitsInSector,
                minDistance
            );

            // Assign units to positions
            for (int i = 0; i < Mathf.Min(unitsInSector, sectorPositions.Count) && unitIndex < units.Count; i++)
            {
                var unitData = units[unitIndex];
                positionedUnits.Add(new UnitStartingData(
                    sectorPositions[i],
                    unitData.TeamId,
                    unitData.UnitClass,
                    unitData.UnitName,
                    unitData.ScaleFactor
                ));
                unitIndex++;
            }
        }

        return positionedUnits;
    }

    private static List<UnitStartingData> GenerateMixedSurroundingFormation(
        List<UnitStartingData> units,
        float centerRadius,
        float xBound,
        float yBound,
        float minDistance)
    {
        var rng = GameManager.GetInstance().rng;

        // Split units between ring formation and scattered placement
        int ringUnits = (int)(units.Count * rng.NextDouble() * 0.4 + 0.4); // 40-80% in rings
        List<UnitStartingData> ringUnitsList = units.Take(ringUnits).ToList();
        List<UnitStartingData> scatteredUnitsList = units.Skip(ringUnits).ToList();

        List<UnitStartingData> positionedUnits = new List<UnitStartingData>();

        // Generate ring positions
        if (ringUnitsList.Count > 0)
        {
            positionedUnits.AddRange(GenerateRingFormation(
                ringUnitsList, centerRadius, xBound, yBound, minDistance));
        }

        // Generate scattered positions
        if (scatteredUnitsList.Count > 0)
        {
            positionedUnits.AddRange(GenerateScatteredOuterPositions(
                scatteredUnitsList, centerRadius, xBound, yBound, minDistance));
        }

        return positionedUnits;
    }

    private static List<UnitStartingData> GenerateScatteredOuterPositions(
        List<UnitStartingData> units,
        float centerRadius,
        float xBound,
        float yBound,
        float minDistance)
    {
        List<UnitStartingData> positionedUnits = new List<UnitStartingData>();
        var rng = GameManager.GetInstance().rng;

        int maxAttempts = units.Count * 100;
        int attempts = 0;
        List<Vector2> placedPositions = new List<Vector2>();

        foreach (var unit in units)
        {
            bool placed = false;

            while (!placed && attempts < maxAttempts)
            {
                // Generate random position outside center radius
                Vector2 candidate = GenerateRandomOuterPosition(centerRadius, xBound, yBound, rng);

                // Check distance from center
                if (Vector2.Distance(candidate, Vector2.zero) <= centerRadius + minDistance)
                {
                    attempts++;
                    continue;
                }

                // Check distance from other placement positions
                bool tooClose = false;
                foreach (var pos in placedPositions)
                {
                    if (Vector2.Distance(candidate, pos) < minDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    positionedUnits.Add(new UnitStartingData(
                        candidate,
                        unit.TeamId,
                        unit.UnitClass,
                        unit.UnitName,
                        unit.ScaleFactor
                    ));
                    placedPositions.Add(candidate);
                    placed = true;
                }

                attempts++;
            }
        }

        return positionedUnits;
    }

    private static Vector2 GenerateRandomOuterPosition(float centerRadius, float xBound, float yBound, System.Random rng)
    {
        // Generate position outside the center circle
        Vector2 position;
        do
        {
            float x = (float)(rng.NextDouble() * (2 * xBound) - xBound);
            float y = (float)(rng.NextDouble() * (2 * yBound) - yBound);
            position = new Vector2(x, y);
        } while (Vector2.Distance(position, Vector2.zero) <= centerRadius);

        return position;
    }

    private static List<Vector2> GenerateRingPositions(
    Vector2 center,
    float radius,
    int unitCount,
    float xBound,
    float yBound,
    float minDistance)
    {
        List<Vector2> positions = new List<Vector2>();
        var rng = GameManager.GetInstance().rng;

        // Calculate circumference and check if all units can fit
        float circumference = 2f * Mathf.PI * radius;
        float requiredSpace = unitCount * minDistance;

        if (circumference < requiredSpace)
        {
            // Not all units fit in perfect circle, distribute with some randomness
            radius = requiredSpace / (2f * Mathf.PI);
        }

        // Place units around the ring with some randomization
        for (int i = 0; i < unitCount; i++)
        {
            float angle = (i * 2f * Mathf.PI) / unitCount;

            // Add some angular randomization
            float angleVariation = (float)(rng.NextDouble() * 0.2f - 0.1f);
            angle += angleVariation;

            // Add some radial randomization
            float radiusVariation = (float)(rng.NextDouble() * minDistance * 0.5f - minDistance * 0.25f);
            float actualRadius = radius + radiusVariation;

            float x = center.x + actualRadius * Mathf.Cos(angle);
            float y = center.y + actualRadius * Mathf.Sin(angle);

            // Ensure position is within bounds with proper margin
            float margin = minDistance;
            x = Mathf.Clamp(x, -xBound + margin, xBound - margin);
            y = Mathf.Clamp(y, -yBound + margin, yBound - margin);

            positions.Add(new Vector2(x, y));
        }

        return positions;
    }

    private static List<Vector2> GenerateSectorPositions(
        float startAngle,
        float endAngle,
        float innerRadius,
        float xBound,
        float yBound,
        int unitCount,
        float minDistance)
    {
        List<Vector2> positions = new List<Vector2>();
        var rng = GameManager.GetInstance().rng;

        float maxRadius = Mathf.Min(xBound, yBound);
        float outerRadius = maxRadius - minDistance;

        for (int i = 0; i < unitCount; i++)
        {
            // Random angle within sector
            float angle = (float)(startAngle + rng.NextDouble() * (endAngle - startAngle));

            // Random radius between inner and outer bounds
            float radius = (float)(innerRadius + minDistance + rng.NextDouble() * (outerRadius - innerRadius - minDistance));

            float x = radius * Mathf.Cos(angle);
            float y = radius * Mathf.Sin(angle);

            // Ensure position is within bounds
            x = Mathf.Clamp(x, -xBound + minDistance / 2, xBound - minDistance / 2);
            y = Mathf.Clamp(y, -yBound + minDistance / 2, yBound - minDistance / 2);

            positions.Add(new Vector2(x, y));
        }

        return positions;
    }

    private static int[] DistributeUnitsAmongRings(int totalUnits, int ringCount)
    {
        int[] distribution = new int[ringCount];
        var rng = GameManager.GetInstance().rng;

        // Base distribution
        int basePerRing = totalUnits / ringCount;
        int remainder = totalUnits % ringCount;

        for (int i = 0; i < ringCount; i++)
        {
            distribution[i] = basePerRing;
            if (remainder > 0)
            {
                distribution[i]++;
                remainder--;
            }
        }

        // Add some randomization - prefer inner rings slightly
        for (int i = 0; i < ringCount - 1; i++)
        {
            if (distribution[i] > 1 && rng.NextDouble() < 0.3)
            {
                distribution[i]--;
                distribution[i + 1]++;
            }
        }

        return distribution;
    }

    private static int[] DistributeUnitsAmongSectors(int totalUnits, int sectorCount)
    {
        int[] distribution = new int[sectorCount];
        var rng = GameManager.GetInstance().rng;

        // Base distribution
        int basePerSector = totalUnits / sectorCount;
        int remainder = totalUnits % sectorCount;

        for (int i = 0; i < sectorCount; i++)
        {
            distribution[i] = basePerSector;
            if (remainder > 0)
            {
                distribution[i]++;
                remainder--;
            }
        }

        // Add variance to distribution
        for (int i = 0; i < sectorCount; i++)
        {
            if (i < sectorCount - 1 && distribution[i] > 1 && rng.NextDouble() < 0.4)
            {
                int transfer = rng.Next(1, Mathf.Min(3, distribution[i]));
                distribution[i] -= transfer;
                distribution[(i + 1) % sectorCount] += transfer;
            }
        }

        return distribution;
    }
}