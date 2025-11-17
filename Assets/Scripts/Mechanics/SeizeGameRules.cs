using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SeizeGameRules : GameRules
{
    [SerializeField] private int _playerTeamId = 1;

    private SeizeGameOverCondition _seizeCondition;

    protected override void Awake()
    {
        // Check if components are assigned in inspector first
        if (_leaderboard == null)
            _leaderboard = GetComponent<LeaderboardBase>();

        if (_gameOverCondition == null)
            _gameOverCondition = GetComponent<GameOverCondition>();

        // If still null, create default components for this game mode
        if (_leaderboard == null)
            _leaderboard = gameObject.AddComponent<SeizeLeaderboard>();

        if (_gameOverCondition == null)
            _gameOverCondition = gameObject.AddComponent<SeizeGameOverCondition>();

        // Cache reference to specific game over condition type
        _seizeCondition = _gameOverCondition as SeizeGameOverCondition;

        if (_leaderboard == null || _gameOverCondition == null)
            Debug.LogError("SeizeGameRules requires Leaderboard and GameOverCondition components!");
    }

    protected override IEnumerator GameOverSequence()
    {
        yield return null;

        if (_leaderboard != null)
            _leaderboard.StopObserving();

        Unit capturer = _seizeCondition?.GetCapturer();
        Debug.Log($"Victory! Target seized by {capturer?.unitName ?? "Unknown"}!");

        // Notify camera about the capture
        var cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null && capturer != null)
        {
            cameraController.ShowCriticalEvent(capturer, null, 5f);
        }

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

    // Optional: Methods to get game state
    public Vector2 GetTargetPoint() => _seizeCondition?.GetTargetPoint() ?? Vector2.zero;
    public float GetClosestPlayerDistance() => _seizeCondition?.GetClosestDistance() ?? float.MaxValue;
    public bool IsPlayerTeamAlive() => _factionUnitCounts.ContainsKey(_playerTeamId) && _factionUnitCounts[_playerTeamId] > 0;
}