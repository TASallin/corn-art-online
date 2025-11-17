using UnityEngine;
using System.Collections.Generic;

public class HotPotatoGameOverCondition : GameOverCondition
{
    private int _totalPotatoes;
    private int _explodedPotatoes = 0;

    void Start()
    {
        Debug.Log("Hot Potato Game Over Condition initialized");
    }

    protected override void CheckGameOverCondition()
    {
        // Game over when all potatoes have exploded
        if (_totalPotatoes > 0 && _explodedPotatoes >= _totalPotatoes)
        {
            Debug.Log($"All {_totalPotatoes} potatoes have exploded! Game Over!");
            TriggerGameOver();
        }
    }

    public void SetTotalPotatoes(int count)
    {
        _totalPotatoes = count;
        Debug.Log($"Total potatoes set to: {count}");
    }

    public void OnPotatoExploded()
    {
        _explodedPotatoes++;
        Debug.Log($"Potato exploded! {_explodedPotatoes}/{_totalPotatoes} potatoes have exploded");
        CheckGameOverCondition();
    }

    public List<Unit> GetRegisteredUnits()
    {
        return new List<Unit>(_registeredUnits);
    }

    public int GetRemainingPotatoCount()
    {
        return _totalPotatoes - _explodedPotatoes;
    }
}
