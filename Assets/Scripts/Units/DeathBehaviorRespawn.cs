using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DeathBehaviorRespawn : UnitDeathBehavior
{
    [SerializeField] private SpriteRenderer deadSprite;
    [SerializeField] private TMP_Text timerText; // Add this in inspector
    [SerializeField] private float fadeDelay = 0.5f;
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private float respawnDelay = 20.0f;
    [SerializeField] private float respawnTransparency = 0.3f; // Alpha value when in respawn zone
    [SerializeField] private float respawnRetryDelay = 1.0f; // Delay to add if no spawn position is found
    [SerializeField] private int respawnSearchAttempts = 20; // Number of attempts to find a valid respawn position

    private static List<Unit> respawningUnits = new List<Unit>();
    private Vector3 originalPosition;
    private float timeRemaining;

    public override void OnDeath(Unit unit)
    {
        unit.aiScript.ForceTransition(AIState.Dead);

        if (deadSprite != null)
        {
            deadSprite.gameObject.SetActive(true);
        }

        StartCoroutine(DeathAndRespawnSequence(unit));
    }

    public void SetRespawnDelay(float delay)
    {
        respawnDelay = delay;
    }

    private IEnumerator DeathAndRespawnSequence(Unit unit)
    {
        originalPosition = unit.transform.position;

        // Play death animation
        yield return new WaitForSeconds(fadeDelay);

        List<SpriteRenderer> fadeSprites = unit.spriteManager.GetAllSprites();
        if (deadSprite != null)
        {
            fadeSprites.Add(deadSprite);
        }

        unit.spriteManager.UseDamagePortrait(-1, true);

        // Fade out
        float fadeTimer = 0;
        while (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.deltaTime;
            float alpha = Mathf.Max(0, 1 - fadeTimer / fadeDuration);
            foreach (SpriteRenderer ren in fadeSprites)
            {
                if (ren != null && ren.gameObject.activeInHierarchy)
                {
                    ren.color = new Color(ren.color.r, ren.color.g, ren.color.b, alpha);
                }
            }
            yield return null;
        }

        if (deadSprite != null)
        {
            deadSprite.gameObject.SetActive(false);
        }

        // Move to respawn zone and setup
        respawningUnits.Add(unit);
        CheckAndUpdateRespawnTimers(); // Add this line
        MoveToRespawnZone(unit);
        SetTransparency(unit, respawnTransparency);
        DisablePhysics(unit);

        // Activate and setup timer
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.color = Color.gray;
        }

        // Countdown timer
        timeRemaining = respawnDelay;
        while (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;

            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(timeRemaining);
                timerText.text = seconds.ToString();
            }

            yield return null;
        }

        // Deactivate timer
        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }

        // Try to respawn the unit
        yield return TryRespawnUnit(unit);
    }

    private void DisablePhysics(Unit unit)
    {
        if (unit.velocityManager != null && unit.velocityManager.rb != null)
        {
            unit.velocityManager.rb.bodyType = RigidbodyType2D.Kinematic;
            unit.velocityManager.rb.velocity = Vector2.zero;
        }

        var collider = unit.GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    private void MoveToRespawnZone(Unit unit)
    {
        float xBound = GameManager.GetInstance().xBound;
        float yBound = GameManager.GetInstance().yBound;

        // Position units in the top left corner, in a vertical line if multiple
        float xPosition = -xBound;
        float yPosition = yBound;

        // Offset each unit downward based on its position in the respawning list
        int index = respawningUnits.IndexOf(unit);
        if (index > 0)
        {
            yPosition -= index * 2f; // Space units 2 units apart vertically
        }

        unit.transform.position = new Vector3(xPosition, yPosition, unit.transform.position.z);
    }

    private IEnumerator TryRespawnUnit(Unit unit)
    {
        // Try to find a valid respawn position
        Vector3? respawnPosition = FindValidRespawnPosition(unit);

        // If no position found, increase the respawn timer and try again
        if (!respawnPosition.HasValue)
        {
            Debug.Log($"No valid respawn position found for {unit.unitName}, retrying in {respawnRetryDelay} seconds");
            yield return new WaitForSeconds(respawnRetryDelay);
            yield return TryRespawnUnit(unit);
            yield break;
        }

        // Remove from respawning list and update other units' positions
        int index = respawningUnits.IndexOf(unit);
        respawningUnits.Remove(unit);

        // Shift all units below this one up
        UpdateRespawnZonePositions(index);

        // Proceed with respawning at the found position
        RespawnUnit(unit, respawnPosition.Value);
    }

    private void UpdateRespawnZonePositions(int removedIndex)
    {
        float xBound = GameManager.GetInstance().xBound;
        float yBound = GameManager.GetInstance().yBound;

        // Update positions of all units that were below the removed unit
        for (int i = 0; i < respawningUnits.Count; i++)
        {
            Unit unit = respawningUnits[i];
            float xPosition = -xBound;
            float yPosition = yBound - (i * 2f); // Recalculate position based on new index

            unit.transform.position = new Vector3(xPosition, yPosition, unit.transform.position.z);
        }
    }

    private Vector3? FindValidRespawnPosition(Unit unit)
    {
        float xBound = GameManager.GetInstance().xBound;
        float yBound = GameManager.GetInstance().yBound;

        // Define the respawn area (left 20% of the stage)
        float leftBoundary = -xBound;
        float rightBoundary = -xBound + (xBound * 0.2f);
        float currentRadius = unit.GetComponent<CircleCollider2D>()?.radius ?? 0.5f;

        // First, try the default position
        Vector3 defaultPosition = new Vector3(leftBoundary + 1f, 0, unit.transform.position.z);
        if (IsPositionValid(defaultPosition, currentRadius))
        {
            return defaultPosition;
        }

        // If default position is blocked, try random positions within the respawn area
        for (int attempt = 0; attempt < respawnSearchAttempts; attempt++)
        {
            float randomX = Random.Range(leftBoundary + currentRadius, rightBoundary - currentRadius);
            float randomY = Random.Range(-yBound + currentRadius, yBound - currentRadius);
            Vector3 testPosition = new Vector3(randomX, randomY, unit.transform.position.z);

            if (IsPositionValid(testPosition, currentRadius))
            {
                return testPosition;
            }
        }

        // No valid position found
        return null;
    }

    private bool IsPositionValid(Vector3 position, float radius)
    {
        // Check if position overlaps with any colliders using Physics2D
        LayerMask unitLayerMask = Physics2D.GetLayerCollisionMask(1); // Adjust based on your layer setup
        Collider2D[] overlappingColliders = Physics2D.OverlapCircleAll(position, radius, unitLayerMask);

        // Position is valid if no colliders overlap
        return overlappingColliders.Length == 0;
    }

    private void RespawnUnit(Unit unit, Vector3 position)
    {
        // Restore opacity
        SetTransparency(unit, 1f);

        // Re-enable physics and collision
        if (unit.velocityManager != null && unit.velocityManager.rb != null)
        {
            unit.velocityManager.rb.bodyType = RigidbodyType2D.Dynamic;
        }

        var collider = unit.GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        // Move unit to the respawn position
        unit.transform.position = position;

        // Reset unit health and state
        unit.SetHP(unit.GetMaxHP());
        unit.aiScript.ForceTransition(AIState.Target);

        // Reset sprites to normal state
        unit.spriteManager.UseNormalPortrait();
        ResetSprites(unit);
    }

    private void SetTransparency(Unit unit, float alpha)
    {
        List<SpriteRenderer> sprites = unit.spriteManager.GetAllSprites();
        foreach (SpriteRenderer ren in sprites)
        {
            if (ren != null)
            {
                Color color = ren.color;
                ren.color = new Color(color.r, color.g, color.b, alpha);
            }
        }
    }

    private void ResetSprites(Unit unit)
    {
        List<SpriteRenderer> sprites = unit.spriteManager.GetAllSprites();
        foreach (SpriteRenderer ren in sprites)
        {
            if (ren != null)
            {
                // Reset color to full opacity
                Color color = ren.color;
                ren.color = new Color(color.r, color.g, color.b, 1f);
            }
        }
    }

    public static void ClearRespawnQueue()
    {
        respawningUnits = new List<Unit>();
    }

    private void CheckAndUpdateRespawnTimers()
    {
        // Only proceed if we have enough respawning units
        if (respawningUnits.Count >= MenuSettings.Instance.playerNames.Length && respawningUnits.Count > 0)
        {
            // Get the first unit's remaining time
            Unit firstUnit = respawningUnits[0];
            float firstUnitTimeLeft = respawnDelay;

            // Find the time left on the first unit by checking its timer text
            if (firstUnit.GetComponentInChildren<DeathBehaviorRespawn>() != null)
            {
                firstUnitTimeLeft = firstUnit.GetComponentInChildren<DeathBehaviorRespawn>().timeRemaining;
            }

            // If first unit has more than 3 seconds left
            if (firstUnitTimeLeft > 3)
            {
                float timeReduction = firstUnitTimeLeft - 3;

                // Update all respawning units' timers
                foreach (Unit unit in respawningUnits)
                {
                    var deathBehavior = unit.GetComponentInChildren<DeathBehaviorRespawn>();
                    if (deathBehavior != null)
                    {
                        float currentSeconds = deathBehavior.timeRemaining;
                        float newTime = Mathf.Max(1, currentSeconds - timeReduction);
                        deathBehavior.timeRemaining = newTime;
                        deathBehavior.timerText.text = newTime.ToString();
                    }
                }

                // Find and trigger the StartFanfare animation
                StartFanfare fanfare = FindObjectOfType<StartFanfare>();
                if (fanfare != null)
                {
                    Color orange = new Color(1f, 0.5f, 0f); // Orange color
                    fanfare.PlayTextAnimation("Phoenix Mode", orange, true);
                }
            }
        }
    }
}