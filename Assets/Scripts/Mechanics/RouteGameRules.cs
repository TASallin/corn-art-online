using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RouteGameRules : GameRules
{
    //[SerializeField] private int _bossTeamId = 2; // Team with bosses to defeat
    [SerializeField] private int _playerTeamId = 1; // Team working to defeat bosses

    private RouteGameOverCondition _routeCondition;

    protected override void Awake()
    {
        // Check if components are assigned in inspector first
        if (_leaderboard == null)
            _leaderboard = GetComponent<LeaderboardBase>();

        if (_gameOverCondition == null)
            _gameOverCondition = GetComponent<GameOverCondition>();

        // If still null, create default components for this game mode
        if (_leaderboard == null)
            _leaderboard = gameObject.AddComponent<RouteLeaderboard>();

        if (_gameOverCondition == null)
            _gameOverCondition = gameObject.AddComponent<RouteGameOverCondition>();

        // Cache reference to specific game over condition type
        _routeCondition = _gameOverCondition as RouteGameOverCondition;

        if (_leaderboard == null || _gameOverCondition == null)
            Debug.LogError("RouteGameRules requires Leaderboard and GameOverCondition components!");
    }

    protected override IEnumerator GameOverSequence()
    {
        yield return null;

        if (_leaderboard != null)
            _leaderboard.StopObserving();

        Debug.Log("Victory! All enemies defeated!");
        // You could play a victory sound/effect here

        // Slow motion
        Time.timeScale = _gameEndTimeScale;
        yield return new WaitForSecondsRealtime(_gameEndSlowMotionDuration);

        // Store leaderboard data
        if (_leaderboard != null)
        {
            LeaderboardData.Instance.SetLeaderboardData(
                _leaderboard.GetUnitsInRank(),
                _leaderboard.GetScores(),
                DetermineNumberOfWinners(),
                _leaderboard.GetEliminationTracker(), // Now returns Dictionary<int, EliminatorData>
                _leaderboard.GetDefeatedEnemiesTracker()
            );
        }

        // Return time to normal
        Time.timeScale = 1f;

        // Load the leaderboard scene
        SceneManager.LoadScene("Leaderboard");
    }

    // Optional: Methods to get game state
    public int GetRemainingEnemyCount() => _routeCondition?.GetRemainingEnemyCount() ?? 0;
    public float GetEnemyDefeatProgress() => _routeCondition?.GetEnemyDefeatProgress() ?? 0f;
    public bool IsPlayerTeamAlive() => _factionUnitCounts.ContainsKey(_playerTeamId) && _factionUnitCounts[_playerTeamId] > 0;
}