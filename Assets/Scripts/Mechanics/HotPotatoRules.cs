using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class HotPotatoRules : GameRules, IObserver
{
    [SerializeField] private GameObject _hotPotatoPrefab;
    private Dictionary<GameObject, Unit> _potatoToUnit = new Dictionary<GameObject, Unit>();
    private Dictionary<Unit, GameObject> _unitToPotato = new Dictionary<Unit, GameObject>();
    public virtual EventType[] ObservedEventTypes => new[] { EventType.UnitDamaged, EventType.UnitDestroyed };

    public void SetHotPotatoPrefab(GameObject prefab)
    {
        _hotPotatoPrefab = prefab;
    }

    protected override void Awake()
    {
        // Check if components are assigned in inspector first
        if (_leaderboard == null)
            _leaderboard = GetComponent<LeaderboardBase>();

        if (_gameOverCondition == null)
            _gameOverCondition = GetComponent<GameOverCondition>();

        // If still null, create default components for this game mode
        if (_leaderboard == null)
            _leaderboard = gameObject.AddComponent<HotPotatoLeaderboard>();

        if (_gameOverCondition == null)
            _gameOverCondition = gameObject.AddComponent<HotPotatoGameOverCondition>();

        if (_leaderboard == null || _gameOverCondition == null)
            Debug.LogError("HotPotatoRules requires Leaderboard and GameOverCondition components!");
    }

    protected override void Start()
    {
        base.Start();

        // Subscribe to damage events for potato transfers
        Observable.AddObserver(this, new EventType[] { EventType.UnitDamaged, EventType.UnitDestroyed });

        // Wait a frame to ensure all units are registered
        StartCoroutine(InitializePotatoes());

        int numberOfPotatoes = GetNumberOfPotatoes();
        Debug.Log($"Hot Potato mode started! {numberOfPotatoes} potato(s) will explode in 60 seconds.");
    }

    private void OnDestroy()
    {
        Observable.RemoveObserver(this, new EventType[] { EventType.UnitDamaged, EventType.UnitDestroyed });
    }

    public void OnEventReceived(EventData data)
    {
        switch (data.Type)
        {
            case EventType.UnitDamaged:
            case EventType.UnitDestroyed:
                HandlePotentialPotatoTransfer(data.SourceUnit, data.TargetUnit);
                break;
        }
    }

    private void HandlePotentialPotatoTransfer(Unit attacker, Unit victim)
    {
        // Check if attacker has a potato and victim doesn't
        if (attacker != null && victim != null &&
            _unitToPotato.ContainsKey(attacker) && !_unitToPotato.ContainsKey(victim))
        {
            TransferPotato(attacker, victim);
        }
    }

    private void TransferPotato(Unit fromUnit, Unit toUnit)
    {
        if (!_unitToPotato.ContainsKey(fromUnit))
        {
            Debug.LogWarning($"Tried to transfer potato from {fromUnit.unitName} but they don't have one!");
            return;
        }

        GameObject potato = _unitToPotato[fromUnit];

        // Notify leaderboard of transfer
        var leaderboard = _leaderboard as HotPotatoLeaderboard;
        if (leaderboard != null)
        {
            leaderboard.OnPotatoTransferredFrom(fromUnit);
            leaderboard.OnPotatoTransferredTo(toUnit);
        }

        // Update tracking dictionaries
        _unitToPotato.Remove(fromUnit);
        _unitToPotato[toUnit] = potato;
        _potatoToUnit[potato] = toUnit;

        // Transfer the potato to the new unit
        potato.transform.SetParent(toUnit.transform);
        potato.transform.localPosition = Vector3.zero;
        potato.transform.localRotation = Quaternion.identity;
        potato.transform.localScale = Vector3.one;

        // Update the potato component
        HotPotato potatoComponent = potato.GetComponent<HotPotato>();
        if (potatoComponent != null)
        {
            potatoComponent.TransferToUnit(toUnit);
        }

        Debug.Log($"Hot Potato transferred from {fromUnit.unitName} to {toUnit.unitName}!");
    }

    private IEnumerator InitializePotatoes()
    {
        yield return null; // Wait one frame for units to register

        if (_hotPotatoPrefab == null)
        {
            Debug.LogError("Hot Potato prefab not set! Cannot start game.");
            yield break;
        }

        CreateHotPotatoes();
    }

    private void CreateHotPotatoes()
    {
        // Get registered units from the game over condition
        var gameOverCondition = _gameOverCondition as HotPotatoGameOverCondition;
        if (gameOverCondition == null)
        {
            Debug.LogError("Cannot create potatoes - wrong game over condition type");
            return;
        }

        var registeredUnits = gameOverCondition.GetRegisteredUnits();
        int numberOfPotatoes = GetNumberOfPotatoes();

        if (registeredUnits.Count == 0)
        {
            Debug.LogError("No units registered! Cannot create potatoes.");
            return;
        }

        if (numberOfPotatoes <= 0)
        {
            Debug.LogError("Invalid number of potatoes calculated!");
            return;
        }

        // Randomly select units to get potatoes
        var shuffledUnits = registeredUnits.OrderBy(x => Random.value).ToList();

        for (int i = 0; i < numberOfPotatoes && i < shuffledUnits.Count; i++)
        {
            Unit targetUnit = shuffledUnits[i];
            GameObject potato = Instantiate(_hotPotatoPrefab, targetUnit.transform);

            // Zero out local transform
            potato.transform.localPosition = Vector3.zero;
            potato.transform.localRotation = Quaternion.identity;
            potato.transform.localScale = Vector3.one;

            // Track the potato
            _potatoToUnit[potato] = targetUnit;
            _unitToPotato[targetUnit] = potato;

            // Calculate staggered explosion time
            float explosionTime = CalculateExplosionTime(i, numberOfPotatoes);

            // Initialize the potato component with custom explosion time
            HotPotato potatoComponent = potato.GetComponent<HotPotato>();
            if (potatoComponent != null)
            {
                potatoComponent.Initialize(targetUnit, this, explosionTime);
            }

            var leaderboard = _leaderboard as HotPotatoLeaderboard;
            if (leaderboard != null)
            {
                leaderboard.OnPotatoInitiallyAssigned(targetUnit);
            }

            Debug.Log($"Hot Potato attached to {targetUnit.unitName} with {explosionTime:F1}s timer");
        }

        // Set total potato count for game over condition
        if (gameOverCondition != null)
        {
            gameOverCondition.SetTotalPotatoes(numberOfPotatoes);
        }
    }

    private float CalculateExplosionTime(int potatoIndex, int totalPotatoes)
    {
        const float baseExplosionTime = 60f;

        if (totalPotatoes == 1)
        {
            return baseExplosionTime;
        }

        // Shortest timer is 2/3 of full time (40 seconds)
        float shortestTime = baseExplosionTime * (2f / 3f);
        float longestTime = baseExplosionTime;

        // Evenly space the timers between shortest and longest
        float timeRange = longestTime - shortestTime;
        float timeStep = timeRange / (totalPotatoes - 1);

        return shortestTime + (potatoIndex * timeStep);
    }

    public void OnPotatoExploded(GameObject potato)
    {
        // Clean up tracking when a potato explodes
        if (_potatoToUnit.ContainsKey(potato))
        {
            Unit unit = _potatoToUnit[potato];
            _potatoToUnit.Remove(potato);

            if (_unitToPotato.ContainsKey(unit))
            {
                _unitToPotato.Remove(unit);
            }
        }
    }

    private int GetNumberOfPotatoes()
    {
        // Number of players minus number of winners
        var gameOverCondition = _gameOverCondition as HotPotatoGameOverCondition;
        if (gameOverCondition != null)
        {
            int totalPlayers = gameOverCondition.GetRegisteredUnits().Count;
            int winners = MenuSettings.Instance.numberOfWinners;
            return Mathf.Max(1, totalPlayers - winners);
        }
        return 1;
    }

    public bool UnitHasPotato(Unit unit)
    {
        return _unitToPotato.ContainsKey(unit);
    }

    public Unit GetPotatoHolder(GameObject potato)
    {
        return _potatoToUnit.ContainsKey(potato) ? _potatoToUnit[potato] : null;
    }

    protected override IEnumerator GameOverSequence()
    {
        Debug.Log("Hot Potato game ended!");
        return base.GameOverSequence();
    }
}