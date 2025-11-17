using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public enum CameraMode
{
    Fixed,
    Dynamic,
    Manual
}

public enum DynamicShotType
{
    Group,     // Shows multiple characters
    Follow,    // Follows a single character
    Pan        // Static position with panning effect
}

public class CameraController : MonoBehaviour
{
    // Previous settings (Manual Mode)
    [Header("Camera Settings")]
    public CameraMode currentMode = CameraMode.Manual;

    [Header("Manual Movement")]
    public float keyboardMoveSpeed = 15f;
    public float dragMoveSpeed = 1.5f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 1.5f;
    public float minZoom = 5f;
    public float maxZoom = 22f;
    public float zoomSmoothness = 8f;

    [Header("Follow Settings")]
    public float followZoom = 8f;
    public float followSmoothness = 8f;
    public LayerMask unitLayer;

    [Header("Boundary Settings")]
    public float boundaryPadding = 6f;

    [Header("Dynamic Camera Settings")]
    public float minShotDuration = 5f;    // Minimum time before changing shots
    public float maxShotDuration = 10f;   // Maximum time before changing shots
    public float transitionSpeed = 2f;    // Speed of transitions between shots
    public float dynamicFollowZoom = 10f; // Zoom level for follow shots
    public float groupShotMinZoom = 12f;  // Minimum zoom for group shots
    public float groupShotMaxZoom = 18f;  // Maximum zoom for group shots
    public float effectIntensity = 0.5f;  // Intensity of panning/zooming (0-1)
    public int characterDetectionInterval = 30; // Frames between character detection updates

    // References and internal variables
    private Camera mainCamera;
    private Vector3 dragOrigin;
    private bool isDragging = false;
    private float targetZoom;

    // Following variables
    private GameObject followTarget;
    private bool isFollowing = false;

    // Dynamic camera variables
    private DynamicShotType currentShotType;
    private Vector3 currentShotTargetPosition;
    private Vector3 previousShotPosition;
    private float currentShotTimer;
    private float currentShotDuration;
    private float effectProgress = 0f;
    private List<GameObject> visibleCharacters = new List<GameObject>();
    private int frameCounter = 0;
    private float effectStartZoom;
    private float effectEndZoom;

    [Header("Critical Event Settings")]
    public float criticalEventZoom = 3f;       // Zoom level for critical events
    public float criticalTransitionSpeed = 3f;   // How fast to move to critical position
    public float criticalEventHoldTime = 3f;     // How long to hold on critical shot
    public float criticalEventPadding = 3f;      // Padding for framing multiple units

    // Critical event variables
    private bool isInCriticalMode = false;
    private Vector3 criticalTargetPosition;
    private List<Unit> criticalUnits = new List<Unit>();
    private float criticalEventTimer = 0f;
    private float previousZoom = -1f; // Initialize to -1 to indicate no previous zoom


    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        targetZoom = mainCamera.orthographicSize;

        if (unitLayer == 0)
        {
            unitLayer = LayerMask.GetMask("Default");
        }

        // Register to receive game events
        // Find EventSystem or GameManager and register
        //var eventSystem = FindObjectOfType<EventSystem>();
        //if (eventSystem != null)
        //{
        //    eventSystem.Register(this, new EventType[] { EventType.UnitDestroyed, EventType.UnitCreated });
        //}
    }
    /*
    void OnDestroy()
    {
        // Clean up event registration
        var eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null)
        {
            eventSystem.Unregister(this);
        }
    }

    public void OnEventReceived(EventData data)
    {
        // Check if this is a boss defeat or other critical event
        if (currentMode == CameraMode.Dynamic && IsCriticalEvent(data))
        {
            InitiateCriticalEventMode(data);
        }
    }
    */

    private bool IsCriticalEvent(EventData data)
    {
        // Check if this is a boss defeat or other critical event
        if (data.Type == EventType.UnitDestroyed && data.TargetUnit != null && data.TargetUnit.IsBoss())
        {
            return true;
        }

        // Add other critical event checks here as needed
        return false;
    }

    private void InitiateCriticalEventMode(EventData data)
    {
        isInCriticalMode = true;
        criticalEventTimer = 0f;

        // Set up units to focus on
        criticalUnits.Clear();

        if (data.SourceUnit != null)
        {
            criticalUnits.Add(data.SourceUnit);
        }

        if (data.TargetUnit != null)
        {
            criticalUnits.Add(data.TargetUnit);
        }

        // Calculate target position and zoom
        CalculateCriticalFraming();

        // Optional: Visual dramatic effect
        StartCoroutine(DramaticZoomEffect());
    }

    private void CalculateCriticalFraming()
    {
        if (criticalUnits.Count == 0)
        {
            // Fallback to current position if no units
            criticalTargetPosition = transform.position;
            targetZoom = criticalEventZoom;
            return;
        }

        if (criticalUnits.Count == 1)
        {
            // Center on single unit
            criticalTargetPosition = criticalUnits[0].transform.position;
            targetZoom = criticalEventZoom;
        } else
        {
            // Frame multiple units
            Vector3 sumPosition = Vector3.zero;
            float maxDistance = 0f;

            foreach (Unit unit in criticalUnits)
            {
                sumPosition += unit.transform.position;
            }

            criticalTargetPosition = sumPosition / criticalUnits.Count;

            // Calculate required zoom to show all units
            foreach (Unit unit in criticalUnits)
            {
                float distance = Vector3.Distance(unit.transform.position, criticalTargetPosition);
                maxDistance = Mathf.Max(maxDistance, distance);
            }

            // Adjust zoom to ensure all units are visible with padding
            float requiredSize = maxDistance + criticalEventPadding;
            targetZoom = Mathf.Max(criticalEventZoom, requiredSize);
        }

        criticalTargetPosition.z = transform.position.z;  // Maintain camera Z position
    }

    private IEnumerator DramaticZoomEffect()
    {
        // Quick zoom out before zooming in on target
        float originalZoom = mainCamera.orthographicSize;
        float dramaticZoomOut = originalZoom * 0.7f;

        // Dramatic zoom out
        float timer = 0;
        while (timer < 0.2f)
        {
            timer += Time.deltaTime;
            mainCamera.orthographicSize = Mathf.Lerp(originalZoom, dramaticZoomOut, timer / 0.2f);
            yield return null;
        }

        // Now transition to target
        targetZoom = criticalEventZoom;
    }

    void Update()
    {
        if (isInCriticalMode)
        {
            UpdateCriticalMode();
            return;  // Don't process other camera modes during critical events
        }

        switch (currentMode)
        {
            case CameraMode.Manual:
                UpdateManualMode();
                break;

            case CameraMode.Dynamic:
                UpdateDynamicMode();
                break;

            case CameraMode.Fixed:
                // Fixed mode doesn't need updates
                break;
        }

        ApplySmoothZoom();

        if (currentMode != CameraMode.Dynamic)
        {
            EnforceBoundaries();
        }
    }

    private void UpdateCriticalMode()
    {
        criticalEventTimer += Time.deltaTime;

        // Smoothly move to critical target
        transform.position = Vector3.Lerp(
            transform.position,
            criticalTargetPosition,
            Time.deltaTime * criticalTransitionSpeed
        );

        // Check if critical event is complete
        if (criticalEventTimer >= criticalEventHoldTime)
        {
            isInCriticalMode = false;

            // Return to normal dynamic mode
            if (currentMode == CameraMode.Dynamic)
            {
                currentShotTimer = maxShotDuration;  // Force a new shot
            }
        }

        // Update target position if units still exist and moved
        if (criticalEventTimer % 0.5f < Time.deltaTime)  // Update position every 0.5s
        {
            CalculateCriticalFraming();
        }
    }

    public void ShowCriticalEvent(Unit sourceUnit, Unit targetUnit, float holdTime = 0)
    {
        if (currentMode != CameraMode.Dynamic)
            return;

        EventData eventData = new EventData(EventType.UnitDestroyed, sourceUnit, targetUnit);
        InitiateCriticalEventMode(eventData);

        if (holdTime > 0)
        {
            criticalEventHoldTime = holdTime;
        }
    }

    void UpdateManualMode()
    {
        // Check for follow activation
        CheckForFollowActivation();

        // Handle following or manual movement
        if (isFollowing)
        {
            HandleFollowing();

            // Cancel following if user tries to move manually
            if (IsUserTryingToMove())
            {
                StopFollowing();
            }
        } else
        {
            HandleKeyboardMovement();
            HandleMouseDrag();
            HandleZoomControls();
        }
    }

    void UpdateDynamicMode()
    {
        // Periodically update character positions (not every frame for performance)
        if (frameCounter <= 0)
        {
            FindVisibleCharacters();
            frameCounter = characterDetectionInterval;
        }
        frameCounter--;

        // Update shot timer
        currentShotTimer += Time.deltaTime;

        // NEW: Check if we need to transition to a new shot, now considering only alive characters
        bool shouldChangeShot = currentShotTimer >= currentShotDuration ||
                               (visibleCharacters.Count == 0 && currentShotTimer >= minShotDuration * 0.5f);

        // NEW: Also check if we're following a dead character
        if (currentShotType == DynamicShotType.Follow && followTarget != null)
        {
            Unit followedUnit = followTarget.GetComponent<Unit>();
            if (followedUnit != null && !followedUnit.GetAlive())
            {
                shouldChangeShot = true;
            }
        }

        if (shouldChangeShot)
        {
            ChangeToNewShot();
        } else
        {
            // Update current shot effects
            UpdateShotEffects();
        }

        // Apply shot transitions
        ApplyShotTransition();

        // Apply dynamic boundary enforcement to keep within game limits
        EnforceDynamicBoundaries();
    }

    void FindVisibleCharacters()
    {
        visibleCharacters.Clear();

        // Find all characters in the scene with UnitSprite component
        UnitSprite[] allUnits = FindObjectsOfType<UnitSprite>();

        foreach (UnitSprite unitSprite in allUnits)
        {
            if (unitSprite != null && unitSprite.gameObject != null)
            {
                // NEW: Check if the unit is alive before adding it
                Unit unit = unitSprite.GetComponent<Unit>();
                if (unit != null && unit.GetAlive())
                {
                    visibleCharacters.Add(unitSprite.gameObject);
                }
            }
        }
    }

    void ChangeToNewShot()
    {
        // Store previous shot position for smooth transitions
        previousShotPosition = transform.position;

        // Set a new shot duration
        currentShotDuration = Random.Range(minShotDuration, maxShotDuration);
        currentShotTimer = 0f;

        // Reset effect progress
        effectProgress = 0f;

        // Choose a new shot type
        ChooseNewShotType();

        // Set up the new shot based on type
        SetupNewShot();
    }

    void ChooseNewShotType()
    {
        // No characters? Default to a group shot of the general area
        if (visibleCharacters.Count == 0)
        {
            currentShotType = DynamicShotType.Pan;
            return;
        }

        // Otherwise, randomly choose a shot type with some weighting
        float rand = Random.value;

        if (rand < 0.5f) // 50% chance for group shots
        {
            currentShotType = DynamicShotType.Group;
        } else if (rand < 0.8f) // 30% chance for follow shots
        {
            currentShotType = DynamicShotType.Follow;
        } else // 20% chance for panning shots
        {
            currentShotType = DynamicShotType.Pan;
        }
    }

    void SetupNewShot()
    {
        switch (currentShotType)
        {
            case DynamicShotType.Group:
                SetupGroupShot();
                break;

            case DynamicShotType.Follow:
                SetupFollowShot();
                break;

            case DynamicShotType.Pan:
                SetupPanShot();
                break;
        }

        // Set up zoom effect for this shot (subtle zoom in or out)
        SetupZoomEffect();
    }

    void SetupGroupShot()
    {
        // NEW: Determine zoom level first with variety
        targetZoom = CalculateVariedZoom(groupShotMinZoom, groupShotMaxZoom);

        if (visibleCharacters.Count == 0)
        {
            // No characters visible, use center of field
            currentShotTargetPosition = new Vector3(0, 0, transform.position.z);
            return;
        }

        // Calculate the visible area based on the chosen zoom level
        float verticalSize = targetZoom;
        float horizontalSize = verticalSize * mainCamera.aspect;

        // Weight positions based on enemy proximity, considering the field of view
        Vector3 center = CalculateOpposingTeamWeightedCenterWithDistance(verticalSize, horizontalSize);

        // Add a smaller random offset, scaled to the zoom level
        float offsetScale = targetZoom / groupShotMaxZoom; // Scale offset based on zoom
        center += new Vector3(
            Random.Range(-5f * offsetScale, 5f * offsetScale),
            Random.Range(-5f * offsetScale, 5f * offsetScale),
            0
        );

        // Keep Z coordinate the same
        center.z = transform.position.z;

        currentShotTargetPosition = center;

        // Store this zoom as the previous for next shot
        previousZoom = targetZoom;
    }

    // Updated method to include distance weighting
    private Vector3 CalculateOpposingTeamWeightedCenterWithDistance(float verticalSize, float horizontalSize)
    {
        if (visibleCharacters.Count == 1)
        {
            return visibleCharacters[0].transform.position;
        }

        // Create a dictionary to store weights for each character
        Dictionary<GameObject, float> characterWeights = new Dictionary<GameObject, float>();

        // Use the field of view to determine interaction radius
        float checkRadius = Mathf.Min(verticalSize, horizontalSize) * 0.8f; // 80% of view size

        // Get current camera position for distance calculations
        Vector3 currentPos = new Vector3(transform.position.x, transform.position.y, 0);

        // Calculate weights for each character based on nearby enemies and distance
        foreach (GameObject character in visibleCharacters)
        {
            Unit characterUnit = character.GetComponent<Unit>();
            if (characterUnit == null) continue;

            float weight = 1.0f; // Base weight
            int nearbyEnemies = 0;

            foreach (GameObject otherCharacter in visibleCharacters)
            {
                if (otherCharacter == character) continue;

                Unit otherUnit = otherCharacter.GetComponent<Unit>();
                if (otherUnit == null) continue;

                // Check if they're enemies and close enough
                if (ArmyManager.IsEnemy(characterUnit, otherUnit))
                {
                    float distance = Vector3.Distance(character.transform.position, otherCharacter.transform.position);
                    if (distance <= checkRadius)
                    {
                        nearbyEnemies++;
                        // Add more weight for closer enemies
                        weight += 2.0f * (1 - (distance / checkRadius));
                    }
                }
            }

            // Dramatically increase weight for areas with multiple enemies
            if (nearbyEnemies >= 2)
            {
                weight *= 1.5f + (nearbyEnemies * 0.5f);
            }

            // NEW: Add weight for distance from current camera position
            float distanceFromCamera = Vector3.Distance(character.transform.position, currentPos);
            float maxReasonableDistance = Mathf.Max(verticalSize, horizontalSize) * 2f;
            float distanceWeight = Mathf.Clamp01(distanceFromCamera / maxReasonableDistance);
            weight *= (1 + distanceWeight * 1.2f); // Up to 120% bonus for distance

            characterWeights[character] = weight;
        }

        // Calculate weighted center
        Vector3 weightedSum = Vector3.zero;
        float totalWeight = 0f;

        foreach (var kvp in characterWeights)
        {
            weightedSum += kvp.Key.transform.position * kvp.Value;
            totalWeight += kvp.Value;
        }

        return totalWeight > 0 ? weightedSum / totalWeight : visibleCharacters[0].transform.position;
    }

    void SetupFollowShot()
    {
        if (visibleCharacters.Count == 0)
        {
            // Fallback to group shot if no characters
            SetupGroupShot();
            return;
        }

        // Select character to follow based on enemy proximity
        GameObject characterToFollow = SelectCharacterToFollow();
        followTarget = characterToFollow;

        // Initial position is at the character's position
        currentShotTargetPosition = new Vector3(
            followTarget.transform.position.x,
            followTarget.transform.position.y,
            transform.position.z
        );

        // NEW: Use varied zoom instead of fixed follow zoom
        float minFollowZoom = dynamicFollowZoom * 0.8f;
        float maxFollowZoom = dynamicFollowZoom * 1.2f;
        targetZoom = CalculateVariedZoom(minFollowZoom, maxFollowZoom);

        // Store this zoom as the previous for next shot
        previousZoom = targetZoom;
    }

    private GameObject SelectCharacterToFollow()
    {
        if (visibleCharacters.Count == 0)
            return null;

        GameObject bestCandidate = null;
        float highestScore = -1f;

        foreach (GameObject character in visibleCharacters)
        {
            Unit characterUnit = character.GetComponent<Unit>();
            if (characterUnit == null || !characterUnit.GetAlive())
                continue;

            // Calculate score based on nearby enemies
            float score = CalculateEnemyProximityScore(characterUnit);

            // Add some randomness to prevent always following the same character
            score *= Random.Range(0.7f, 1.3f);

            if (score > highestScore)
            {
                highestScore = score;
                bestCandidate = character;
            }
        }

        return bestCandidate ?? visibleCharacters[Random.Range(0, visibleCharacters.Count)];
    }

    // NEW METHOD: Calculate score based on enemy proximity
    private float CalculateEnemyProximityScore(Unit characterUnit)
    {
        float score = 0f;

        foreach (GameObject otherCharacter in visibleCharacters)
        {
            Unit otherUnit = otherCharacter.GetComponent<Unit>();
            if (otherUnit == null || otherUnit == characterUnit || !otherUnit.GetAlive())
                continue;

            if (ArmyManager.IsEnemy(characterUnit, otherUnit))
            {
                float distance = Vector3.Distance(characterUnit.transform.position, otherCharacter.transform.position);
                // Score is higher for closer enemies
                if (distance <= 10f)
                {
                    score += (10f - distance) / 10f; // Score between 0 and 1
                }
            }
        }

        return score;
    }

    void SetupPanShot()
    {
        // NEW: Determine zoom level first, with bias away from previous zoom
        targetZoom = CalculateVariedZoom(groupShotMinZoom, groupShotMaxZoom);

        // Calculate the visible area based on the chosen zoom level
        float verticalSize = targetZoom;
        float horizontalSize = verticalSize * mainCamera.aspect;

        // Determine a good search radius based on the field of view
        float searchRadius = Mathf.Max(verticalSize, horizontalSize) * 1.5f;

        // Get current camera position for distance calculations
        Vector3 currentPos = new Vector3(transform.position.x, transform.position.y, 0);

        if (visibleCharacters.Count == 0)
        {
            // No characters, use a position that's at a reasonable distance from current
            currentShotTargetPosition = CalculateDistantPoint(currentPos, searchRadius, true);
        } else
        {
            // Find the best position considering both combat activity and distance from current position
            currentShotTargetPosition = FindOptimalPanPosition(currentPos, verticalSize, horizontalSize, searchRadius);
        }

        // Keep Z coordinate the same
        currentShotTargetPosition.z = transform.position.z;

        // Store this zoom as the previous for next shot
        previousZoom = targetZoom;
    }

    private float CalculateVariedZoom(float minZoom, float maxZoom)
    {
        float zoom;

        if (previousZoom > 0)
        {
            // Create zones of preferred zoom levels to encourage variety
            float zoomRange = maxZoom - minZoom;
            float thirdRange = zoomRange / 3f;

            // Determine which third of the range the previous zoom was in
            if (previousZoom < minZoom + thirdRange)
            {
                // Previous was close, prefer medium to far
                zoom = Random.Range(minZoom + thirdRange, maxZoom);
            } else if (previousZoom > minZoom + (2 * thirdRange))
            {
                // Previous was far, prefer close to medium
                zoom = Random.Range(minZoom, minZoom + (2 * thirdRange));
            } else
            {
                // Previous was medium, prefer close or far
                if (Random.value < 0.5f)
                    zoom = Random.Range(minZoom, minZoom + thirdRange);
                else
                    zoom = Random.Range(minZoom + (2 * thirdRange), maxZoom);
            }
        } else
        {
            // First shot, use normal random
            zoom = Random.Range(minZoom, maxZoom);
        }

        // Add slight variation to prevent exact same zoom levels
        zoom += Random.Range(-0.5f, 0.5f);
        return Mathf.Clamp(zoom, minZoom, maxZoom);
    }

    // NEW METHOD: Find optimal pan position considering combat and distance
    private Vector3 FindOptimalPanPosition(Vector3 currentPos, float verticalSize, float horizontalSize, float searchRadius)
    {
        // Create a list to track positions and their scores
        List<(Vector3 position, float score)> candidatePositions = new List<(Vector3, float)>();

        // Sample several positions around interesting characters
        int sampleCount = Mathf.Min(10, visibleCharacters.Count * 2);

        for (int i = 0; i < sampleCount; i++)
        {
            // Pick a random character as a starting point
            GameObject randomChar = visibleCharacters[Random.Range(0, visibleCharacters.Count)];

            // Generate a position near this character
            Vector2 randomOffset = Random.insideUnitCircle * searchRadius;
            Vector3 candidatePos = randomChar.transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);

            // Calculate score for this position
            float score = ScorePanPosition(candidatePos, currentPos, verticalSize, horizontalSize);

            candidatePositions.Add((candidatePos, score));
        }

        // Also consider some completely random positions for variety
        for (int i = 0; i < 3; i++)
        {
            Vector3 randomPos = CalculateDistantPoint(currentPos, searchRadius * 1.5f, false);
            float score = ScorePanPosition(randomPos, currentPos, verticalSize, horizontalSize);
            candidatePositions.Add((randomPos, score));
        }

        // Sort by score and pick from the top candidates with some randomness
        candidatePositions.Sort((a, b) => b.score.CompareTo(a.score));

        // Pick from top 30% with weighted randomness
        int topCount = Mathf.Max(1, candidatePositions.Count * 30 / 100);
        float[] weights = new float[topCount];
        for (int i = 0; i < topCount; i++)
        {
            weights[i] = 1.0f / (i + 1); // Higher weight for better scores
        }

        int selectedIndex = WeightedRandomChoice(weights);
        return candidatePositions[selectedIndex].position;
    }

    // NEW METHOD: Score a potential pan position
    private float ScorePanPosition(Vector3 position, Vector3 currentPos, float verticalSize, float horizontalSize)
    {
        float score = 0f;

        // 1. Distance from current position (weighted heavily to encourage movement)
        float distance = Vector3.Distance(position, currentPos);
        float maxReasonableDistance = Mathf.Max(verticalSize, horizontalSize) * 2f;
        float distanceScore = Mathf.Clamp01(distance / maxReasonableDistance);
        score += distanceScore * 40f; // Weight: 40% for distance variety

        // 2. Number of characters visible from this position
        int visibleCharCount = CountCharactersInView(position, verticalSize, horizontalSize);
        float characterDensityScore = Mathf.Clamp01(visibleCharCount / 5f);
        score += characterDensityScore * 20f; // Weight: 20% for character density

        // 3. Enemy proximity score
        float enemyProximityScore = CalculateEnemyProximityAtPosition(position, verticalSize, horizontalSize);
        score += enemyProximityScore * 35f; // Weight: 35% for combat interest

        // 4. Avoid going too close to edges
        float boundaryScore = CalculateBoundaryScore(position);
        score += boundaryScore * 5f; // Weight: 5% to avoid extreme edges

        return score;
    }

    // NEW METHOD: Calculate how many characters would be visible from a position
    private int CountCharactersInView(Vector3 position, float verticalSize, float horizontalSize)
    {
        int count = 0;

        foreach (GameObject character in visibleCharacters)
        {
            Vector3 charPos = character.transform.position;

            // Check if character would be within the view bounds
            if (Mathf.Abs(charPos.x - position.x) <= horizontalSize &&
                Mathf.Abs(charPos.y - position.y) <= verticalSize)
            {
                count++;
            }
        }

        return count;
    }

    // NEW METHOD: Calculate enemy proximity score at a specific position
    private float CalculateEnemyProximityAtPosition(Vector3 position, float verticalSize, float horizontalSize)
    {
        float score = 0f;
        float viewRadius = Mathf.Max(verticalSize, horizontalSize);

        // Check each character that would be visible
        foreach (GameObject character in visibleCharacters)
        {
            Unit unit = character.GetComponent<Unit>();
            if (unit == null || !unit.GetAlive()) continue;

            Vector3 charPos = character.transform.position;
            if (Vector3.Distance(charPos, position) > viewRadius) continue;

            // Count nearby enemies for this character
            foreach (GameObject other in visibleCharacters)
            {
                Unit otherUnit = other.GetComponent<Unit>();
                if (otherUnit == null || otherUnit == unit || !otherUnit.GetAlive()) continue;

                if (ArmyManager.IsEnemy(unit, otherUnit))
                {
                    float distance = Vector3.Distance(charPos, other.transform.position);
                    if (distance <= viewRadius * 0.8f)
                    {
                        score += 1.0f / (1 + distance); // Closer enemies score higher
                    }
                }
            }
        }

        return score;
    }

    // NEW METHOD: Calculate boundary score to avoid extreme edges
    private float CalculateBoundaryScore(Vector3 position)
    {
        float xBound = GameManager.GetInstance().xBound;
        float yBound = GameManager.GetInstance().yBound;

        float xScore = 1.0f - Mathf.Clamp01(Mathf.Abs(position.x) / (xBound * 0.8f));
        float yScore = 1.0f - Mathf.Clamp01(Mathf.Abs(position.y) / (yBound * 0.8f));

        return (xScore + yScore) / 2f;
    }

    // NEW METHOD: Calculate a point at a distance from current position
    private Vector3 CalculateDistantPoint(Vector3 currentPos, float radius, bool fullyRandom)
    {
        if (fullyRandom)
        {
            float xBound = GameManager.GetInstance().xBound;
            float yBound = GameManager.GetInstance().yBound;

            return new Vector3(
                Random.Range(-xBound * 0.7f, xBound * 0.7f),
                Random.Range(-yBound * 0.7f, yBound * 0.7f),
                0
            );
        }

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(radius * 0.5f, radius);

        return currentPos + new Vector3(randomDir.x * randomDistance, randomDir.y * randomDistance, 0);
    }

    // NEW METHOD: Weighted random choice from array
    private int WeightedRandomChoice(float[] weights)
    {
        float totalWeight = 0f;
        foreach (float weight in weights)
        {
            totalWeight += weight;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            currentWeight += weights[i];
            if (randomValue <= currentWeight)
            {
                return i;
            }
        }

        return weights.Length - 1; // Fallback
    }

    void SetupZoomEffect()
    {
        // 70% chance to have a zoom effect
        if (Random.value > 0.3f)
        {
            // Store starting zoom
            effectStartZoom = targetZoom;

            // Determine end zoom with a small delta
            float zoomDelta = Random.Range(-3f, 3f) * effectIntensity;
            effectEndZoom = Mathf.Clamp(effectStartZoom + zoomDelta, minZoom, maxZoom);
        } else
        {
            // No zoom effect
            effectStartZoom = targetZoom;
            effectEndZoom = targetZoom;
        }
    }

    void UpdateShotEffects()
    {
        // Update effect progress
        effectProgress = Mathf.Clamp01(currentShotTimer / currentShotDuration);

        switch (currentShotType)
        {
            case DynamicShotType.Follow:
                UpdateFollowShot();
                break;

            case DynamicShotType.Pan:
                UpdatePanShot();
                break;

                // Group shots don't need updates as they're static positions
        }

        // Update zoom effect
        UpdateZoomEffect();
    }

    void UpdateFollowShot()
    {
        // Check if target still exists
        if (followTarget == null)
        {
            // Target lost, prepare to change shot
            currentShotTimer = currentShotDuration;
            return;
        }

        // NEW: Check if target is still alive
        Unit followedUnit = followTarget.GetComponent<Unit>();
        if (followedUnit != null && !followedUnit.GetAlive())
        {
            // Target died, prepare to change shot
            currentShotTimer = currentShotDuration;
            return;
        }

        // Update target position based on followed character
        currentShotTargetPosition = new Vector3(
            followTarget.transform.position.x,
            followTarget.transform.position.y,
            transform.position.z
        );
    }

    void UpdatePanShot()
    {
        // Apply subtle panning motion using sine waves
        float panX = Mathf.Sin(effectProgress * Mathf.PI * 2) * 3f * effectIntensity;
        float panY = Mathf.Cos(effectProgress * Mathf.PI * 1.5f) * 2f * effectIntensity;

        Vector3 panOffset = new Vector3(panX, panY, 0);

        // Add the panning motion to the base position
        currentShotTargetPosition = previousShotPosition + panOffset;
    }

    void UpdateZoomEffect()
    {
        // Interpolate between start and end zoom values
        if (effectStartZoom != effectEndZoom)
        {
            // Use easeInOut curve for more natural effect
            float t = EaseInOutQuad(effectProgress);
            targetZoom = Mathf.Lerp(effectStartZoom, effectEndZoom, t);
        }
    }

    float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }

    void ApplyShotTransition()
    {
        // Smoothly transition to target position
        transform.position = Vector3.Lerp(
            transform.position,
            currentShotTargetPosition,
            Time.deltaTime * transitionSpeed
        );
    }

    void EnforceDynamicBoundaries()
    {
        float xBound = GameManager.GetInstance().xBound;
        float yBound = GameManager.GetInstance().yBound;

        float verticalSize = mainCamera.orthographicSize;
        float horizontalSize = verticalSize * mainCamera.aspect;

        // Allow a bit more padding for dynamic shots
        float dynamicPadding = boundaryPadding * 1.5f;

        float minX = -xBound + horizontalSize - dynamicPadding;
        float maxX = xBound - horizontalSize + dynamicPadding;
        float minY = -yBound + verticalSize - dynamicPadding;
        float maxY = yBound - verticalSize + dynamicPadding;

        if (minX > maxX) { minX = maxX = 0; }
        if (minY > maxY) { minY = maxY = 0; }

        // Clamp the target position, not the actual position
        currentShotTargetPosition.x = Mathf.Clamp(currentShotTargetPosition.x, minX, maxX);
        currentShotTargetPosition.y = Mathf.Clamp(currentShotTargetPosition.y, minY, maxY);
    }

    void CheckForFollowActivation()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            // Get the mouse position in world space
            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            // Find unit at mouse position
            Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, unitLayer);

            if (hit != null)
            {
                UnitSprite unitSprite = hit.GetComponent<UnitSprite>();
                if (unitSprite != null)
                {
                    StartFollowing(hit.gameObject);
                }
            } else
            {
                // If already following and F is pressed with no target, stop following
                if (isFollowing)
                {
                    StopFollowing();
                }
            }
        }
    }

    void StartFollowing(GameObject target)
    {
        followTarget = target;
        isFollowing = true;
        targetZoom = followZoom; // Set zoom to follow level
    }

    void StopFollowing()
    {
        followTarget = null;
        isFollowing = false;
    }

    void HandleFollowing()
    {
        // Check if target still exists
        if (followTarget == null)
        {
            StopFollowing();
            return;
        }

        // Smoothly follow the target
        Vector3 targetPosition = new Vector3(
            followTarget.transform.position.x,
            followTarget.transform.position.y,
            transform.position.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * followSmoothness
        );
    }

    bool IsUserTryingToMove()
    {
        // Check for any manual movement inputs
        bool isKeyboardInput = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                              Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

        bool isMouseDrag = Input.GetMouseButton(0) || Input.GetMouseButtonDown(0);

        bool isZoomInput = Input.GetAxis("Mouse ScrollWheel") != 0 ||
                          Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E);

        return isKeyboardInput || isMouseDrag || isZoomInput;
    }

    void HandleKeyboardMovement()
    {
        float horizontal = 0;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1;
        if (Input.GetKey(KeyCode.D)) horizontal += 1;

        float vertical = 0;
        if (Input.GetKey(KeyCode.S)) vertical -= 1;
        if (Input.GetKey(KeyCode.W)) vertical += 1;

        if (horizontal != 0 || vertical != 0)
        {
            Vector3 movement = new Vector3(horizontal, vertical, 0).normalized;
            float zoomFactor = mainCamera.orthographicSize / 10f;
            transform.position += movement * keyboardMoveSpeed * zoomFactor * Time.deltaTime;
        }
    }

    void HandleMouseDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            isDragging = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 currentPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 difference = dragOrigin - currentPos;

            float zoomFactor = mainCamera.orthographicSize / 10f;
            transform.position += difference * dragMoveSpeed * zoomFactor;

            dragOrigin = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    void HandleZoomControls()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (Input.GetKey(KeyCode.Q))
        {
            scrollInput += 0.05f * Time.deltaTime * zoomSpeed * 10;
        }
        if (Input.GetKey(KeyCode.E))
        {
            scrollInput -= 0.05f * Time.deltaTime * zoomSpeed * 10;
        }

        if (scrollInput != 0)
        {
            targetZoom -= scrollInput * zoomSpeed * 10;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
    }

    void ApplySmoothZoom()
    {
        if (Mathf.Abs(mainCamera.orthographicSize - targetZoom) > 0.01f)
        {
            mainCamera.orthographicSize = Mathf.Lerp(
                mainCamera.orthographicSize,
                targetZoom,
                Time.deltaTime * zoomSmoothness
            );
        } else
        {
            mainCamera.orthographicSize = targetZoom;
        }
    }

    void EnforceBoundaries()
    {
        float xBound = GameManager.GetInstance().xBound;
        float yBound = GameManager.GetInstance().yBound;

        float verticalSize = mainCamera.orthographicSize;
        float horizontalSize = verticalSize * mainCamera.aspect;

        float minX = -xBound + horizontalSize - boundaryPadding;
        float maxX = xBound - horizontalSize + boundaryPadding;
        float minY = -yBound + verticalSize - boundaryPadding;
        float maxY = yBound - verticalSize + boundaryPadding;

        if (minX > maxX) { minX = maxX = 0; }
        if (minY > maxY) { minY = maxY = 0; }

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        transform.position = position;
    }

    public void SetCameraMode(CameraMode mode)
    {
        // Stop following if switching from manual mode
        if (currentMode == CameraMode.Manual && isFollowing)
        {
            StopFollowing();
        }

        // Stop dynamic camera if switching from dynamic
        if (currentMode == CameraMode.Dynamic && mode != CameraMode.Dynamic)
        {
            followTarget = null;
        }

        currentMode = mode;

        switch (mode)
        {
            case CameraMode.Fixed:
                transform.position = new Vector3(0, 0, transform.position.z);
                targetZoom = maxZoom;
                break;

            case CameraMode.Dynamic:
                // Initialize dynamic camera
                currentShotTimer = maxShotDuration; // Force immediate shot change
                frameCounter = 0;
                ChangeToNewShot();
                break;

            case CameraMode.Manual:
                // Keep current position and zoom
                break;
        }
    }

    // Optional: Visual feedback for the followed unit
    void OnGUI()
    {
        if (isFollowing && followTarget != null)
        {
            // Display a small text indicator when following
            Vector3 screenPos = mainCamera.WorldToScreenPoint(followTarget.transform.position);
            GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y - 20, 100, 20), "Following");
        }
    }
}