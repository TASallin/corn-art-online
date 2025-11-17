using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SurviveGameRules : GameRules
{
    [SerializeField] private int _playerTeamId = 1; // Team trying to survive
    private float _gameStartTime;
    private SurviveGameOverCondition _surviveCondition;

    protected override void Awake()
    {
        // Check if components are assigned in inspector first
        if (_leaderboard == null)
            _leaderboard = GetComponent<LeaderboardBase>();

        if (_gameOverCondition == null)
            _gameOverCondition = GetComponent<GameOverCondition>();

        // If still null, create default components for this game mode
        if (_leaderboard == null)
            _leaderboard = gameObject.AddComponent<SurviveLeaderboard>();

        if (_gameOverCondition == null)
            _gameOverCondition = gameObject.AddComponent<SurviveGameOverCondition>();

        // Cache reference to specific game over condition type
        _surviveCondition = _gameOverCondition as SurviveGameOverCondition;

        if (_leaderboard == null || _gameOverCondition == null)
            Debug.LogError("SurviveGameRules requires Leaderboard and GameOverCondition components!");
    }

    protected override void Start()
    {
        base.Start();
        _gameStartTime = Time.time;
    }

    protected override IEnumerator GameOverSequence()
    {
        yield return null;

        if (_leaderboard != null)
            _leaderboard.StopObserving();

        float survivedTime = Time.time - _gameStartTime;
        Debug.Log($"Survival Game Over! Time survived: {survivedTime:F1} seconds");

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
                _leaderboard.GetEliminationTracker(),
                _leaderboard.GetDefeatedEnemiesTracker()
            );
        }

        // Return time to normal
        Time.timeScale = 1f;

        // Load the leaderboard scene
        SceneManager.LoadScene("Leaderboard");
    }

    // Methods to get game state
    public float GetSurvivedTime() => Time.time - _gameStartTime;
    public int GetRemainingPlayerCount() => _factionUnitCounts.ContainsKey(_playerTeamId) ? _factionUnitCounts[_playerTeamId] : 0;
}