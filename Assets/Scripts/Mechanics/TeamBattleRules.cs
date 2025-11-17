using UnityEngine;
using System.Collections;

public class TeamBattleRules : GameRules
{
    protected override void Awake()
    {
        // Check if components are assigned in inspector first
        if (_leaderboard == null)
            _leaderboard = GetComponent<LeaderboardBase>();

        if (_gameOverCondition == null)
            _gameOverCondition = GetComponent<GameOverCondition>();

        // If still null, create default components for this game mode
        if (_leaderboard == null)
            _leaderboard = gameObject.AddComponent<TeamBattleLeaderboard>();

        if (_gameOverCondition == null)
            _gameOverCondition = gameObject.AddComponent<TeamBattleGameOverCondition>();

        if (_leaderboard == null || _gameOverCondition == null)
            Debug.LogError("TeamBattleRules requires Leaderboard and GameOverCondition components!");
    }

    protected override void Start()
    {
        base.Start();

        // Get team info
        int numberOfTeams = TeamBattleUtility.DetermineNumberOfTeams(
            MenuSettings.Instance.playerNames.Length,
            MenuSettings.Instance.numberOfWinners
        );

        Debug.Log($"Team Battle mode started! {numberOfTeams} teams competing until {MenuSettings.Instance.numberOfWinners} survivor(s) remain.");
    }

    protected override IEnumerator GameOverSequence()
    {
        Debug.Log("Team Battle ended!");
        return base.GameOverSequence();
    }
}