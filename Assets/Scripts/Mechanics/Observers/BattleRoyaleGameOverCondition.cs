using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BattleRoyaleGameOverCondition : GameOverCondition
{
    [SerializeField] private int _minUnitsToKeepPlaying = 1;
    private int _requiredSurvivors;

    void Start()
    {
        // Get the number of winners from MenuSettings
        _requiredSurvivors = MenuSettings.Instance.numberOfWinners;

        Debug.Log($"Battle Royale: Playing until {_requiredSurvivors} survivor(s) remain");
    }

    protected override void CheckGameOverCondition()
    {
        // Game over when units remaining equals required survivors
        if (_registeredUnits.Count <= _requiredSurvivors)
        {
            TriggerGameOver();
        }
    }

    // Get remaining units (all are on team 0 in Battle Royale)
    public List<Unit> GetRemainingUnits()
    {
        return new List<Unit>(_registeredUnits);
    }

    public int GetRemainingUnitCount()
    {
        return _registeredUnits.Count;
    }
}