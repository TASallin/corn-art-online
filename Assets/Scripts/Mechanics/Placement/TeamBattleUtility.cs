using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class TeamBattleUtility
{
    private const int MAX_TEAMS = 20;

    /// <summary>
    /// Determines the optimal number of teams for team battle mode based on the number of players and winners.
    /// </summary>
    /// <param name="totalPlayers">Total number of players</param>
    /// <param name="numberOfWinners">Number of winners</param>
    /// <returns>The number of teams, or -1 if no valid configuration exists</returns>
    public static int DetermineNumberOfTeams(int totalPlayers, int numberOfWinners)
    {
        // Validate inputs
        if (totalPlayers <= 1 || numberOfWinners < 1 || numberOfWinners > totalPlayers)
        {
            Debug.Log($"Invalid team battle configuration: {totalPlayers} players, {numberOfWinners} winners");
            return -1;
        }

        List<int> validTeamCounts = new List<int>();

        // Find all valid team counts
        // A valid team count must:
        // 1. Be a factor of totalPlayers (so teams are even)
        // 2. Result in numberOfWinners being a multiple of playersPerTeam
        // 3. Result in more than one player per team
        // 4. Not exceed MAX_TEAMS
        for (int teams = 2; teams <= Mathf.Min(totalPlayers / 2, MAX_TEAMS); teams++)
        {
            if (totalPlayers % teams == 0) // Is a factor of totalPlayers
            {
                int playersPerTeam = totalPlayers / teams;
                if (playersPerTeam > 1 && numberOfWinners % playersPerTeam == 0) // Winners is multiple of team size
                {
                    validTeamCounts.Add(teams);
                }
            }
        }

        if (validTeamCounts.Count == 0)
        {
            Debug.Log($"No valid team configuration found for {totalPlayers} players with {numberOfWinners} winners");
            return -1;
        }

        // Choose randomly with weight based on proximity to square root of player count
        float targetTeamCount = Mathf.Sqrt(totalPlayers);
        List<float> weights = new List<float>();

        foreach (int teamCount in validTeamCounts)
        {
            float distance = Mathf.Abs(teamCount - targetTeamCount);
            // Weight is inversely proportional to distance from target
            // Add 1 to avoid division by zero
            float weight = 1f / (distance + 1f);
            weights.Add(weight);
        }

        // Normalize weights to create probability distribution
        float totalWeight = weights.Sum();
        float randomValue = (float)GameManager.GetInstance().rng.NextDouble() * totalWeight;

        float cumulativeWeight = 0f;
        for (int i = 0; i < validTeamCounts.Count; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue <= cumulativeWeight)
            {
                int selectedTeams = validTeamCounts[i];
                Debug.Log($"Team Battle: {totalPlayers} players, {numberOfWinners} winners -> {selectedTeams} teams of {totalPlayers / selectedTeams} players each");
                return selectedTeams;
            }
        }

        // Fallback (should never reach here)
        return validTeamCounts[0];
    }

    /// <summary>
    /// Returns an array of team IDs to use, skipping teams 0 and 3
    /// </summary>
    /// <param name="numberOfTeams">Number of teams needed</param>
    /// <returns>Array of team IDs</returns>
    public static int[] GetValidTeamIds(int numberOfTeams)
    {
        if (numberOfTeams <= 0)
        {
            return new int[0];
        }

        List<int> teamIds = new List<int>();
        int currentId = 1; // Start from 1 (skip 0)

        while (teamIds.Count < numberOfTeams)
        {
            if (currentId != 3) // Skip team 3
            {
                teamIds.Add(currentId);
            }
            currentId++;
        }

        return teamIds.ToArray();
    }

    /// <summary>
    /// Debug helper to log all valid team configurations
    /// </summary>
    public static void LogValidConfigurations(int totalPlayers, int numberOfWinners)
    {
        Debug.Log($"Valid team configurations for {totalPlayers} players, {numberOfWinners} winners:");

        for (int teams = 2; teams <= Mathf.Min(totalPlayers / 2, MAX_TEAMS); teams++)
        {
            if (totalPlayers % teams == 0)
            {
                int playersPerTeam = totalPlayers / teams;
                if (playersPerTeam > 1 && numberOfWinners % playersPerTeam == 0)
                {
                    Debug.Log($"  - {teams} teams of {playersPerTeam} players each");
                }
            }
        }
    }
}