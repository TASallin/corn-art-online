using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateSeekTargetPoint : UnitAIState
{
    public static readonly float targetRadius = 4f;
    public static readonly float arrivalThreshold = 0.5f;
    public static readonly float maxDuration = 4f; // Between 3-5 seconds
    private float timer;
    private Vector2 targetPoint;
    private Vector2 finalDestination;

    public override IEnumerator Run()
    {
        // Get the seize target point
        targetPoint = GetSeizeTargetPoint();
        if (targetPoint == Vector2.zero)
        {
            Debug.LogWarning("Could not find seize target point, ending seek target state");
            yield break;
        }

        // Turn off brakes for smooth movement
        parentAI.user.velocityManager.noBrakes = true;

        Vector2 currentPosition = Linalg.Vector3ToVector2(parentAI.user.transform.position);
        float distanceToTarget = (targetPoint - currentPosition).magnitude;

        // Determine final destination based on current position
        if (distanceToTarget <= targetRadius)
        {
            // Already inside the circle, move directly to target point
            finalDestination = targetPoint;
        } else
        {
            // Outside the circle, move to edge of circle
            Vector2 directionToTarget = (targetPoint - currentPosition).normalized;
            finalDestination = targetPoint - directionToTarget * targetRadius;
        }

        // Ensure the final destination is in bounds
        GameManager gm = GameManager.GetInstance();
        if (!gm.InBounds(finalDestination))
        {
            // Find nearest in-bounds point
            finalDestination.x = Mathf.Clamp(finalDestination.x, -gm.xBound, gm.xBound);
            finalDestination.y = Mathf.Clamp(finalDestination.y, -gm.yBound, gm.yBound);
        }

        // Set movement target
        parentAI.user.velocityManager.SetTarget(finalDestination);

        // Start timer
        timer = 0f;

        // Wait until we reach destination or timeout
        while (timer < maxDuration)
        {
            timer += Time.deltaTime;

            currentPosition = Linalg.Vector3ToVector2(parentAI.user.transform.position);
            float distanceToDestination = (finalDestination - currentPosition).magnitude;

            // Check if we've reached our destination
            if (distanceToDestination <= arrivalThreshold)
            {
                break;
            }

            yield return null;
        }

        // Clean up
        parentAI.user.velocityManager.ClearTarget();
        yield return null;
    }

    public override void ForceExit()
    {
        // Always turn brakes back on when exiting
        parentAI.user.velocityManager.noBrakes = false;
        parentAI.user.velocityManager.ClearTarget();
    }

    private Vector2 GetSeizeTargetPoint()
    {
        // Try to get the target point from the SeizeGameOverCondition
        var seizeCondition = GameObject.FindObjectOfType<SeizeGameOverCondition>();
        if (seizeCondition != null)
        {
            return seizeCondition.GetTargetPoint();
        }

        // Fallback - use the same logic as StartingPositionsSeize
        GameManager gm = GameManager.GetInstance();
        float xPos = gm.xBound * UnityEngine.Random.Range(0.5f, 1.0f);
        float yPos = UnityEngine.Random.Range(-gm.yBound * 0.8f, gm.yBound * 0.8f);
        return new Vector2(xPos, yPos);
    }
}