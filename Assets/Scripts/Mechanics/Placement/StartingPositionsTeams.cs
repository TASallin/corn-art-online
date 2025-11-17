using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class StartingPositionsTeams
{
    public static UnitStartingData[] CalculateTeamBattlePositions(
        TeamClassComposition.TeamComposition teamComposition,
        int numberOfTeams,
        int[] teamIds)
    {
        // Validate inputs
        if (numberOfTeams <= 0 || teamIds == null || teamIds.Length != numberOfTeams)
        {
            Debug.LogError($"Invalid team battle parameters: {numberOfTeams} teams, {teamIds?.Length} team IDs");
            return new UnitStartingData[0];
        }

        // Get all units from the composition
        List<UnitStartingData> allUnits = new List<UnitStartingData>();
        foreach (var classCount in teamComposition.classDistribution)
        {
            allUnits.AddRange(classCount.unitData);
        }

        int totalUnits = allUnits.Count;
        int unitsPerTeam = totalUnits / numberOfTeams;
        int remainder = totalUnits % numberOfTeams;

        // Get map bounds
        float xBound = GameManager.GetInstance().xBound;
        float yBound = GameManager.GetInstance().yBound;
        float minDistance = 0.63f * 2 * 1.1f;

        List<UnitStartingData> finalPositions = new List<UnitStartingData>();

        // Special case: 2 teams - use existing formation
        if (numberOfTeams == 2)
        {
            return GenerateTwoTeamPositions(allUnits, teamIds, unitsPerTeam, remainder, xBound, yBound, minDistance);
        }

        // For multiple teams, divide the map into sectors
        var teamAreas = DivideMapIntoTeamAreas(numberOfTeams, xBound, yBound);

        // Distribute units to teams
        int unitIndex = 0;
        for (int teamIndex = 0; teamIndex < numberOfTeams; teamIndex++)
        {
            int teamId = teamIds[teamIndex];
            int teamUnitCount = unitsPerTeam;

            // Distribute remainder units to first teams
            if (teamIndex < remainder)
            {
                teamUnitCount++;
            }

            // Get units for this team
            List<UnitStartingData> teamUnits = new List<UnitStartingData>();
            for (int i = 0; i < teamUnitCount && unitIndex < allUnits.Count; i++)
            {
                teamUnits.Add(allUnits[unitIndex++]);
            }

            if (teamUnits.Count == 0) continue;

            // Generate positions for this team
            var teamArea = teamAreas[teamIndex];
            var teamPositions = GenerateTeamFormation(
                teamUnits,
                teamId,
                teamArea,
                minDistance,
                numberOfTeams
            );

            finalPositions.AddRange(teamPositions);
        }

        return finalPositions.ToArray();
    }

    private static UnitStartingData[] GenerateTwoTeamPositions(
        List<UnitStartingData> allUnits,
        int[] teamIds,
        int unitsPerTeam,
        int remainder,
        float xBound,
        float yBound,
        float minDistance)
    {
        // Split units between two teams
        List<UnitStartingData> team1Units = new List<UnitStartingData>();
        List<UnitStartingData> team2Units = new List<UnitStartingData>();

        int team1Count = unitsPerTeam + (remainder > 0 ? 1 : 0);

        for (int i = 0; i < allUnits.Count; i++)
        {
            if (i < team1Count)
            {
                team1Units.Add(allUnits[i]);
            } else
            {
                team2Units.Add(allUnits[i]);
            }
        }

        // Create team compositions for the existing two-team system
        var team1Comp = CreateTeamComposition(team1Units, teamIds[0]);
        var team2Comp = CreateTeamComposition(team2Units, teamIds[1]);

        // Use existing team battle position generation
        return StartingPositionsCommon.CalculateTeamBattlePositions(
            new TeamClassComposition.TeamComposition[] { team1Comp, team2Comp }
        );
    }

    private static TeamClassComposition.TeamComposition CreateTeamComposition(
        List<UnitStartingData> units,
        int teamId)
    {
        var comp = new TeamClassComposition.TeamComposition();
        comp.teamId = teamId;
        comp.classDistribution = new List<TeamClassComposition.ClassCount>();

        // Group units by class
        var unitsByClass = units.GroupBy(u => u.UnitClass.name);

        foreach (var classGroup in unitsByClass)
        {
            var classCount = new TeamClassComposition.ClassCount
            (
                classGroup.Key,
                classGroup.Count()
            );
            classCount.unitData = classGroup.ToList();
            comp.classDistribution.Add(classCount);
        }

        return comp;
    }

    private static List<TeamArea> DivideMapIntoTeamAreas(int numberOfTeams, float xBound, float yBound)
    {
        List<TeamArea> areas = new List<TeamArea>();

        // Calculate optimal grid layout
        int cols, rows;
        CalculateOptimalGrid(numberOfTeams, xBound, yBound, out cols, out rows);

        float cellWidth = (2 * xBound) / cols;
        float cellHeight = (2 * yBound) / rows;

        // Add small buffer between areas
        float buffer = 0.5f;

        int teamIndex = 0;
        for (int row = 0; row < rows && teamIndex < numberOfTeams; row++)
        {
            for (int col = 0; col < cols && teamIndex < numberOfTeams; col++)
            {
                float minX = -xBound + col * cellWidth + buffer;
                float maxX = -xBound + (col + 1) * cellWidth - buffer;
                float minY = -yBound + row * cellHeight + buffer;
                float maxY = -yBound + (row + 1) * cellHeight - buffer;

                // Calculate facing direction based on position
                FacingDirection facing = DetermineFacingDirection(col, row, cols, rows);

                areas.Add(new TeamArea(minX, maxX, minY, maxY, facing));
                teamIndex++;
            }
        }

        return areas;
    }

    private static void CalculateOptimalGrid(int numberOfTeams, float xBound, float yBound, out int cols, out int rows)
    {
        // Start with square root as baseline
        float sqrtTeams = Mathf.Sqrt(numberOfTeams);
        float aspectRatio = xBound / yBound;

        // Adjust for map aspect ratio
        cols = Mathf.CeilToInt(sqrtTeams * Mathf.Sqrt(aspectRatio));
        rows = Mathf.CeilToInt((float)numberOfTeams / cols);

        // Ensure we have enough cells
        while (cols * rows < numberOfTeams)
        {
            if (xBound > yBound)
                cols++;
            else
                rows++;
        }
    }

    private static FacingDirection DetermineFacingDirection(int col, int row, int totalCols, int totalRows)
    {
        // Teams on edges face toward center
        // Teams in center face outward based on their quadrant

        float colRatio = totalCols > 1 ? (float)col / (totalCols - 1) : 0.5f;
        float rowRatio = totalRows > 1 ? (float)row / (totalRows - 1) : 0.5f;

        // Edge cases
        if (col == 0) return FacingDirection.Right;
        if (col == totalCols - 1) return FacingDirection.Left;
        if (row == 0) return FacingDirection.Up;
        if (row == totalRows - 1) return FacingDirection.Down;

        // Center teams - face based on quadrant
        if (colRatio < 0.5f && rowRatio < 0.5f) return FacingDirection.UpRight;
        if (colRatio >= 0.5f && rowRatio < 0.5f) return FacingDirection.UpLeft;
        if (colRatio < 0.5f && rowRatio >= 0.5f) return FacingDirection.DownRight;
        return FacingDirection.DownLeft;
    }

    private static List<UnitStartingData> GenerateTeamFormation(
        List<UnitStartingData> teamUnits,
        int teamId,
        TeamArea area,
        float minDistance,
        int totalTeams)
    {
        var rng = GameManager.GetInstance().rng;
        List<UnitStartingData> positionedUnits = new List<UnitStartingData>();

        // Determine formation strategy based on team size and total teams
        bool useMiniFormations = totalTeams > 4 || teamUnits.Count < 15 || rng.NextDouble() < 0.6;

        if (useMiniFormations)
        {
            // Use mini formations for smaller teams or many teams
            var miniFormationUnits = StartingPositionsMiniFormations.GenerateMiniFormations(
                teamUnits,
                area.minX,
                area.maxX,
                area.minY,
                area.maxY,
                minDistance,
                IsFacingRight(area.facing)
            );

            // Update team IDs
            foreach (var unit in miniFormationUnits)
            {
                positionedUnits.Add(new UnitStartingData(
                    unit.Position,
                    teamId,
                    unit.UnitClass,
                    unit.UnitName,
                    unit.ScaleFactor
                ));
            }
        } else
        {
            // Use standard formation for larger teams
            var teamComp = CreateTeamComposition(teamUnits, teamId);
            var standardFormation = StartingPositionsCommon.GenerateFormationForSingleTeam(
                teamComp,
                area.minX,
                area.maxX,
                area.minY,
                area.maxY,
                minDistance,
                IsFacingRight(area.facing)
            );

            positionedUnits.AddRange(standardFormation);
        }

        // Apply rotation based on facing direction if needed
        if (area.facing != FacingDirection.Right && area.facing != FacingDirection.Left)
        {
            positionedUnits = ApplyRotationToPositions(positionedUnits, area);
        }

        return positionedUnits;
    }

    private static bool IsFacingRight(FacingDirection facing)
    {
        return facing == FacingDirection.Right ||
               facing == FacingDirection.UpRight ||
               facing == FacingDirection.DownRight;
    }

    private static List<UnitStartingData> ApplyRotationToPositions(
        List<UnitStartingData> units,
        TeamArea area)
    {
        Vector2 center = new Vector2(
            (area.minX + area.maxX) / 2,
            (area.minY + area.maxY) / 2
        );

        float rotation = GetRotationAngle(area.facing);
        List<UnitStartingData> rotatedUnits = new List<UnitStartingData>();

        foreach (var unit in units)
        {
            Vector2 rotatedPos = RotatePoint(unit.Position, center, rotation);
            rotatedUnits.Add(new UnitStartingData(
                rotatedPos,
                unit.TeamId,
                unit.UnitClass,
                unit.UnitName,
                unit.ScaleFactor
            ));
        }

        return rotatedUnits;
    }

    private static float GetRotationAngle(FacingDirection facing)
    {
        switch (facing)
        {
            case FacingDirection.Up: return 90f;
            case FacingDirection.Down: return -90f;
            case FacingDirection.UpRight: return 45f;
            case FacingDirection.UpLeft: return 135f;
            case FacingDirection.DownRight: return -45f;
            case FacingDirection.DownLeft: return -135f;
            default: return 0f;
        }
    }

    private static Vector2 RotatePoint(Vector2 point, Vector2 pivot, float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        Vector2 translated = point - pivot;
        Vector2 rotated = new Vector2(
            translated.x * cos - translated.y * sin,
            translated.x * sin + translated.y * cos
        );

        return rotated + pivot;
    }

    private class TeamArea
    {
        public float minX, maxX, minY, maxY;
        public FacingDirection facing;

        public TeamArea(float minX, float maxX, float minY, float maxY, FacingDirection facing)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
            this.facing = facing;
        }
    }

    private enum FacingDirection
    {
        Right,
        Left,
        Up,
        Down,
        UpRight,
        UpLeft,
        DownRight,
        DownLeft
    }
}