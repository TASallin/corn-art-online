using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public static class StartingPositionsMiniFormations
{
    // Base class for all mini formations
    public abstract class MiniFormation
    {
        public abstract bool CanHandle(int unitCount);
        public abstract List<Vector2> GeneratePositions(int unitCount, FormationArea area, float minDistance, bool facingRight);

        // Determines if this formation prefers certain unit types
        public virtual float GetPreferenceScore(List<UnitStartingData> units)
        {
            // Default: no preference
            return 1.0f;
        }

        // Orders units within the formation based on their classes
        public virtual List<UnitStartingData> OrderUnits(List<UnitStartingData> units, bool facingRight)
        {
            // Default: put melee in front, ranged in back
            var orderedUnits = new List<UnitStartingData>();

            var meleeUnits = units.Where(u => !IsRangedUnit(u)).ToList();
            var rangedUnits = units.Where(u => IsRangedUnit(u)).ToList();

            if (facingRight)
            {
                // Melee first (rightmost), then ranged
                orderedUnits.AddRange(meleeUnits);
                orderedUnits.AddRange(rangedUnits);
            } else
            {
                // Ranged first (rightmost when facing left), then melee
                orderedUnits.AddRange(rangedUnits);
                orderedUnits.AddRange(meleeUnits);
            }

            return orderedUnits;
        }

        protected bool IsRangedUnit(UnitStartingData unit)
        {
            return ClassDataManager.Instance.GetClassDataByName(unit.UnitClass.name).IsRangedClass();
        }
    }

    // Line formation
    public class LineFormation : MiniFormation
    {
        public override bool CanHandle(int unitCount)
        {
            return unitCount >= 1; // Lines work with any number
        }

        public override List<Vector2> GeneratePositions(int unitCount, FormationArea area, float minDistance, bool facingRight)
        {
            List<Vector2> positions = new List<Vector2>();
            var rng = GameManager.GetInstance().rng;

            if (unitCount == 1)
            {
                positions.Add(area.GetCenter());
                return positions;
            }

            // Determine if horizontal or vertical based on area shape
            bool horizontal = area.width > area.height;

            if (horizontal)
            {
                float spacing = Mathf.Min(area.width / (unitCount - 1), minDistance * 1.2f);
                float startX = area.minX + (area.width - (spacing * (unitCount - 1))) / 2;
                float y = area.GetCenter().y;

                for (int i = 0; i < unitCount; i++)
                {
                    float x = startX + (i * spacing);
                    float randomOffset = (float)(rng.NextDouble() * minDistance / 2 - minDistance / 4);
                    positions.Add(new Vector2(x, y + randomOffset));
                }
            } else
            {
                float spacing = Mathf.Min(area.height / (unitCount - 1), minDistance * 1.2f);
                float startY = area.minY + (area.height - (spacing * (unitCount - 1))) / 2;
                float x = area.GetCenter().x;

                for (int i = 0; i < unitCount; i++)
                {
                    float y = startY + (i * spacing);
                    float randomOffset = (float)(rng.NextDouble() * minDistance / 2 - minDistance / 4);
                    positions.Add(new Vector2(x + randomOffset, y));
                }
            }

            return positions;
        }

        public override List<UnitStartingData> OrderUnits(List<UnitStartingData> units, bool facingRight)
        {
            var orderedUnits = new List<UnitStartingData>();

            var meleeUnits = units.Where(u => !IsRangedUnit(u)).ToList();
            var rangedUnits = units.Where(u => IsRangedUnit(u)).ToList();

            // For lines, alternate unit types if we have both
            if (meleeUnits.Count > 0 && rangedUnits.Count > 0)
            {
                int meleeIndex = 0, rangedIndex = 0;
                bool startWithMelee = facingRight;

                while (meleeIndex < meleeUnits.Count || rangedIndex < rangedUnits.Count)
                {
                    if (startWithMelee && meleeIndex < meleeUnits.Count)
                    {
                        orderedUnits.Add(meleeUnits[meleeIndex++]);
                    } else if (!startWithMelee && rangedIndex < rangedUnits.Count)
                    {
                        orderedUnits.Add(rangedUnits[rangedIndex++]);
                    } else if (meleeIndex < meleeUnits.Count)
                    {
                        orderedUnits.Add(meleeUnits[meleeIndex++]);
                    } else if (rangedIndex < rangedUnits.Count)
                    {
                        orderedUnits.Add(rangedUnits[rangedIndex++]);
                    }

                    startWithMelee = !startWithMelee;
                }
            } else
            {
                // All same type, just return as is
                orderedUnits.AddRange(units);
            }

            return orderedUnits;
        }
    }

    // Arc formation
    public class ArcFormation : MiniFormation
    {
        public override bool CanHandle(int unitCount)
        {
            return unitCount >= 3; // Arcs need at least 3 units to look good
        }

        public override List<Vector2> GeneratePositions(int unitCount, FormationArea area, float minDistance, bool facingRight)
        {
            List<Vector2> positions = new List<Vector2>();
            Vector2 center = area.GetCenter();
            var rng = GameManager.GetInstance().rng;

            // Calculate radius based on area size and unit count
            float radius = Mathf.Min(area.width, area.height) * 0.4f;

            // Arc angle range (120 degrees = 2π/3 radians)
            float arcAngle = 2f * Mathf.PI / 3f;

            // Calculate start angle based on facing direction
            float startAngle = facingRight ? Mathf.PI + (Mathf.PI - arcAngle) / 2 : (Mathf.PI - arcAngle) / 2;

            for (int i = 0; i < unitCount; i++)
            {
                float angle = startAngle + (arcAngle * i / (unitCount - 1));
                float x = center.x + radius * Mathf.Cos(angle);
                float y = center.y + radius * Mathf.Sin(angle);

                // Add small random offset
                x += (float)(rng.NextDouble() * minDistance / 2 - minDistance / 4);
                y += (float)(rng.NextDouble() * minDistance / 2 - minDistance / 4);

                positions.Add(new Vector2(x, y));
            }

            return positions;
        }

        public override float GetPreferenceScore(List<UnitStartingData> units)
        {
            // Arcs work better with homogeneous units or balanced mixed units
            var meleeCount = units.Count(u => !IsRangedUnit(u));
            var rangedCount = units.Count(u => IsRangedUnit(u));

            if (meleeCount == 0 || rangedCount == 0)
            {
                return 1.2f; // Prefer homogeneous
            } else if (Math.Abs(meleeCount - rangedCount) <= 1)
            {
                return 1.1f; // Also good for balanced
            }
            return 0.9f; // Less preferred for imbalanced
        }

        public override List<UnitStartingData> OrderUnits(List<UnitStartingData> units, bool facingRight)
        {
            // For arcs, put melee on the flanks and ranged in the center
            var orderedUnits = new List<UnitStartingData>();

            var meleeUnits = units.Where(u => !IsRangedUnit(u)).ToList();
            var rangedUnits = units.Where(u => IsRangedUnit(u)).ToList();

            if (meleeUnits.Count == 0 || rangedUnits.Count == 0)
            {
                return units; // All same type
            }

            // Distribute symmetrically
            int totalPositions = units.Count;
            bool[] isMeleePosition = new bool[totalPositions];

            // Place melee on edges
            for (int i = 0; i < meleeUnits.Count; i++)
            {
                if (i % 2 == 0)
                {
                    isMeleePosition[i / 2] = true;
                } else
                {
                    isMeleePosition[totalPositions - 1 - (i / 2)] = true;
                }
            }

            // Fill positions
            int meleeIndex = 0, rangedIndex = 0;
            for (int i = 0; i < totalPositions; i++)
            {
                if (isMeleePosition[i] && meleeIndex < meleeUnits.Count)
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

            return orderedUnits;
        }
    }

    // Formation area class
    public class FormationArea
    {
        public float minX, maxX, minY, maxY;
        public float width, height;

        public FormationArea(float minX, float maxX, float minY, float maxY)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
            this.width = maxX - minX;
            this.height = maxY - minY;
        }

        public Vector2 GetCenter()
        {
            return new Vector2((minX + maxX) / 2, (minY + maxY) / 2);
        }
    }

    // List of available formations
    private static readonly List<MiniFormation> availableFormations = new List<MiniFormation>
    {
        new LineFormation(),
        new ArcFormation(),
        new StartingPositionsMiniFormationsExtra.GridFormation(),
        new StartingPositionsMiniFormationsExtra.CrossFormation(),
        new StartingPositionsMiniFormationsExtra.CircleFormation(),
        new StartingPositionsMiniFormationsExtra.ClusterFormation(),
        new StartingPositionsMiniFormationsExtra.WedgeFormation(),
        new StartingPositionsMiniFormationsExtra.DiamondFormation()
    };

    public static List<UnitStartingData> GenerateMiniFormations(
    List<UnitStartingData> units,
    float minX, float maxX, float minY, float maxY,
    float minDistance,
    bool facingRight)
    {
        List<UnitStartingData> positionedUnits = new List<UnitStartingData>();
        List<FormationArea> usedAreas = new List<FormationArea>();
        List<UnitStartingData> remainingUnits = new List<UnitStartingData>(units);
        var rng = GameManager.GetInstance().rng;

        // Sort units into groups based on type
        var unitGroups = GroupUnitsByType(remainingUnits);

        while (remainingUnits.Count > 0)
        {
            // Find an empty area for the next formation
            FormationArea area = FindEmptyArea(usedAreas, minX, maxX, minY, maxY, minDistance);
            if (area == null) break;

            // Determine formation size (1-8 units, or remaining units if less)
            int formationSize = Mathf.Min(
                remainingUnits.Count,
                rng.Next(1, 9)
            );

            // Select units for this formation (prefer whole groups)
            List<UnitStartingData> formationUnits = SelectUnitsForFormation(unitGroups, formationSize);

            // ISSUE 1: SelectUnitsForFormation might not return exactly formationSize units
            // Fix by checking actual count:
            if (formationUnits.Count == 0) continue;

            // Choose appropriate formation type
            var validFormations = availableFormations.Where(f => f.CanHandle(formationUnits.Count)).ToList();
            if (validFormations.Count == 0) continue;

            // Weight selection by preference score
            float totalScore = validFormations.Sum(f => f.GetPreferenceScore(formationUnits));
            float randomValue = (float)rng.NextDouble() * totalScore;

            MiniFormation selectedFormation = validFormations[0];
            float cumulativeScore = 0;
            foreach (var formation in validFormations)
            {
                cumulativeScore += formation.GetPreferenceScore(formationUnits);
                if (randomValue <= cumulativeScore)
                {
                    selectedFormation = formation;
                    break;
                }
            }

            // Generate positions for this formation
            List<Vector2> formationPositions = selectedFormation.GeneratePositions(
                formationUnits.Count,
                area,
                minDistance,
                facingRight
            );

            // Order units appropriately for the formation
            var orderedUnits = selectedFormation.OrderUnits(formationUnits, facingRight);

            // ISSUE 2: Make sure we don't have more units than positions
            int unitsToPlace = Mathf.Min(orderedUnits.Count, formationPositions.Count);

            // Assign positions to units
            for (int i = 0; i < unitsToPlace; i++)
            {
                var unit = orderedUnits[i];
                positionedUnits.Add(new UnitStartingData(
                    formationPositions[i],
                    unit.TeamId,
                    unit.UnitClass,
                    unit.UnitName,
                    unit.ScaleFactor
                ));

                // ISSUE 3: Only remove from remainingUnits, not from the selected units
                remainingUnits.Remove(unit);
            }

            // Fix for groups tracking - ensure we clean up groups correctly
            foreach (var unit in orderedUnits.Take(unitsToPlace))
            {
                foreach (var group in unitGroups.Values)
                {
                    group.Remove(unit);
                }
            }

            // Record used area with appropriate positioning
            var adjustedArea = AdjustAreaForUnitType(area, formationUnits, facingRight);
            usedAreas.Add(adjustedArea);
        }

        // IMPORTANT: Make sure we track the correct count
        Debug.Log($"Positioned {positionedUnits.Count} units, {remainingUnits.Count} remaining");

        // If any units remain, place them individually
        if (remainingUnits.Count > 0)
        {
            List<Vector2> remainingPositions = StartingPositionsCommon.GenerateTeamPositions(
                remainingUnits.Count, minX, maxX, minY, maxY, minDistance);

            for (int i = 0; i < remainingUnits.Count && i < remainingPositions.Count; i++)
            {
                positionedUnits.Add(new UnitStartingData(
                    remainingPositions[i],
                    remainingUnits[i].TeamId,
                    remainingUnits[i].UnitClass,
                    remainingUnits[i].UnitName,
                    remainingUnits[i].ScaleFactor
                ));
            }
        }

        // VERIFY: Log final count
        Debug.Log($"Total units placed: {positionedUnits.Count}, Expected: {units.Count}");

        return positionedUnits;
    }

    private static Dictionary<string, List<UnitStartingData>> GroupUnitsByType(List<UnitStartingData> units)
    {
        var groups = new Dictionary<string, List<UnitStartingData>>();

        foreach (var unit in units)
        {
            string type = ClassDataManager.Instance.GetClassDataByName(unit.UnitClass.name).IsRangedClass() ? "ranged" : "melee";
            if (!groups.ContainsKey(type))
            {
                groups[type] = new List<UnitStartingData>();
            }
            groups[type].Add(unit);
        }

        return groups;
    }

    private static List<UnitStartingData> SelectUnitsForFormation(
    Dictionary<string, List<UnitStartingData>> groups,
    int targetSize)
    {
        List<UnitStartingData> selected = new List<UnitStartingData>();
        var rng = GameManager.GetInstance().rng;

        // Create a working copy of groups to avoid modifying the original
        var workingGroups = new Dictionary<string, List<UnitStartingData>>();
        foreach (var kvp in groups)
        {
            workingGroups[kvp.Key] = new List<UnitStartingData>(kvp.Value);
        }

        // Try to select a whole group first
        var nonEmptyGroups = workingGroups.Where(g => g.Value.Count > 0).ToList();
        if (nonEmptyGroups.Count > 0)
        {
            var selectedGroup = nonEmptyGroups[rng.Next(nonEmptyGroups.Count)];

            // If group fits entirely, take it all
            if (selectedGroup.Value.Count <= targetSize)
            {
                selected.AddRange(selectedGroup.Value);
                // Clear the group from working copy
                workingGroups[selectedGroup.Key].Clear();
            } else
            {
                // Take exactly what we need
                for (int i = 0; i < targetSize; i++)
                {
                    int index = rng.Next(selectedGroup.Value.Count);
                    selected.Add(selectedGroup.Value[index]);
                    selectedGroup.Value.RemoveAt(index);
                }
                return selected; // Return early with exact count
            }
        }

        // If we need more units, take from remaining groups
        while (selected.Count < targetSize)
        {
            nonEmptyGroups = workingGroups.Where(g => g.Value.Count > 0).ToList();
            if (nonEmptyGroups.Count == 0) break;

            var randomGroup = nonEmptyGroups[rng.Next(nonEmptyGroups.Count)];
            if (randomGroup.Value.Count > 0)
            {
                int index = rng.Next(randomGroup.Value.Count);
                selected.Add(randomGroup.Value[index]);
                randomGroup.Value.RemoveAt(index);
            }
        }

        // Update the original groups to reflect what we took
        foreach (var kvp in groups)
        {
            kvp.Value.Clear();
            kvp.Value.AddRange(workingGroups[kvp.Key]);
        }

        return selected;
    }

    private static FormationArea AdjustAreaForUnitType(FormationArea area, List<UnitStartingData> units, bool facingRight)
    {
        // Check if all units are the same type
        bool allMelee = units.All(u => !ClassDataManager.Instance.GetClassDataByName(u.UnitClass.name).IsRangedClass());
        bool allRanged = units.All(u => ClassDataManager.Instance.GetClassDataByName(u.UnitClass.name).IsRangedClass());

        if (!allMelee && !allRanged)
            return area; // Mixed formation, use normal area

        // Adjust area position based on unit type
        float adjustment = area.width * 0.2f;

        if (facingRight)
        {
            // Melee formations pushed forward (right), ranged pulled back (left)
            if (allMelee)
                return new FormationArea(area.minX + adjustment, area.maxX + adjustment, area.minY, area.maxY);
            else
                return new FormationArea(area.minX - adjustment, area.maxX - adjustment, area.minY, area.maxY);
        } else
        {
            // Melee formations pushed forward (left), ranged pulled back (right)
            if (allMelee)
                return new FormationArea(area.minX - adjustment, area.maxX - adjustment, area.minY, area.maxY);
            else
                return new FormationArea(area.minX + adjustment, area.maxX + adjustment, area.minY, area.maxY);
        }
    }

    private static FormationArea FindEmptyArea(
        List<FormationArea> usedAreas,
        float minX, float maxX, float minY, float maxY,
        float minDistance)
    {
        const int MAX_ATTEMPTS = 50;
        float minAreaSize = minDistance * 2;
        var rng = GameManager.GetInstance().rng;

        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            // Generate random area size
            float areaWidth = (float)(rng.NextDouble() * ((maxX - minX) * 0.4f - minAreaSize) + minAreaSize);
            float areaHeight = (float)(rng.NextDouble() * ((maxY - minY) * 0.4f - minAreaSize) + minAreaSize);

            // Generate random position
            float areaMinX = (float)(rng.NextDouble() * (maxX - minX - areaWidth) + minX);
            float areaMinY = (float)(rng.NextDouble() * (maxY - minY - areaHeight) + minY);

            FormationArea candidateArea = new FormationArea(
                areaMinX,
                areaMinX + areaWidth,
                areaMinY,
                areaMinY + areaHeight
            );

            // Check if area overlaps with any used areas
            bool overlaps = false;
            foreach (var usedArea in usedAreas)
            {
                if (AreasOverlap(candidateArea, usedArea, minDistance))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                return candidateArea;
            }
        }

        return null;
    }

    private static bool AreasOverlap(FormationArea a1, FormationArea a2, float buffer)
    {
        return !(a1.maxX + buffer < a2.minX ||
                 a2.maxX + buffer < a1.minX ||
                 a1.maxY + buffer < a2.minY ||
                 a2.maxY + buffer < a1.minY);
    }
}