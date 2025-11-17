using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SeizeGameOverCondition : GameOverCondition
{
    [SerializeField] private int _playerTeamId = 1;
    [SerializeField] private float _captureRadius = 0.5f;

    private Vector2 _targetPoint;
    private Unit _capturer;
    private GameObject _targetPrefab;

    // Track player units separately
    private List<Unit> _playerUnits = new List<Unit>();
    // Only populate this when game ends
    private Dictionary<Unit, float> _unitDistances = new Dictionary<Unit, float>();

    public void Initialize(GameObject targetPrefab)
    {
        _targetPrefab = targetPrefab;
        CalculateTargetPoint();
        CreateTargetVisual();
    }

    private void CalculateTargetPoint()
    {
        GameManager gm = GameManager.GetInstance();
        float xPos = gm.xBound * UnityEngine.Random.Range(0.5f, 1.0f);
        float yPos = UnityEngine.Random.Range(-gm.yBound * 0.8f, gm.yBound * 0.8f);
        _targetPoint = new Vector2(xPos, yPos);
        Debug.Log($"Seize target point placed at: {_targetPoint}");
    }

    private void CreateTargetVisual()
    {
        if (_targetPrefab != null)
        {
            GameObject targetVisual = Instantiate(_targetPrefab,
                new Vector3(_targetPoint.x, _targetPoint.y, 0),
                Quaternion.identity);

            var targetPoint = targetVisual.GetComponent<SeizeTargetPoint>();
            if (targetPoint != null)
            {
                targetPoint.Initialize(this);
            }
        }
    }

    protected override EventType[] GetObservedEventTypes()
    {
        return new[] { EventType.UnitCreated, EventType.UnitDestroyed };
    }

    public override void OnEventReceived(EventData data)
    {
        switch (data.Type)
        {
            case EventType.UnitCreated:
                RegisterUnit(data.SourceUnit);
                break;
        }
    }

    public override void RegisterUnit(Unit unit)
    {
        base.RegisterUnit(unit);

        if (unit.teamID == _playerTeamId)
        {
            _playerUnits.Add(unit);
            Debug.Log($"Registered player unit: {unit.unitName}");
        }
    }

    // Calculate all unit distances at the moment of capture
    private void CalculateFinalDistances()
    {
        // Clear any existing distances and calculate fresh
        _unitDistances.Clear();

        Debug.Log($"Calculating final distances for {_playerUnits.Count} player units");

        foreach (var unit in _playerUnits)
        {
            if (unit != null && unit.GetAlive())
            {
                float distance = Vector2.Distance(
                    new Vector2(unit.transform.position.x, unit.transform.position.y),
                    _targetPoint);

                _unitDistances[unit] = distance;
                Debug.Log($"{unit.unitName}: Distance = {distance:F2}");
            }
        }

        // Set capturer's distance to 0
        if (_capturer != null)
        {
            _unitDistances[_capturer] = 0f;
            Debug.Log($"{_capturer.unitName} captured target - Distance set to 0");
        }

        // Debug all final distances
        Debug.Log("===== FINAL DISTANCES =====");
        foreach (var kvp in _unitDistances)
        {
            Debug.Log($"{kvp.Key.unitName}: {kvp.Value:F2}");
        }
        Debug.Log("=========================");
    }

    public void CheckCapture(Unit unit)
    {
        if (unit.teamID == _playerTeamId)
        {
            _capturer = unit;
            Debug.Log($"Target seized by {unit.unitName}!");

            // Calculate all final distances before game over
            CalculateFinalDistances();

            TriggerGameOver();
        }
    }

    protected override void CheckGameOverCondition()
    {
        // Game over is triggered by collision check in CheckCapture method
    }

    public Vector2 GetTargetPoint() => _targetPoint;
    public Unit GetCapturer() => _capturer;
    public Dictionary<Unit, float> GetUnitDistances() => new Dictionary<Unit, float>(_unitDistances);
    public float GetClosestDistance()
    {
        if (_unitDistances.Count == 0) return float.MaxValue;
        return _unitDistances.Values.Min();
    }

    public float GetMaxPossibleDistance()
    {
        GameManager gm = GameManager.GetInstance();

        // Calculate the maximum possible distance within bounds
        // Get the corners of the playfield
        Vector2[] corners = new Vector2[]
        {
        new Vector2(-gm.xBound, -gm.yBound),
        new Vector2(-gm.xBound, gm.yBound),
        new Vector2(gm.xBound, -gm.yBound),
        new Vector2(gm.xBound, gm.yBound)
        };

        // Find the maximum distance from the target to any corner
        float maxDistance = corners.Max(corner =>
            Vector2.Distance(corner, _targetPoint));

        // Add 1 to ensure it's greater than any possible in-bounds distance
        return maxDistance + 1f;
    }
}