using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class GameRules : MonoBehaviour
{
    [SerializeField] protected float _gameEndSlowMotionDuration = 2.0f;
    [SerializeField] protected float _gameEndTimeScale = 0.3f;

    protected LeaderboardBase _leaderboard;
    protected GameOverCondition _gameOverCondition;
    protected Dictionary<int, int> _factionUnitCounts = new Dictionary<int, int>();

    protected bool _isGameOver = false;

    protected virtual void Awake()
    {
        // Create components if not assigned in inspector
        if (_leaderboard == null)
            _leaderboard = GetComponent<LeaderboardBase>();

        if (_gameOverCondition == null)
            _gameOverCondition = GetComponent<GameOverCondition>();

        if (_leaderboard == null || _gameOverCondition == null)
            Debug.LogError("GameRules requires Leaderboard and GameOverCondition components!");
    }

    protected virtual void Start()
    {
        _gameOverCondition.OnGameOver += HandleGameOver;
    }

    protected virtual void OnDestroy()
    {
        if (_gameOverCondition != null)
            _gameOverCondition.OnGameOver -= HandleGameOver;
    }

    public virtual void RegisterUnit(Unit unit)
    {
        int factionId = unit.teamID;

        if (!_factionUnitCounts.ContainsKey(factionId))
            _factionUnitCounts[factionId] = 0;

        _factionUnitCounts[factionId]++;

        // Notify game over condition about new unit
        _gameOverCondition?.RegisterUnit(unit);
    }

    public virtual void UnregisterUnit(Unit unit)
    {
        int factionId = unit.teamID;

        if (_factionUnitCounts.ContainsKey(factionId))
        {
            _factionUnitCounts[factionId]--;
            if (_factionUnitCounts[factionId] <= 0)
                _factionUnitCounts.Remove(factionId);
        }

        // Notify game over condition about removed unit
        _gameOverCondition?.UnregisterUnit(unit);
    }

    public virtual void HandleGameOver()
    {
        if (_isGameOver)
            return;

        _isGameOver = true;

        // Stop leaderboard from observing
        //if (_leaderboard != null)
        //    _leaderboard.StopObserving();

        // Slow down time
        StartCoroutine(GameOverSequence());
    }

    protected virtual int DetermineNumberOfWinners()
    {
        // Try to get settings from MenuSettings, default to 1 if not found
        int numberOfWinners = 1; // Default fallback

        try
        {
            if (MenuSettings.Instance != null)
            {
                numberOfWinners = MenuSettings.Instance.numberOfWinners;

                // Ensure we always have at least 1 winner
                if (numberOfWinners < 1)
                    numberOfWinners = 1;

                Debug.Log($"Number of winners from MenuSettings: {numberOfWinners}");
            } else
            {
                Debug.LogWarning("MenuSettings.Instance is null, defaulting to 1 winner");
            }
        } catch (System.Exception e)
        {
            Debug.LogError($"Error accessing MenuSettings: {e.Message}");
        }
        return numberOfWinners;
    }

    // In GameOverSequence:
    protected virtual IEnumerator GameOverSequence()
    {
        yield return null;

        Observable.NotifyObservers(new EventData(EventType.GameOver));

        if (_leaderboard != null)
            _leaderboard.StopObserving();

        // Slow motion
        Time.timeScale = _gameEndTimeScale;

        yield return new WaitForSecondsRealtime(_gameEndSlowMotionDuration);

        // Store leaderboard data before changing scene
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

    // Optional: Restart game
    public virtual void RestartGame()
    {
        Time.timeScale = 1f;
        _isGameOver = false;
        // Implementation depends on your scene management
    }

    // Get current faction unit counts
    public Dictionary<int, int> GetFactionUnitCounts()
    {
        return new Dictionary<int, int>(_factionUnitCounts); // Return a copy
    }

    public int GetTotalUnitCount()
    {
        int total = 0;
        foreach (var count in _factionUnitCounts.Values)
            total += count;
        return total;
    }
}