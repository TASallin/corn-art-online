using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class StartingPositionsCommon
{
    // Generates positions in a circular formation
    private static List<Vector2> GenerateCircularFormation(int unitCount, float xBound, float yBound, float minDistance)
    {
        List<Vector2> positions = new List<Vector2>();

        // Calculate radius based on bounds and unit count
        float radius = Mathf.Min(xBound, yBound) * 0.8f;

        // Ensure units don't overlap
        float circumference = 2f * Mathf.PI * radius;
        float requiredSpace = unitCount * minDistance;

        // If circumference can't fit all units, adjust radius
        if (circumference < requiredSpace)
        {
            radius = requiredSpace / (2f * Mathf.PI);
        }

        // Place units in a circle
        for (int i = 0; i < unitCount; i++)
        {
            float angle = (i * 2f * Mathf.PI) / unitCount;
            float x = radius * Mathf.Cos(angle);
            float y = radius * Mathf.Sin(angle);
            positions.Add(new Vector2(x, y));
        }

        return positions;
    }

    // Generates positions in a distributed grid that fully utilizes the available space
    private static List<Vector2> GenerateDistributedGridFormation(int unitCount, float xBound, float yBound, float minDistance)
    {
        List<Vector2> positions = new List<Vector2>();

        // Calculate approximate grid size based on square root of unit count
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(unitCount));

        // Calculate cell size based on available space
        float cellWidth = (2f * xBound) / gridSize;
        float cellHeight = (2f * yBound) / gridSize;

        // Ensure minimum distance
        cellWidth = Mathf.Max(cellWidth, minDistance);
        cellHeight = Mathf.Max(cellHeight, minDistance);

        // Calculate how many units can fit in each row/column
        int cols = Mathf.FloorToInt((2f * xBound) / cellWidth);
        int rows = Mathf.FloorToInt((2f * yBound) / cellHeight);

        // If we need more cells than available, recalculate with tighter spacing
        if (rows * cols < unitCount)
        {
            cellWidth = (2f * xBound) / Mathf.CeilToInt(Mathf.Sqrt(unitCount * (xBound / yBound)));
            cellHeight = (2f * yBound) / Mathf.CeilToInt(Mathf.Sqrt(unitCount * (yBound / xBound)));

            // Ensure we maintain minimum distance
            if (cellWidth < minDistance || cellHeight < minDistance)
            {
                // Use Poisson disc sampling if we can't fit with grid
                return GeneratePoissonDiscPositions(unitCount, xBound, yBound, minDistance);
            }

            cols = Mathf.FloorToInt((2f * xBound) / cellWidth);
            rows = Mathf.FloorToInt((2f * yBound) / cellHeight);
        }

        // Adjust centering of the grid
        float offsetX = -xBound + (2f * xBound - cols * cellWidth) / 2f + cellWidth / 2f;
        float offsetY = -yBound + (2f * yBound - rows * cellHeight) / 2f + cellHeight / 2f;

        // Create a list of all possible positions
        List<Vector2> allPositions = new List<Vector2>();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                float x = offsetX + col * cellWidth;
                float y = offsetY + row * cellHeight;

                // Add some minor randomization within the cell (optional)
                float randomizationFactor = 0.15f;
                float randX = Random.Range(-cellWidth * randomizationFactor, cellWidth * randomizationFactor);
                float randY = Random.Range(-cellHeight * randomizationFactor, cellHeight * randomizationFactor);

                Vector2 position = new Vector2(x + randX, y + randY);
                allPositions.Add(position);
            }
        }

        // Shuffle all positions
        ShufflePositions(allPositions);

        // Take only as many positions as we need
        for (int i = 0; i < Mathf.Min(unitCount, allPositions.Count); i++)
        {
            positions.Add(allPositions[i]);
        }

        return positions;
    }

    // Poisson disc sampling for more organic but well-spaced distribution
    private static List<Vector2> GeneratePoissonDiscPositions(int unitCount, float xBound, float yBound, float minDistance)
    {
        List<Vector2> positions = new List<Vector2>();
        List<Vector2> spawnPoints = new List<Vector2>();

        // Parameters for Poisson disc sampling
        float cellSize = minDistance / Mathf.Sqrt(2);

        int gridWidth = Mathf.CeilToInt(2f * xBound / cellSize);
        int gridHeight = Mathf.CeilToInt(2f * yBound / cellSize);

        Vector2[,] grid = new Vector2[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = Vector2.zero; // Mark as empty
            }
        }

        // Start with a random position
        Vector2 firstPosition = new Vector2(
            Random.Range(-xBound + minDistance, xBound - minDistance),
            Random.Range(-yBound + minDistance, yBound - minDistance)
        );

        int gridX = Mathf.FloorToInt((firstPosition.x + xBound) / cellSize);
        int gridY = Mathf.FloorToInt((firstPosition.y + yBound) / cellSize);

        grid[gridX, gridY] = firstPosition;
        positions.Add(firstPosition);
        spawnPoints.Add(firstPosition);

        // Generate points
        while (spawnPoints.Count > 0 && positions.Count < unitCount)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Vector2 spawnCenter = spawnPoints[spawnIndex];

            bool foundValidPoint = false;

            for (int i = 0; i < 30; i++) // Try 30 times to find a valid point
            {
                // Generate random point between minDistance and 2*minDistance from spawnCenter
                float angle = Random.Range(0f, 2f * Mathf.PI);
                float distance = Random.Range(minDistance, 2f * minDistance);

                Vector2 candidate = spawnCenter + new Vector2(
                    distance * Mathf.Cos(angle),
                    distance * Mathf.Sin(angle)
                );

                // Check if candidate is within bounds
                if (candidate.x < -xBound || candidate.x > xBound ||
                    candidate.y < -yBound || candidate.y > yBound)
                {
                    continue;
                }

                // Check grid position
                gridX = Mathf.FloorToInt((candidate.x + xBound) / cellSize);
                gridY = Mathf.FloorToInt((candidate.y + yBound) / cellSize);

                if (gridX < 0 || gridX >= gridWidth || gridY < 0 || gridY >= gridHeight)
                {
                    continue;
                }

                // Check if this point is valid (not too close to existing points)
                bool validPoint = true;

                // Check surrounding cells
                for (int dx = -2; dx <= 2 && validPoint; dx++)
                {
                    for (int dy = -2; dy <= 2 && validPoint; dy++)
                    {
                        int neighborX = gridX + dx;
                        int neighborY = gridY + dy;

                        if (neighborX >= 0 && neighborX < gridWidth &&
                            neighborY >= 0 && neighborY < gridHeight)
                        {

                            Vector2 neighbor = grid[neighborX, neighborY];

                            // If cell has a point and it's too close, invalidate candidate
                            if (neighbor != Vector2.zero &&
                                Vector2.Distance(candidate, neighbor) < minDistance)
                            {
                                validPoint = false;
                            }
                        }
                    }
                }

                if (validPoint)
                {
                    grid[gridX, gridY] = candidate;
                    positions.Add(candidate);
                    spawnPoints.Add(candidate);
                    foundValidPoint = true;
                    break;
                }
            }

            // If no valid points found after tries, remove this spawn point
            if (!foundValidPoint)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }

        // If we couldn't generate enough positions, fall back to random positions
        if (positions.Count < unitCount)
        {
            while (positions.Count < unitCount)
            {
                Vector2 randomPos = new Vector2(
                    Random.Range(-xBound + minDistance / 2, xBound - minDistance / 2),
                    Random.Range(-yBound + minDistance / 2, yBound - minDistance / 2)
                );

                // Try to maintain some minimum distance if possible
                bool tooClose = false;
                foreach (Vector2 existingPos in positions)
                {
                    if (Vector2.Distance(randomPos, existingPos) < minDistance * 0.7f)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    positions.Add(randomPos);
                }
            }
        }

        return positions;
    }

    // Shuffles the list of positions
    private static void ShufflePositions(List<Vector2> positions)
    {
        int n = positions.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            Vector2 temp = positions[k];
            positions[k] = positions[n];
            positions[n] = temp;
        }
    }

    // Line formation (good for small teams)
    private static List<Vector2> GenerateLineFormation(int unitCount, float minX, float maxX, float minY, float maxY, float minDistance)
    {
        List<Vector2> positions = new List<Vector2>();

        float width = maxX - minX;
        float height = maxY - minY;
        float centerX = minX + width / 2;

        // If area is wider than tall, horizontal line
        if (width > height)
        {
            float lineLength = (unitCount - 1) * minDistance;
            float startX = centerX - lineLength / 2;
            float y = (minY + maxY) / 2; // Center Y

            // If line would exceed bounds, adjust
            if (startX < minX)
            {
                startX = minX + minDistance / 2;
            }

            for (int i = 0; i < unitCount; i++)
            {
                float x = startX + i * minDistance;
                // Ensure we stay within bounds
                x = Mathf.Min(x, maxX - minDistance / 2);
                positions.Add(new Vector2(x, y));
            }
        }
        // Otherwise, vertical line
        else
        {
            float lineLength = (unitCount - 1) * minDistance;
            float startY = (minY + maxY) / 2 - lineLength / 2;
            float x = centerX; // Center X

            // If line would exceed bounds, adjust
            if (startY < minY)
            {
                startY = minY + minDistance / 2;
            }

            for (int i = 0; i < unitCount; i++)
            {
                float y = startY + i * minDistance;
                // Ensure we stay within bounds
                y = Mathf.Min(y, maxY - minDistance / 2);
                positions.Add(new Vector2(x, y));
            }
        }

        return positions;
    }

    // Columnar formation (good for tall, narrow areas)
    private static List<Vector2> GenerateColumnarFormation(int unitCount, float minX, float maxX, float minY, float maxY, float minDistance)
    {
        List<Vector2> positions = new List<Vector2>();

        float width = maxX - minX;
        float height = maxY - minY;

        // Calculate how many columns we can fit
        int maxColumns = Mathf.FloorToInt(width / minDistance);
        maxColumns = Mathf.Max(1, maxColumns);

        // Calculate ideal columns based on unit count and available height
        int unitsPerColumn = Mathf.CeilToInt((float)unitCount / maxColumns);
        int optimalColumns = Mathf.CeilToInt((float)unitCount / unitsPerColumn);
        int columns = Mathf.Min(optimalColumns, maxColumns);

        // Recalculate units per column with optimal column count
        unitsPerColumn = Mathf.CeilToInt((float)unitCount / columns);

        // Calculate spacing
        float xSpacing = width / (columns + 1);
        float ySpacing = Mathf.Min(minDistance, height / (unitsPerColumn + 1));

        // Place units in columns
        int unitIndex = 0;
        for (int col = 0; col < columns && unitIndex < unitCount; col++)
        {
            float x = minX + (col + 1) * xSpacing;

            int unitsInThisColumn = Mathf.Min(unitsPerColumn, unitCount - unitIndex);
            float columnHeight = unitsInThisColumn * ySpacing;
            float startY = (minY + maxY) / 2 - columnHeight / 2 + ySpacing / 2;

            for (int row = 0; row < unitsInThisColumn; row++)
            {
                float y = startY + row * ySpacing;
                positions.Add(new Vector2(x, y));
                unitIndex++;
            }
        }

        return positions;
    }

    // Grid formation (good for general case)
    private static List<Vector2> GenerateGridFormation(int unitCount, float minX, float maxX, float minY, float maxY, float minDistance)
    {
        List<Vector2> positions = new List<Vector2>();

        float width = maxX - minX;
        float height = maxY - minY;

        // Calculate aspect ratio of area to determine grid shape
        float aspectRatio = width / height;

        // Calculate grid dimensions
        int cols = Mathf.CeilToInt(Mathf.Sqrt(unitCount * aspectRatio));
        int rows = Mathf.CeilToInt((float)unitCount / cols);

        // Ensure cols and rows will fit at least unitCount
        while (cols * rows < unitCount)
        {
            cols++;
        }

        // Calculate cell size
        float cellWidth = width / cols;
        float cellHeight = height / rows;

        // Ensure minimum distance
        if (cellWidth < minDistance || cellHeight < minDistance)
        {
            // If cells would be too small, use Poisson disc sampling
            return GeneratePoissonDiscPositions(unitCount, minX, maxX, minY, maxY, minDistance);
        }

        // Calculate starting position (center of first cell)
        float startX = minX + cellWidth / 2;
        float startY = maxY - cellHeight / 2;

        // Generate positions in a grid pattern
        int placed = 0;
        for (int row = 0; row < rows && placed < unitCount; row++)
        {
            for (int col = 0; col < cols && placed < unitCount; col++)
            {
                float x = startX + col * cellWidth;
                float y = startY - row * cellHeight;

                // Add small randomization within cell
                float randX = Random.Range(-cellWidth * 0.2f, cellWidth * 0.2f);
                float randY = Random.Range(-cellHeight * 0.2f, cellHeight * 0.2f);

                positions.Add(new Vector2(x + randX, y + randY));
                placed++;
            }
        }

        return positions;
    }

    private static List<Vector2> GeneratePoissonDiscPositions(int unitCount, float minX, float maxX, float minY, float maxY, float minDistance)
    {
        List<Vector2> positions = new List<Vector2>();
        List<Vector2> spawnPoints = new List<Vector2>();

        // Parameters for Poisson disc sampling
        float cellSize = minDistance / Mathf.Sqrt(2);

        float width = maxX - minX;
        float height = maxY - minY;

        int gridWidth = Mathf.CeilToInt(width / cellSize);
        int gridHeight = Mathf.CeilToInt(height / cellSize);

        Vector2[,] grid = new Vector2[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = Vector2.zero; // Mark as empty
            }
        }

        // Start with a random position
        Vector2 firstPosition = new Vector2(
            Random.Range(minX + minDistance, maxX - minDistance),
            Random.Range(minY + minDistance, maxY - minDistance)
        );

        int gridX = Mathf.FloorToInt((firstPosition.x - minX) / cellSize);
        int gridY = Mathf.FloorToInt((firstPosition.y - minY) / cellSize);
        gridX = Mathf.Clamp(gridX, 0, gridWidth - 1);
        gridY = Mathf.Clamp(gridY, 0, gridHeight - 1);

        grid[gridX, gridY] = firstPosition;
        positions.Add(firstPosition);
        spawnPoints.Add(firstPosition);

        // Generate points
        while (spawnPoints.Count > 0 && positions.Count < unitCount)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Vector2 spawnCenter = spawnPoints[spawnIndex];

            bool foundValidPoint = false;

            for (int i = 0; i < 30; i++) // Try 30 times to find a valid point
            {
                float angle = Random.Range(0f, 2f * Mathf.PI);
                float distance = Random.Range(minDistance, 2f * minDistance);

                Vector2 candidate = spawnCenter + new Vector2(
                    distance * Mathf.Cos(angle),
                    distance * Mathf.Sin(angle)
                );

                // Check if candidate is within bounds
                if (candidate.x < minX || candidate.x > maxX ||
                    candidate.y < minY || candidate.y > maxY)
                {
                    continue;
                }

                // Check grid position
                gridX = Mathf.FloorToInt((candidate.x - minX) / cellSize);
                gridY = Mathf.FloorToInt((candidate.y - minY) / cellSize);

                gridX = Mathf.Clamp(gridX, 0, gridWidth - 1);
                gridY = Mathf.Clamp(gridY, 0, gridHeight - 1);

                // Check if this point is valid (not too close to existing points)
                bool validPoint = true;

                // Check surrounding cells
                for (int dx = -2; dx <= 2 && validPoint; dx++)
                {
                    for (int dy = -2; dy <= 2 && validPoint; dy++)
                    {
                        int neighborX = gridX + dx;
                        int neighborY = gridY + dy;

                        if (neighborX >= 0 && neighborX < gridWidth &&
                            neighborY >= 0 && neighborY < gridHeight)
                        {

                            Vector2 neighbor = grid[neighborX, neighborY];

                            // If cell has a point and it's too close, invalidate candidate
                            if (neighbor != Vector2.zero &&
                                Vector2.Distance(candidate, neighbor) < minDistance)
                            {
                                validPoint = false;
                            }
                        }
                    }
                }

                if (validPoint)
                {
                    grid[gridX, gridY] = candidate;
                    positions.Add(candidate);
                    spawnPoints.Add(candidate);
                    foundValidPoint = true;
                    break;
                }
            }

            // If no valid points found after tries, remove this spawn point
            if (!foundValidPoint)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }

        // If we couldn't generate enough positions, fall back to random positions
        if (positions.Count < unitCount)
        {
            while (positions.Count < unitCount)
            {
                Vector2 randomPos = new Vector2(
                    Random.Range(minX + minDistance / 2, maxX - minDistance / 2),
                    Random.Range(minY + minDistance / 2, maxY - minDistance / 2)
                );

                // Try to maintain some minimum distance if possible
                bool tooClose = false;
                foreach (Vector2 existingPos in positions)
                {
                    if (Vector2.Distance(randomPos, existingPos) < minDistance * 0.7f)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    positions.Add(randomPos);
                }
            }
        }

        return positions;
    }

    // Generate positions for one team within a specific area
    public static List<Vector2> GenerateTeamPositions(int unitCount, float minX, float maxX, float minY, float maxY, float minDistance)
    {
        if (unitCount <= 0) return new List<Vector2>();

        List<Vector2> positions = new List<Vector2>();

        // Calculate area dimensions
        float width = maxX - minX;
        float height = maxY - minY;

        // Determine formation type based on unit count and area shape
        if (unitCount <= 10)
        {
            // For small teams, use a line formation
            positions = GenerateLineFormation(unitCount, minX, maxX, minY, maxY, minDistance);
        } else if (height > width * 1.5f)
        {
            // For tall, narrow areas, use columns
            positions = GenerateColumnarFormation(unitCount, minX, maxX, minY, maxY, minDistance);
        } else
        {
            // Default to grid formation
            positions = GenerateGridFormation(unitCount, minX, maxX, minY, maxY, minDistance);
        }

        return positions;
    }

    public static UnitStartingData[] CalculateTeamBattlePositions(TeamClassComposition.TeamComposition[] teamCompositions)
    {
        // Get total counts for each team
        int team1Count = teamCompositions[0].classDistribution.Sum(cd => cd.count);
        int team2Count = teamCompositions[1].classDistribution.Sum(cd => cd.count);

        // Get map bounds from GameManager
        float xBound = GameManager.GetInstance().xBound;
        float yBound = GameManager.GetInstance().yBound;

        // Minimum distance between units
        float minDistance = 0.63f * 2 * 1.1f;

        // Calculate optimal separation between teams
        float optimalSeparation = CalculateOptimalTeamSeparation(team1Count, team2Count, xBound, yBound);

        // Calculate buffer space in middle - use optimal separation or default
        float midBuffer = optimalSeparation;
        int totalUnits = team1Count + team2Count;

        // Only reduce buffer if we have too many units
        if (totalUnits > 120) midBuffer = Mathf.Max(optimalSeparation * 0.5f, xBound * 0.1f);
        if (totalUnits > 200) midBuffer = Mathf.Max(optimalSeparation * 0.3f, 0);

        // Ensure the buffer doesn't exceed reasonable bounds
        midBuffer = Mathf.Min(midBuffer, xBound * 0.6f);

        // Calculate team areas
        float team1Width = xBound - midBuffer / 2;
        float team2Width = xBound - midBuffer / 2;

        List<UnitStartingData> allUnits = new List<UnitStartingData>();

        // Generate positions for Team 1 with individual roll
        List<UnitStartingData> team1Units = GenerateFormationForSingleTeam(
            teamCompositions[0],
            -xBound,
            -xBound + team1Width,
            -yBound,
            yBound,
            minDistance,
            true  // Team 1 faces right
        );

        // Generate positions for Team 2 with individual roll
        List<UnitStartingData> team2Units = GenerateFormationForSingleTeam(
            teamCompositions[1],
            xBound - team2Width,
            xBound,
            -yBound,
            yBound,
            minDistance,
            false  // Team 2 faces left
        );

        // Combine both teams
        allUnits.AddRange(team1Units);
        allUnits.AddRange(team2Units);

        return allUnits.ToArray();
    }

    public static List<UnitStartingData> GenerateFormationForSingleTeam(
    TeamClassComposition.TeamComposition teamComp,
    float minX, float maxX, float minY, float maxY,
    float minDistance,
    bool facingRight)
    {
        var rng = GameManager.GetInstance().rng;

        // 75% chance to use mini formations, 25% for full formation
        if (rng.NextDouble() < 0.75)
        {
            // Flatten all units for mini formations
            List<UnitStartingData> allUnits = new List<UnitStartingData>();
            foreach (var classCount in teamComp.classDistribution)
            {
                allUnits.AddRange(classCount.unitData);
            }

            return StartingPositionsMiniFormations.GenerateMiniFormations(
                allUnits, minX, maxX, minY, maxY, minDistance, facingRight);
        } else
        {
            // Use full formation
            return GenerateClassBasedPositions(teamComp, minX, maxX, minY, maxY, minDistance, facingRight);
        }
    }

    private static List<UnitStartingData> GenerateClassBasedPositions(
    TeamClassComposition.TeamComposition teamComp,
    float minX, float maxX, float minY, float maxY,
    float minDistance,
    bool facingRight)
    {
        List<UnitStartingData> units = new List<UnitStartingData>();

        // Sort classes into melee and ranged with their unit data
        List<ClassUnitCount> meleeClasses = new List<ClassUnitCount>();
        List<ClassUnitCount> rangedClasses = new List<ClassUnitCount>();

        foreach (var classCount in teamComp.classDistribution)
        {
            UnitClass unitClass = ClassDataManager.Instance.GetClassDataByName(classCount.className);
            bool isRanged = IsRangedClass(unitClass);

            ClassUnitCount classUnitCount = new ClassUnitCount(unitClass, classCount.count);
            classUnitCount.unitData = classCount.unitData;

            if (isRanged)
                rangedClasses.Add(classUnitCount);
            else
                meleeClasses.Add(classUnitCount);
        }

        int meleeCount = meleeClasses.Sum(c => c.count);
        int rangedCount = rangedClasses.Sum(c => c.count);

        if (meleeCount == 0)
        {
            return GenerateVariedPositionsForUnits(rangedClasses, teamComp.teamId, minX, maxX, minY, maxY, minDistance);
        } else if (rangedCount == 0)
        {
            return GenerateVariedPositionsForUnits(meleeClasses, teamComp.teamId, minX, maxX, minY, maxY, minDistance);
        }

        // Choose formation strategy
        float formationChoice = UnityEngine.Random.value;

        if (formationChoice < 0.33f)
        {
            // Standard melee front, ranged back with variance
            return GenerateStandardFormationWithVariance(meleeClasses, rangedClasses, teamComp.teamId, minX, maxX, minY, maxY, minDistance, facingRight);
        } else if (formationChoice < 0.66f)
        {
            // Multiple groups formation
            return GenerateMultiGroupFormation(meleeClasses, rangedClasses, teamComp.teamId, minX, maxX, minY, maxY, minDistance, facingRight);
        } else
        {
            // Original center formation (kept for variety)
            return GenerateOriginalFormation(meleeClasses, rangedClasses, teamComp.teamId, minX, maxX, minY, maxY, minDistance, facingRight);
        }
    }

    private static List<UnitStartingData> GenerateStandardFormationWithVariance(
    List<ClassUnitCount> meleeClasses,
    List<ClassUnitCount> rangedClasses,
    int teamId,
    float minX, float maxX, float minY, float maxY,
    float minDistance,
    bool facingRight)
    {
        List<UnitStartingData> units = new List<UnitStartingData>();
        float totalWidth = maxX - minX;
        float totalHeight = maxY - minY;

        // Vary the front/back split
        float meleeWidthRatio = UnityEngine.Random.Range(0.5f, 0.7f);
        float meleeWidth = totalWidth * meleeWidthRatio;
        float rangedWidth = totalWidth - meleeWidth;

        // Add vertical offset variance
        float verticalOffset = UnityEngine.Random.Range(-totalHeight * 0.2f, totalHeight * 0.2f);
        float verticalSpread = UnityEngine.Random.Range(0.6f, 0.95f); // How much of vertical space to use

        // Calculate areas with variance
        float meleeMinX, meleeMaxX, rangedMinX, rangedMaxX;

        if (facingRight)
        {
            meleeMinX = minX + rangedWidth;
            meleeMaxX = maxX;
            rangedMinX = minX;
            rangedMaxX = minX + rangedWidth;
        } else
        {
            meleeMinX = minX;
            meleeMaxX = minX + meleeWidth;
            rangedMinX = minX + meleeWidth;
            rangedMaxX = maxX;
        }

        // Apply vertical adjustments
        float effectiveMinY = minY + (totalHeight * (1 - verticalSpread) / 2) + verticalOffset;
        float effectiveMaxY = maxY - (totalHeight * (1 - verticalSpread) / 2) + verticalOffset;

        // Clamp to ensure we stay in bounds
        effectiveMinY = Mathf.Max(effectiveMinY, minY);
        effectiveMaxY = Mathf.Min(effectiveMaxY, maxY);

        // Generate positions for melee units
        List<UnitStartingData> meleeUnits = GenerateFormationWithSpread(
            meleeClasses, teamId, meleeMinX, meleeMaxX, effectiveMinY, effectiveMaxY, minDistance);

        // Generate positions for ranged units
        List<UnitStartingData> rangedUnits = GenerateFormationWithSpread(
            rangedClasses, teamId, rangedMinX, rangedMaxX, effectiveMinY, effectiveMaxY, minDistance);

        units.AddRange(meleeUnits);
        units.AddRange(rangedUnits);

        return units;
    }

    private static List<UnitStartingData> GenerateMultiGroupFormation(
    List<ClassUnitCount> meleeClasses,
    List<ClassUnitCount> rangedClasses,
    int teamId,
    float minX, float maxX, float minY, float maxY,
    float minDistance,
    bool facingRight)
    {
        List<UnitStartingData> units = new List<UnitStartingData>();

        // Flatten all units into lists
        List<UnitStartingData> allMeleeUnits = new List<UnitStartingData>();
        List<UnitStartingData> allRangedUnits = new List<UnitStartingData>();

        foreach (var meleeClass in meleeClasses)
        {
            allMeleeUnits.AddRange(meleeClass.unitData);
        }

        foreach (var rangedClass in rangedClasses)
        {
            allRangedUnits.AddRange(rangedClass.unitData);
        }

        int meleeCount = allMeleeUnits.Count;
        int rangedCount = allRangedUnits.Count;

        // Determine number of groups (2-4)
        int numGroups = UnityEngine.Random.Range(2, 5);

        float totalWidth = maxX - minX;
        float totalHeight = maxY - minY;

        // Split the area into groups
        float groupHeight = totalHeight / numGroups;
        float meleeWidthRatio = UnityEngine.Random.Range(0.5f, 0.7f);

        // Distribute units among groups
        int[] meleePerGroup = DistributeUnitsToGroups(meleeCount, numGroups);
        int[] rangedPerGroup = DistributeUnitsToGroups(rangedCount, numGroups);

        int meleeIndex = 0;
        int rangedIndex = 0;

        for (int group = 0; group < numGroups; group++)
        {
            float groupMinY = minY + group * groupHeight;
            float groupMaxY = groupMinY + groupHeight;

            // Add some overlap between groups
            if (group > 0) groupMinY -= minDistance * 0.5f;
            if (group < numGroups - 1) groupMaxY += minDistance * 0.5f;

            // Extract units for this group
            List<UnitStartingData> groupMeleeUnits = new List<UnitStartingData>();
            List<UnitStartingData> groupRangedUnits = new List<UnitStartingData>();

            // Get melee units for this group
            for (int i = 0; i < meleePerGroup[group] && meleeIndex < allMeleeUnits.Count; i++)
            {
                groupMeleeUnits.Add(allMeleeUnits[meleeIndex++]);
            }

            // Get ranged units for this group
            for (int i = 0; i < rangedPerGroup[group] && rangedIndex < allRangedUnits.Count; i++)
            {
                groupRangedUnits.Add(allRangedUnits[rangedIndex++]);
            }

            // Generate positions for this group
            List<UnitStartingData> groupUnits = GenerateGroupFormationSimple(
                groupMeleeUnits, groupRangedUnits, teamId,
                minX, maxX, groupMinY, groupMaxY,
                minDistance, facingRight, meleeWidthRatio);

            units.AddRange(groupUnits);
        }

        return units;
    }

    // Simplified group formation generation
    private static List<UnitStartingData> GenerateGroupFormationSimple(
        List<UnitStartingData> meleeUnits,
        List<UnitStartingData> rangedUnits,
        int teamId,
        float minX, float maxX, float minY, float maxY,
        float minDistance,
        bool facingRight,
        float meleeWidthRatio)
    {
        List<UnitStartingData> units = new List<UnitStartingData>();

        float totalWidth = maxX - minX;
        float meleeWidth = totalWidth * meleeWidthRatio;
        float rangedWidth = totalWidth - meleeWidth;

        float meleeMinX, meleeMaxX, rangedMinX, rangedMaxX;

        if (facingRight)
        {
            meleeMinX = minX + rangedWidth;
            meleeMaxX = maxX;
            rangedMinX = minX;
            rangedMaxX = minX + rangedWidth;
        } else
        {
            meleeMinX = minX;
            meleeMaxX = minX + meleeWidth;
            rangedMinX = minX + meleeWidth;
            rangedMaxX = maxX;
        }

        // Generate positions for melee units
        if (meleeUnits.Count > 0)
        {
            List<Vector2> meleePositions = GenerateTeamPositions(
                meleeUnits.Count, meleeMinX, meleeMaxX, minY, maxY, minDistance);

            for (int i = 0; i < meleeUnits.Count && i < meleePositions.Count; i++)
            {
                units.Add(new UnitStartingData(
                    meleePositions[i],
                    meleeUnits[i].TeamId,
                    meleeUnits[i].UnitClass,
                    meleeUnits[i].UnitName,
                    meleeUnits[i].ScaleFactor
                ));
            }
        }

        // Generate positions for ranged units
        if (rangedUnits.Count > 0)
        {
            List<Vector2> rangedPositions = GenerateTeamPositions(
                rangedUnits.Count, rangedMinX, rangedMaxX, minY, maxY, minDistance);

            for (int i = 0; i < rangedUnits.Count && i < rangedPositions.Count; i++)
            {
                units.Add(new UnitStartingData(
                    rangedPositions[i],
                    rangedUnits[i].TeamId,
                    rangedUnits[i].UnitClass,
                    rangedUnits[i].UnitName,
                    rangedUnits[i].ScaleFactor
                ));
            }
        }

        return units;
    }

    // Helper method to distribute units among groups
    private static int[] DistributeUnitsToGroups(int totalUnits, int numGroups)
    {
        int[] distribution = new int[numGroups];
        int basePerGroup = totalUnits / numGroups;
        int remainder = totalUnits % numGroups;

        for (int i = 0; i < numGroups; i++)
        {
            distribution[i] = basePerGroup;
            if (remainder > 0)
            {
                distribution[i]++;
                remainder--;
            }
        }

        // Add some variance
        for (int i = 0; i < numGroups - 1; i++)
        {
            if (distribution[i] > 1 && distribution[i + 1] > 1)
            {
                int shift = UnityEngine.Random.Range(-1, 2);
                distribution[i] += shift;
                distribution[i + 1] -= shift;
            }
        }

        return distribution;
    }

    // Generate formation for a single group
    private static List<UnitStartingData> GenerateGroupFormation(
        List<ClassUnitCount> meleeClasses,
        List<ClassUnitCount> rangedClasses,
        int teamId,
        float minX, float maxX, float minY, float maxY,
        float minDistance,
        bool facingRight,
        float meleeWidthRatio)
    {
        List<UnitStartingData> units = new List<UnitStartingData>();

        float totalWidth = maxX - minX;
        float meleeWidth = totalWidth * meleeWidthRatio;
        float rangedWidth = totalWidth - meleeWidth;

        float meleeMinX, meleeMaxX, rangedMinX, rangedMaxX;

        if (facingRight)
        {
            meleeMinX = minX + rangedWidth;
            meleeMaxX = maxX;
            rangedMinX = minX;
            rangedMaxX = minX + rangedWidth;
        } else
        {
            meleeMinX = minX;
            meleeMaxX = minX + meleeWidth;
            rangedMinX = minX + meleeWidth;
            rangedMaxX = maxX;
        }

        // Generate positions
        List<UnitStartingData> meleeUnits = GenerateFormationWithSpread(
            meleeClasses, teamId, meleeMinX, meleeMaxX, minY, maxY, minDistance);

        List<UnitStartingData> rangedUnits = GenerateFormationWithSpread(
            rangedClasses, teamId, rangedMinX, rangedMaxX, minY, maxY, minDistance);

        units.AddRange(meleeUnits);
        units.AddRange(rangedUnits);

        return units;
    }

    private static List<UnitStartingData> GenerateFormationWithSpread(
    List<ClassUnitCount> classUnits,
    int teamId,
    float minX, float maxX, float minY, float maxY,
    float minDistance)
    {
        List<UnitStartingData> units = new List<UnitStartingData>();

        // Flatten all units first
        List<UnitStartingData> allUnits = new List<UnitStartingData>();
        foreach (var classUnit in classUnits)
        {
            allUnits.AddRange(classUnit.unitData);
        }

        int totalCount = allUnits.Count;
        if (totalCount == 0) return units;

        // Choose formation type based on area shape and unit count
        float width = maxX - minX;
        float height = maxY - minY;
        float aspectRatio = width / height;

        List<Vector2> positions;

        if (totalCount <= 8 || aspectRatio > 2)
        {
            positions = GenerateSpreadLineFormation(totalCount, minX, maxX, minY, maxY, minDistance);
        } else if (height > width * 1.5f)
        {
            positions = GenerateSpreadColumnarFormation(totalCount, minX, maxX, minY, maxY, minDistance);
        } else
        {
            positions = GenerateSpreadGridFormation(totalCount, minX, maxX, minY, maxY, minDistance);
        }

        // Make sure we have enough positions
        if (positions.Count < totalCount)
        {
            Debug.LogWarning($"Not enough positions generated: {positions.Count} < {totalCount}");
            // Generate additional positions if needed
            positions.AddRange(GenerateTeamPositions(
                totalCount - positions.Count, minX, maxX, minY, maxY, minDistance));
        }

        // Assign positions to units
        for (int i = 0; i < allUnits.Count && i < positions.Count; i++)
        {
            UnitStartingData finalUnitData = new UnitStartingData(
                positions[i],
                allUnits[i].TeamId,
                allUnits[i].UnitClass,
                allUnits[i].UnitName,
                allUnits[i].ScaleFactor
            );
            units.Add(finalUnitData);
        }

        return units;
    }

    // Keep the original formation as one option
    private static List<UnitStartingData> GenerateOriginalFormation(
        List<ClassUnitCount> meleeClasses,
        List<ClassUnitCount> rangedClasses,
        int teamId,
        float minX, float maxX, float minY, float maxY,
        float minDistance,
        bool facingRight)
    {
        List<UnitStartingData> units = new List<UnitStartingData>();

        int meleeCount = meleeClasses.Sum(c => c.count);
        int rangedCount = rangedClasses.Sum(c => c.count);

        // Handle special cases - use the unified method for both
        if (meleeCount == 0) // All ranged
        {
            return GeneratePositionsForUnits(rangedClasses, teamId, minX, maxX, minY, maxY, minDistance);
        } else if (rangedCount == 0) // All melee
        {
            return GeneratePositionsForUnits(meleeClasses, teamId, minX, maxX, minY, maxY, minDistance);
        }

        // Calculate area division - how much space to allocate for melee vs ranged
        float totalWidth = maxX - minX;
        float meleeWidthRatio = 0.6f; // Melee gets 60% of the space by default

        // Adjust ratio based on unit counts
        if (meleeCount > rangedCount * 2)
        {
            meleeWidthRatio = 0.7f; // Give more space to melee if they outnumber ranged
        } else if (rangedCount > meleeCount * 2)
        {
            meleeWidthRatio = 0.5f; // Equal space if ranged outnumber melee
        }

        float meleeWidth = totalWidth * meleeWidthRatio;
        float rangedWidth = totalWidth - meleeWidth;

        // Determine areas for each group based on facing direction
        float meleeMinX, meleeMaxX, rangedMinX, rangedMaxX;

        if (facingRight)
        {
            // Team 1 (facing right) - melee in front (right), ranged behind (left)
            meleeMinX = minX + rangedWidth;
            meleeMaxX = maxX;
            rangedMinX = minX;
            rangedMaxX = minX + rangedWidth;
        } else
        {
            // Team 2 (facing left) - melee in front (left), ranged behind (right)
            meleeMinX = minX;
            meleeMaxX = minX + meleeWidth;
            rangedMinX = minX + meleeWidth;
            rangedMaxX = maxX;
        }

        // Generate positions for melee units
        List<UnitStartingData> meleeUnits = GeneratePositionsForUnits(
            meleeClasses, teamId, meleeMinX, meleeMaxX, minY, maxY, minDistance);

        // Generate positions for ranged units
        List<UnitStartingData> rangedUnits = GeneratePositionsForUnits(
            rangedClasses, teamId, rangedMinX, rangedMaxX, minY, maxY, minDistance);

        // Combine both groups
        units.AddRange(meleeUnits);
        units.AddRange(rangedUnits);

        return units;
    }

    private static List<UnitStartingData> GenerateVariedPositionsForUnits(
    List<ClassUnitCount> classUnits,
    int teamId,
    float minX, float maxX, float minY, float maxY,
    float minDistance)
    {
        List<UnitStartingData> units = new List<UnitStartingData>();

        // Flatten all units
        List<UnitStartingData> allUnits = new List<UnitStartingData>();
        foreach (var classUnit in classUnits)
        {
            allUnits.AddRange(classUnit.unitData);
        }

        if (allUnits.Count == 0) return units;

        // Generate positions using standard method
        List<Vector2> positions = GenerateTeamPositions(allUnits.Count, minX, maxX, minY, maxY, minDistance);

        // Assign positions to units
        for (int i = 0; i < allUnits.Count && i < positions.Count; i++)
        {
            UnitStartingData finalUnitData = new UnitStartingData(
                positions[i],
                allUnits[i].TeamId,
                allUnits[i].UnitClass,
                allUnits[i].UnitName,
                allUnits[i].ScaleFactor
            );
            units.Add(finalUnitData);
        }

        return units;
    }

    // New formation generation methods with better spread
    private static List<Vector2> GenerateSpreadLineFormation(int unitCount, float minX, float maxX, float minY, float maxY, float minDistance)
    {
        List<Vector2> positions = new List<Vector2>();

        float width = maxX - minX;
        float height = maxY - minY;

        // Determine orientation and add variance
        bool horizontal = width > height * 1.2f;
        float spread = UnityEngine.Random.Range(0.7f, 0.95f);

        if (horizontal)
        {
            float lineWidth = Mathf.Min(width * spread, (unitCount - 1) * minDistance);
            float startX = minX + (width - lineWidth) / 2;
            float spacing = unitCount > 1 ? lineWidth / (unitCount - 1) : 0;
            spacing = Mathf.Max(spacing, minDistance);

            // Vary Y position
            float baseY = minY + height * UnityEngine.Random.Range(0.3f, 0.7f);

            for (int i = 0; i < unitCount; i++)
            {
                float x = startX + i * spacing;
                float y = baseY + UnityEngine.Random.Range(-height * 0.1f, height * 0.1f);
                y = Mathf.Clamp(y, minY + minDistance / 2, maxY - minDistance / 2);
                positions.Add(new Vector2(x, y));
            }
        } else
        {
            float lineHeight = Mathf.Min(height * spread, (unitCount - 1) * minDistance);
            float startY = minY + (height - lineHeight) / 2;
            float spacing = unitCount > 1 ? lineHeight / (unitCount - 1) : 0;
            spacing = Mathf.Max(spacing, minDistance);

            // Vary X position
            float baseX = minX + width * UnityEngine.Random.Range(0.3f, 0.7f);

            for (int i = 0; i < unitCount; i++)
            {
                float y = startY + i * spacing;
                float x = baseX + UnityEngine.Random.Range(-width * 0.1f, width * 0.1f);
                x = Mathf.Clamp(x, minX + minDistance / 2, maxX - minDistance / 2);
                positions.Add(new Vector2(x, y));
            }
        }

        return positions;
    }

    private static List<Vector2> GenerateSpreadColumnarFormation(int unitCount, float minX, float maxX, float minY, float maxY, float minDistance)
    {
        List<Vector2> positions = new List<Vector2>();

        float width = maxX - minX;
        float height = maxY - minY;

        // Calculate columns with variance
        int maxColumns = Mathf.FloorToInt(width / minDistance);
        maxColumns = Mathf.Max(1, maxColumns);

        // Vary the number of columns
        int targetColumns = UnityEngine.Random.Range(2, Mathf.Min(maxColumns, unitCount / 2 + 1));
        int unitsPerColumn = Mathf.CeilToInt((float)unitCount / targetColumns);
        int columns = Mathf.Min(targetColumns, maxColumns);

        // Vary horizontal spread
        float horizontalSpread = UnityEngine.Random.Range(0.6f, 0.9f);
        float effectiveWidth = width * horizontalSpread;
        float startX = minX + (width - effectiveWidth) / 2;

        // Calculate spacing
        float xSpacing = columns > 1 ? effectiveWidth / (columns - 1) : 0;

        int unitIndex = 0;
        for (int col = 0; col < columns && unitIndex < unitCount; col++)
        {
            float x = startX + col * xSpacing;
            int unitsInThisColumn = Mathf.Min(unitsPerColumn, unitCount - unitIndex);

            // Vary vertical spread per column
            float columnSpread = UnityEngine.Random.Range(0.7f, 0.95f);
            float effectiveHeight = height * columnSpread;
            float startY = minY + (height - effectiveHeight) / 2;

            float ySpacing = unitsInThisColumn > 1 ? effectiveHeight / (unitsInThisColumn - 1) : 0;
            ySpacing = Mathf.Max(ySpacing, minDistance);

            for (int row = 0; row < unitsInThisColumn; row++)
            {
                float y = startY + row * ySpacing;
                positions.Add(new Vector2(x, y));
                unitIndex++;
            }
        }

        return positions;
    }

    private static List<Vector2> GenerateSpreadGridFormation(int unitCount, float minX, float maxX, float minY, float maxY, float minDistance)
    {
        List<Vector2> positions = new List<Vector2>();

        float width = maxX - minX;
        float height = maxY - minY;
        float aspectRatio = width / height;

        // Calculate grid dimensions with variance
        int cols = Mathf.CeilToInt(Mathf.Sqrt(unitCount * aspectRatio));
        int rows = Mathf.CeilToInt((float)unitCount / cols);

        // Vary the spread
        float horizontalSpread = UnityEngine.Random.Range(0.7f, 0.95f);
        float verticalSpread = UnityEngine.Random.Range(0.7f, 0.95f);

        float effectiveWidth = width * horizontalSpread;
        float effectiveHeight = height * verticalSpread;

        float cellWidth = effectiveWidth / cols;
        float cellHeight = effectiveHeight / rows;

        // Ensure minimum distance
        cellWidth = Mathf.Max(cellWidth, minDistance);
        cellHeight = Mathf.Max(cellHeight, minDistance);

        // Recalculate grid if needed
        cols = Mathf.FloorToInt(effectiveWidth / cellWidth);
        rows = Mathf.FloorToInt(effectiveHeight / cellHeight);

        // Center the grid
        float startX = minX + (width - cols * cellWidth) / 2 + cellWidth / 2;
        float startY = minY + (height - rows * cellHeight) / 2 + cellHeight / 2;

        int placed = 0;
        for (int row = 0; row < rows && placed < unitCount; row++)
        {
            for (int col = 0; col < cols && placed < unitCount; col++)
            {
                float x = startX + col * cellWidth;
                float y = startY + row * cellHeight;

                // Add small randomization
                x += UnityEngine.Random.Range(-cellWidth * 0.2f, cellWidth * 0.2f);
                y += UnityEngine.Random.Range(-cellHeight * 0.2f, cellHeight * 0.2f);

                // Ensure we stay in bounds
                x = Mathf.Clamp(x, minX + minDistance / 2, maxX - minDistance / 2);
                y = Mathf.Clamp(y, minY + minDistance / 2, maxY - minDistance / 2);

                positions.Add(new Vector2(x, y));
                placed++;
            }
        }

        return positions;
    }

    // Unified method that replaces GenerateAllRangedPositions and GenerateAllMeleePositions
    private static List<UnitStartingData> GeneratePositionsForUnits(
        List<ClassUnitCount> classUnits,
        int teamId,
        float minX, float maxX, float minY, float maxY,
        float minDistance)
    {
        List<UnitStartingData> units = new List<UnitStartingData>();
        int totalCount = classUnits.Sum(c => c.count);

        // Generate positions using standard method
        List<Vector2> positions = GenerateTeamPositions(totalCount, minX, maxX, minY, maxY, minDistance);
        int positionIndex = 0;

        // Assign positions to units while preserving their data
        foreach (var classUnit in classUnits)
        {
            foreach (var unitData in classUnit.unitData)
            {
                if (positionIndex < positions.Count)
                {
                    UnitStartingData finalUnitData = new UnitStartingData(
                        positions[positionIndex],
                        unitData.TeamId,
                        unitData.UnitClass,
                        unitData.UnitName,
                        unitData.ScaleFactor
                    );
                    units.Add(finalUnitData);
                    positionIndex++;
                }
            }
        }

        return units;
    }

    public static UnitStartingData[] CalculateBattleRoyalePositions(TeamClassComposition.TeamComposition teamComp)
    {
        List<UnitStartingData> allPositions = new List<UnitStartingData>();

        // Count total units
        int totalUnits = teamComp.classDistribution.Sum(cd => cd.count);

        // Get map bounds from GameManager
        float xBound = GameManager.GetInstance().xBound;
        float yBound = GameManager.GetInstance().yBound;

        // Minimum distance between units
        float minDistance = 0.63f * 2 * 1.1f;

        List<Vector2> positions;

        // For battle royale, we want units spread out across the entire map
        if (totalUnits <= 16)
        {
            // Use circular formation for small numbers
            positions = GenerateCircularFormation(totalUnits, xBound, yBound, minDistance);
        //} else if (totalUnits <= 50)
        //{
        //    // Use Poisson disc sampling for medium numbers - gives good spread
        //    positions = GeneratePoissonDiscPositions(totalUnits, xBound, yBound, minDistance * 1.5f);
        } else
        {
            // Use distributed grid for large numbers
            positions = GenerateDistributedGridFormation(totalUnits, xBound, yBound, minDistance);
        }

        // Shuffle positions to randomize which class goes where
        ShufflePositions(positions);

        int positionIndex = 0;

        // Process each class and assign positions to units
        foreach (var classCount in teamComp.classDistribution)
        {
            // For each unit in this class
            foreach (var unitData in classCount.unitData)
            {
                if (positionIndex < positions.Count)
                {
                    // Create new UnitStartingData with position and all unit info
                    UnitStartingData startingData = new UnitStartingData(
                        positions[positionIndex],
                        0, // Team 0 for battle royale (no team)
                        unitData.UnitClass,
                        unitData.UnitName,
                        unitData.ScaleFactor
                    );

                    allPositions.Add(startingData);
                    positionIndex++;
                }
            }
        }

        return allPositions.ToArray();
    }

    // Helper method to check if a class is ranged
    private static bool IsRangedClass(object unitClass)
    {
        // Call the IsRangedClass method via reflection or direct call
        // For simplicity, assuming unitClass has IsRangedClass() method
        Debug.Log(unitClass);
        System.Reflection.MethodInfo method = unitClass.GetType().GetMethod("IsRangedClass");
        if (method != null)
        {
            return (bool)method.Invoke(unitClass, null);
        }

        // Fallback - assume melee if we can't determine
        return false;
    }

    public static float CalculateOptimalTeamSeparation(int team1Count, int team2Count, float xBound, float yBound)
    {
        // Base minimum separation
        float minSeparation = 3f;

        // Calculate area needed for each team
        float minDistance = 0.63f * 2 * 1.1f;

        // Estimate area needed for team formations
        float team1Area = team1Count * minDistance * minDistance * 2f;
        float team2Area = team2Count * minDistance * minDistance * 2f;

        // Calculate rough radius for each team's formation
        float team1Radius = Mathf.Sqrt(team1Area / Mathf.PI);
        float team2Radius = Mathf.Sqrt(team2Area / Mathf.PI);

        // Desired separation is sum of radii plus buffer
        float optimalSeparation = team1Radius + team2Radius + minSeparation;

        // Ensure we don't exceed map bounds
        float maxSeparation = Mathf.Min(xBound * 0.8f, yBound * 0.8f);

        return Mathf.Min(optimalSeparation, maxSeparation);
    }

    // Helper class for tracking units by class
    private class ClassUnitCount
    {
        public UnitClass unitClass;
        public int count;
        public List<UnitStartingData> unitData;

        public ClassUnitCount(UnitClass unitClass, int count)
        {
            this.unitClass = unitClass;
            this.count = count;
            this.unitData = new List<UnitStartingData>();
        }

    }
}
