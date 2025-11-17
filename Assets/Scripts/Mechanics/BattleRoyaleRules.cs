using UnityEngine;
using System.Collections;

public class BattleRoyaleRules : GameRules
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
            _leaderboard = gameObject.AddComponent<BattleRoyaleLeaderboard>();

        if (_gameOverCondition == null)
            _gameOverCondition = gameObject.AddComponent<BattleRoyaleGameOverCondition>();

        if (_leaderboard == null || _gameOverCondition == null)
            Debug.LogError("BattleRoyaleRules requires Leaderboard and GameOverCondition components!");
    }

    protected override void Start()
    {
        base.Start();
        Debug.Log($"Battle Royale mode started! Playing until {MenuSettings.Instance.numberOfWinners} survivor(s) remain.");
    }

    protected override IEnumerator GameOverSequence()
    {
        Debug.Log("Battle Royale ended!");
        return base.GameOverSequence();
    }
}