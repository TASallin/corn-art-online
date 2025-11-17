using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class StartingPositionsMiniFormationsExtra
{
    // Grid Formation - works well for 4, 9, 16 units (square numbers) or rectangles
    public class GridFormation : StartingPositionsMiniFormations.MiniFormation
    {
        public override bool CanHandle(int unitCount)
        {
            return unitCount >= 4 && unitCount <= 25; // Reasonable grid sizes
        }

        public override List<Vector2> GeneratePositions(int unitCount, StartingPositionsMiniFormations.FormationArea area, float minDistance, bool facingRight)
        {
            List<Vector2> positions = new List<Vector2>();

            // Calculate optimal grid dimensions
            int cols = Mathf.CeilToInt(Mathf.Sqrt(unitCount));
            int rows = Mathf.CeilToInt((float)unitCount / cols);

            // Adjust for rectangular grids
            while (cols * rows < unitCount)
            {
                if (cols <= rows) cols++;
                else rows++;
            }

            float cellWidth = area.width / (cols + 1);
            float cellHeight = area.height / (rows + 1);

            // Ensure minimum spacing
            cellWidth = Mathf.Max(cellWidth, minDistance);
            cellHeight = Mathf.Max(cellHeight, minDistance);

            // Center the grid
            float startX = area.minX + (area.width - (cols - 1) * cellWidth) / 2;
            float startY = area.minY + (area.height - (rows - 1) * cellHeight) / 2;

            int placed = 0;
            for (int row = 0; row < rows && placed < unitCount; row++)
            {
                for (int col = 0; col < cols && placed < unitCount; col++)
                {
                    float x = startX + col * cellWidth;
                    float y = startY + row * cellHeight;
                    positions.Add(new Vector2(x, y));
                    placed++;
                }
            }

            return positions;
        }

        public override List<UnitStartingData> OrderUnits(List<UnitStartingData> units, bool facingRight)
        {
            var meleeUnits = units.Where(u => !IsRangedUnit(u)).ToList();
            var rangedUnits = units.Where(u => IsRangedUnit(u)).ToList();

            if (meleeUnits.Count == 0 || rangedUnits.Count == 0)
                return units; // All same type

            // For grids, put melee on the perimeter and ranged in the center
            var orderedUnits = new List<UnitStartingData>();
            int totalUnits = units.Count;

            // Calculate grid dimensions
            int cols = Mathf.CeilToInt(Mathf.Sqrt(totalUnits));
            int rows = Mathf.CeilToInt((float)totalUnits / cols);

            // Create a 2D representation to determine perimeter positions
            bool[,] isPerimeter = new bool[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (r == 0 || r == rows - 1 || c == 0 || c == cols - 1)
                        isPerimeter[r, c] = true;
                }
            }

            // Fill positions
            int meleeIndex = 0, rangedIndex = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (orderedUnits.Count >= totalUnits) break;

                    if (isPerimeter[r, c] && meleeIndex < meleeUnits.Count)
                    {
                        orderedUnits.Add(meleeUnits[meleeIndex++]);
                    } else if (rangedIndex < rangedUnits.Count)
                    {
                        orderedUnits.Add(rangedUnits[rangedIndex++]);
                    } else if (meleeIndex < meleeUnits.Count)
                    {
                        orderedUnits.Add(meleeUnits[meleeIndex++]);
                    }
                }
            }

            return orderedUnits;
        }
    }

    // Cross Formation - works best with 5, 9, 13 units (4n+1)
    public class CrossFormation : StartingPositionsMiniFormations.MiniFormation
    {
        public override bool CanHandle(int unitCount)
        {
            // Works best with odd numbers that form a cross
            return unitCount >= 5 && (unitCount - 1) % 4 <= 2;
        }

        public override List<Vector2> GeneratePositions(int unitCount, StartingPositionsMiniFormations.FormationArea area, float minDistance, bool facingRight)
        {
            List<Vector2> positions = new List<Vector2>();
            Vector2 center = area.GetCenter();

            // Always place one unit at center
            positions.Add(center);

            int remainingUnits = unitCount - 1;
            int unitsPerArm = remainingUnits / 4;
            int extraUnits = remainingUnits % 4;

            // Calculate spacing
            float armLength = Mathf.Min(area.width, area.height) * 0.4f;
            float spacing = Mathf.Min(armLength / unitsPerArm, minDistance * 1.2f);

            // Place units on each arm of the cross
            Vector2[] directions = new Vector2[]
            {
                Vector2.right,
                Vector2.up,
                Vector2.left,
                Vector2.down
            };

            for (int arm = 0; arm < 4; arm++)
            {
                int unitsOnThisArm = unitsPerArm + (arm < extraUnits ? 1 : 0);

                for (int i = 1; i <= unitsOnThisArm; i++)
                {
                    Vector2 pos = center + directions[arm] * (spacing * i);
                    positions.Add(pos);
                }
            }

            return positions;
        }

        public override float GetPreferenceScore(List<UnitStartingData> units)
        {
            // Cross formations work best with mixed units (center different from edges)
            var meleeCount = units.Count(u => !IsRangedUnit(u));
            var rangedCount = units.Count(u => IsRangedUnit(u));

            // Perfect for 1 unit of one type in center, rest of another
            if (meleeCount == 1 || rangedCount == 1)
                return 1.5f;

            return 1.0f;
        }

        public override List<UnitStartingData> OrderUnits(List<UnitStartingData> units, bool facingRight)
        {
            var meleeUnits = units.Where(u => !IsRangedUnit(u)).ToList();
            var rangedUnits = units.Where(u => IsRangedUnit(u)).ToList();

            var orderedUnits = new List<UnitStartingData>();

            // Place single unit type in center if we have exactly one
            if (meleeUnits.Count == 1)
            {
                orderedUnits.Add(meleeUnits[0]);
                orderedUnits.AddRange(rangedUnits);
            } else if (rangedUnits.Count == 1)
            {
                orderedUnits.Add(rangedUnits[0]);
                orderedUnits.AddRange(meleeUnits);
            } else
            {
                // Default ordering
                return base.OrderUnits(units, facingRight);
            }

            return orderedUnits;
        }
    }

    // Circle Formation - evenly spaced units around a circle
    public class CircleFormation : StartingPositionsMiniFormations.MiniFormation
    {
        public override bool CanHandle(int unitCount)
        {
            return unitCount >= 4 && unitCount <= 20; // Circles get crowded with too many
        }

        public override List<Vector2> GeneratePositions(int unitCount, StartingPositionsMiniFormations.FormationArea area, float minDistance, bool facingRight)
        {
            List<Vector2> positions = new List<Vector2>();
            Vector2 center = area.GetCenter();
            var rng = GameManager.GetInstance().rng;

            // Calculate radius based on area and unit count
            float radius = Mathf.Min(area.width, area.height) * 0.4f;

            // Ensure units aren't too close on the circumference
            float circumference = 2 * Mathf.PI * radius;
            float requiredSpace = unitCount * minDistance;

            if (circumference < requiredSpace)
            {
                radius = requiredSpace / (2 * Mathf.PI);
            }

            // Random starting angle for variety
            float startAngle = (float)(rng.NextDouble() * 2 * Mathf.PI);

            for (int i = 0; i < unitCount; i++)
            {
                float angle = startAngle + (2 * Mathf.PI * i / unitCount);
                float x = center.x + radius * Mathf.Cos(angle);
                float y = center.y + radius * Mathf.Sin(angle);
                positions.Add(new Vector2(x, y));
            }

            return positions;
        }

        public override List<UnitStartingData> OrderUnits(List<UnitStartingData> units, bool facingRight)
        {
            var meleeUnits = units.Where(u => !IsRangedUnit(u)).ToList();
            var rangedUnits = units.Where(u => IsRangedUnit(u)).ToList();

            if (meleeUnits.Count == 0 || rangedUnits.Count == 0)
                return units;

            // Alternate unit types around the circle for balance
            var orderedUnits = new List<UnitStartingData>();
            int meleeIndex = 0, rangedIndex = 0;

            while (meleeIndex < meleeUnits.Count || rangedIndex < rangedUnits.Count)
            {
                if (meleeIndex < meleeUnits.Count)
                    orderedUnits.Add(meleeUnits[meleeIndex++]);
                if (rangedIndex < rangedUnits.Count)
                    orderedUnits.Add(rangedUnits[rangedIndex++]);
            }

            return orderedUnits;
        }
    }

    // Cluster Formation - tight group with some randomization
    public class ClusterFormation : StartingPositionsMiniFormations.MiniFormation
    {
        public override bool CanHandle(int unitCount)
        {
            return unitCount >= 1 && unitCount <= 15; // Changed from 2 to 1
        }

        public override List<Vector2> GeneratePositions(int unitCount, StartingPositionsMiniFormations.FormationArea area, float minDistance, bool facingRight)
        {
            List<Vector2> positions = new List<Vector2>();
            var rng = GameManager.GetInstance().rng;
            Vector2 center = area.GetCenter();

            // First unit at center
            positions.Add(center);

            if (unitCount == 1) return positions; // Return early for single unit

            // Use Poisson disc sampling for organic clustering
            List<Vector2> spawnPoints = new List<Vector2> { center };

            while (positions.Count < unitCount && spawnPoints.Count > 0)
            {
                int spawnIndex = rng.Next(spawnPoints.Count);
                Vector2 spawnCenter = spawnPoints[spawnIndex];

                bool foundValidPoint = false;

                // Try to find a valid position near the spawn point
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    float angle = (float)(rng.NextDouble() * 2 * Mathf.PI);
                    float distance = (float)(rng.NextDouble() * minDistance + minDistance);

                    Vector2 candidate = spawnCenter + new Vector2(
                        distance * Mathf.Cos(angle),
                        distance * Mathf.Sin(angle)
                    );

                    // Check if within area bounds
                    if (candidate.x < area.minX || candidate.x > area.maxX ||
                        candidate.y < area.minY || candidate.y > area.maxY)
                        continue;

                    // Check distance from existing positions
                    bool tooClose = false;
                    foreach (var pos in positions)
                    {
                        if (Vector2.Distance(candidate, pos) < minDistance * 0.9f)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (!tooClose)
                    {
                        positions.Add(candidate);
                        spawnPoints.Add(candidate);
                        foundValidPoint = true;
                        break;
                    }
                }

                if (!foundValidPoint)
                    spawnPoints.RemoveAt(spawnIndex);
            }

            return positions;
        }

        public override float GetPreferenceScore(List<UnitStartingData> units)
        {
            // Clusters work well with homogeneous units
            var meleeCount = units.Count(u => !IsRangedUnit(u));
            var rangedCount = units.Count(u => IsRangedUnit(u));

            if (meleeCount == 0 || rangedCount == 0)
                return 1.3f;

            return 0.8f;
        }
    }

    // Wedge Formation - triangular formation pointing in facing direction
    public class WedgeFormation : StartingPositionsMiniFormations.MiniFormation
    {
        public override bool CanHandle(int unitCount)
        {
            // Wedges work best with triangular numbers
            return unitCount >= 3 && unitCount <= 15;
        }

        public override List<Vector2> GeneratePositions(int unitCount, StartingPositionsMiniFormations.FormationArea area, float minDistance, bool facingRight)
        {
            List<Vector2> positions = new List<Vector2>();
            Vector2 center = area.GetCenter();

            // Calculate rows needed for the wedge
            int rows = 1;
            int totalPlaced = 0;
            while (totalPlaced < unitCount)
            {
                totalPlaced += rows;
                if (totalPlaced < unitCount) rows++;
            }

            // Spacing between units
            float horizontalSpacing = minDistance * 1.2f;
            float verticalSpacing = minDistance * 1.1f;

            // Start position (tip of the wedge)
            float startX = facingRight ?
                center.x + (area.width * 0.3f) :
                center.x - (area.width * 0.3f);
            float startY = center.y;

            int placed = 0;
            for (int row = 0; row < rows && placed < unitCount; row++)
            {
                int unitsInRow = row + 1;

                // Center units in this row
                float rowWidth = (unitsInRow - 1) * horizontalSpacing;
                float rowStartY = startY - rowWidth / 2;

                for (int col = 0; col < unitsInRow && placed < unitCount; col++)
                {
                    float x = startX + (facingRight ? -row : row) * verticalSpacing;
                    float y = rowStartY + col * horizontalSpacing;

                    positions.Add(new Vector2(x, y));
                    placed++;
                }
            }

            return positions;
        }

        public override List<UnitStartingData> OrderUnits(List<UnitStartingData> units, bool facingRight)
        {
            var meleeUnits = units.Where(u => !IsRangedUnit(u)).ToList();
            var rangedUnits = units.Where(u => IsRangedUnit(u)).ToList();

            if (meleeUnits.Count == 0 || rangedUnits.Count == 0)
                return units;

            var orderedUnits = new List<UnitStartingData>();

            // Put melee at the tip and edges, ranged in the center/back
            // First unit is always melee (tip of wedge)
            if (meleeUnits.Count > 0)
            {
                orderedUnits.Add(meleeUnits[0]);
                meleeUnits.RemoveAt(0);
            }

            // Alternate for the rest, prioritizing melee for edges
            int totalRemaining = meleeUnits.Count + rangedUnits.Count;
            bool preferMelee = true;

            while (orderedUnits.Count < units.Count)
            {
                if (preferMelee && meleeUnits.Count > 0)
                {
                    orderedUnits.Add(meleeUnits[0]);
                    meleeUnits.RemoveAt(0);
                } else if (rangedUnits.Count > 0)
                {
                    orderedUnits.Add(rangedUnits[0]);
                    rangedUnits.RemoveAt(0);
                } else if (meleeUnits.Count > 0)
                {
                    orderedUnits.Add(meleeUnits[0]);
                    meleeUnits.RemoveAt(0);
                }

                preferMelee = !preferMelee;
            }

            return orderedUnits;
        }
    }

    // Diamond Formation - four-pointed formation
    public class DiamondFormation : StartingPositionsMiniFormations.MiniFormation
    {
        public override bool CanHandle(int unitCount)
        {
            // Works best with 4n+1 units (center + equal on each point)
            return unitCount >= 5 && unitCount <= 17;
        }

        public override List<Vector2> GeneratePositions(int unitCount, StartingPositionsMiniFormations.FormationArea area, float minDistance, bool facingRight)
        {
            List<Vector2> positions = new List<Vector2>();
            Vector2 center = area.GetCenter();

            // Adjust diamond size based on area
            float maxDimension = Mathf.Min(area.width, area.height) * 0.8f;

            if (unitCount == 1)
            {
                positions.Add(center);
                return positions;
            }

            // For diamond pattern, we want to distribute units along the edges
            int unitsPerSide = (unitCount - 1) / 4; // Exclude center if present
            int remainder = (unitCount - 1) % 4;

            // Define diamond vertices
            Vector2[] vertices = new Vector2[]
            {
                center + new Vector2(maxDimension / 2, 0),    // Right
                center + new Vector2(0, maxDimension / 2),    // Top
                center + new Vector2(-maxDimension / 2, 0),   // Left
                center + new Vector2(0, -maxDimension / 2)    // Bottom
            };

            // Place center unit if odd number
            if (unitCount % 2 == 1)
                positions.Add(center);

            // Place units along diamond edges
            for (int side = 0; side < 4; side++)
            {
                Vector2 start = vertices[side];
                Vector2 end = vertices[(side + 1) % 4];

                int unitsOnThisSide = unitsPerSide + (side < remainder ? 1 : 0);

                for (int i = 0; i <= unitsOnThisSide; i++)
                {
                    float t = unitsOnThisSide == 0 ? 0.5f : (float)i / unitsOnThisSide;
                    Vector2 pos = Vector2.Lerp(start, end, t);

                    // Don't place at vertices (avoid duplicates)
                    if (i > 0 || side == 0)
                        positions.Add(pos);
                }
            }

            return positions.Take(unitCount).ToList();
        }

        public override List<UnitStartingData> OrderUnits(List<UnitStartingData> units, bool facingRight)
        {
            var meleeUnits = units.Where(u => !IsRangedUnit(u)).ToList();
            var rangedUnits = units.Where(u => IsRangedUnit(u)).ToList();

            if (meleeUnits.Count == 0 || rangedUnits.Count == 0)
                return units;

            // For diamonds, put ranged in center and melee on the edges
            var orderedUnits = new List<UnitStartingData>();

            // If we have a center position (odd count), prefer ranged there
            if (units.Count % 2 == 1 && rangedUnits.Count > 0)
            {
                orderedUnits.Add(rangedUnits[0]);
                rangedUnits.RemoveAt(0);
            }

            // Fill the rest with melee priority on edges
            while (meleeUnits.Count > 0 || rangedUnits.Count > 0)
            {
                if (meleeUnits.Count > 0)
                {
                    orderedUnits.Add(meleeUnits[0]);
                    meleeUnits.RemoveAt(0);
                }
                if (rangedUnits.Count > 0 && orderedUnits.Count < units.Count)
                {
                    orderedUnits.Add(rangedUnits[0]);
                    rangedUnits.RemoveAt(0);
                }
            }

            return orderedUnits;
        }
    }
}