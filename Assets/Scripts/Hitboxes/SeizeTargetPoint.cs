using UnityEngine;

public class SeizeTargetPoint : MonoBehaviour
{
    private SeizeGameOverCondition _gameOverCondition;

    public void Initialize(SeizeGameOverCondition gameOverCondition)
    {
        _gameOverCondition = gameOverCondition;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_gameOverCondition != null)
        {
            Unit unit = other.GetComponent<Unit>();
            if (unit != null)
            {
                _gameOverCondition.CheckCapture(unit);
            }
        }
    }
}