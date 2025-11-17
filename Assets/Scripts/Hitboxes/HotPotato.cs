using UnityEngine;
using TMPro;
using System.Collections;

public class HotPotato : MonoBehaviour
{
    [SerializeField] private TMP_Text _countdownText;
    [SerializeField] private float _explosionTime = 60f;
    [SerializeField] private UnitDeathBehavior _explosionDeathBehaviorPrefab;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Sprite _explosionSprite;

    private Unit _attachedUnit;
    private HotPotatoRules _gameRules;
    private float _timeRemaining;
    private bool _hasExploded = false;

    // Spinning variables
    private float _currentRotationSpeed = 0f;
    private float _targetRotationSpeed = 0f;
    private float _speedChangeTimer = 0f;
    private float _speedChangeCooldown = 2f; // Change speed every 2 seconds

    public Unit AttachedUnit => _attachedUnit;
    public float TimeRemaining => _timeRemaining;
    public bool HasExploded => _hasExploded;

    void Start()
    {
        if (_countdownText == null)
            _countdownText = GetComponentInChildren<TMP_Text>();

        _timeRemaining = _explosionTime;
        UpdateCountdownDisplay();

        // Initialize spinning
        SetNewRotationTarget();
    }

    void Update()
    {
        if (_hasExploded) return;

        // Handle countdown
        _timeRemaining -= Time.deltaTime;

        if (_timeRemaining <= 0)
        {
            Explode();
        } else
        {
            UpdateCountdownDisplay();
        }

        // Handle erratic spinning
        UpdateSpinning();
    }

    private void UpdateSpinning()
    {
        // Update speed change timer
        _speedChangeTimer -= Time.deltaTime;

        if (_speedChangeTimer <= 0f)
        {
            SetNewRotationTarget();
            _speedChangeCooldown = Random.Range(1f, 3f); // Vary the change interval
            _speedChangeTimer = _speedChangeCooldown;
        }

        // Lerp towards target speed
        _currentRotationSpeed = Mathf.Lerp(_currentRotationSpeed, _targetRotationSpeed, Time.deltaTime * 2f);

        // Apply rotation
        if (_spriteRenderer != null)
        {
            _spriteRenderer.transform.Rotate(0, 0, _currentRotationSpeed * Time.deltaTime);
        }
    }

    private void SetNewRotationTarget()
    {
        // Random speed between -720 and 720 degrees per second (can be negative for direction changes)
        _targetRotationSpeed = Random.Range(-720f, 720f);

        // Sometimes make it really slow or really fast for variety
        float intensity = Random.value;
        if (intensity < 0.2f) // 20% chance for very slow
        {
            _targetRotationSpeed = Random.Range(-180f, 180f);
        } else if (intensity > 0.8f) // 20% chance for very fast
        {
            _targetRotationSpeed = Random.Range(-1440f, 1440f);
        }
    }

    public void Initialize(Unit unit, HotPotatoRules gameRules, float explosionTime = 60f)
    {
        _attachedUnit = unit;
        _gameRules = gameRules;
        _explosionTime = explosionTime;
        _timeRemaining = _explosionTime;
    }

    private void UpdateCountdownDisplay()
    {
        if (_countdownText != null)
        {
            int seconds = Mathf.CeilToInt(_timeRemaining);
            _countdownText.text = seconds.ToString();

            // Change color as time runs out
            if (seconds <= 10)
                _countdownText.color = Color.red;
            else if (seconds <= 30)
                _countdownText.color = Color.yellow;
            else
                _countdownText.color = Color.white;
        }
    }

    private void Explode()
    {
        if (_hasExploded) return;

        _hasExploded = true;
        Debug.Log($"Hot Potato exploded on {(_attachedUnit ? _attachedUnit.unitName : "unknown unit")}!");

        // Notify the game rules that this potato exploded
        if (_gameRules != null)
        {
            _gameRules.OnPotatoExploded(gameObject);
        }

        // Notify the game over condition
        var gameOverCondition = FindObjectOfType<HotPotatoGameOverCondition>();
        if (gameOverCondition != null)
        {
            gameOverCondition.OnPotatoExploded();
        }

        if (_attachedUnit != null)
        {
            // IMPORTANT: Notify observers BEFORE changing death behavior
            // This marks the unit as killed by potato explosion (no killer = potato death)
            Observable.NotifyObservers(
                new EventData(EventType.UnitDestroyed, null, _attachedUnit)
            );

            // Replace death behavior
            if (_explosionDeathBehaviorPrefab != null)
            {
                Destroy(_attachedUnit.deathBehavior.gameObject);
                UnitDeathBehavior newDeathBehavior = Instantiate(_explosionDeathBehaviorPrefab, _attachedUnit.transform);
                _attachedUnit.deathBehavior = newDeathBehavior;
            }

            // Make sure unit has at least 1 HP before killing it
            _attachedUnit.SetHP(1);
            _attachedUnit.Damage(1); // This will trigger the death behavior
        }

        // Visual explosion effect
        if (_spriteRenderer != null && _explosionSprite != null)
        {
            _spriteRenderer.sprite = _explosionSprite;
            _spriteRenderer.transform.localScale = transform.localScale * 5;
            // Stop spinning when exploded
            _currentRotationSpeed = 0f;
            _targetRotationSpeed = 0f;
        }

        if (_countdownText != null)
        {
            _countdownText.text = "";
            _countdownText.color = Color.red;
        }

        // Disable any colliders to prevent further interactions
        var colliders = GetComponents<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        // Destroy after a brief delay to show explosion
        Destroy(gameObject, 1f);
    }

    // Add safety check for potato transfer
    public void TransferToUnit(Unit newUnit)
    {
        if (_hasExploded) return;

        _attachedUnit = newUnit;
        Debug.Log($"Hot Potato transferred to {newUnit.unitName}. {_timeRemaining:F1} seconds remaining!");
    }
}