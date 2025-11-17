using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateSeekFromBelow : UnitAIState
{
    public static readonly float targetRefreshRate = 0.3f;
    public float verticalRange;
    public float horizontalMinRange;
    public float horizontalMaxRange;

    public override IEnumerator Run()
    {
        if (parentAI.target == null)
        {
            parentAI.nextState = AIState.Target;
            yield break;
        }
        float refreshTimer = 0f;
        Vector2 targetPosition = Linalg.Vector3ToVector2(parentAI.target.transform.position);
        Vector2 currentPosition = Linalg.Vector3ToVector2(parentAI.user.transform.position);
        do
        {
            if (parentAI.target == null)
            {
                parentAI.nextState = AIState.Target;
                yield break;
            }
            targetPosition = Linalg.Vector3ToVector2(parentAI.target.transform.position);
            currentPosition = Linalg.Vector3ToVector2(parentAI.user.transform.position);
            refreshTimer -= Time.deltaTime;
            if (refreshTimer <= 0f)
            {
                if (!parentAI.target.GetAlive())
                {
                    parentAI.nextState = AIState.Target;
                    yield break;
                }
                Vector2 currentMovementTarget = parentAI.user.velocityManager.targetPosition;
                if (parentAI.user.velocityManager.asleep || !IsValidTarget(targetPosition, currentMovementTarget))
                {
                    float targetY = currentPosition.y;
                    if (targetY > targetPosition.y || targetPosition.y - targetY > verticalRange)
                    {
                        targetY = targetPosition.y - verticalRange / 10;
                    }
                    float targetX = currentPosition.x;
                    float horizontalDistance = System.Math.Abs(targetPosition.x - targetX);
                    if (horizontalDistance < horizontalMinRange || horizontalDistance > horizontalMaxRange)
                    {
                        if (targetX < targetPosition.x)
                        {
                            targetX = targetPosition.x - (horizontalMinRange + (horizontalMaxRange - horizontalMinRange) / 2);
                        } else
                        {
                            targetX = targetPosition.x + (horizontalMinRange + (horizontalMaxRange - horizontalMinRange) / 2);
                        }
                    }
                    Vector2 movementTarget = new Vector2(targetX, targetY);
                    //Debug.DrawLine(parentAI.user.transform.position, movementTarget, Color.white, 1f);
                    parentAI.user.velocityManager.SetTarget(movementTarget);
                }
                refreshTimer = targetRefreshRate;
            }
            yield return null;
        } while (!IsValidTarget(targetPosition, currentPosition));
        parentAI.user.velocityManager.ClearTarget();
        yield return null;
    }

    public override void ForceExit()
    {
        parentAI.user.velocityManager.ClearTarget();
    }

    public bool IsValidTarget(Vector2 targetPosition, Vector2 currentMovementTarget)
    {
        if (!GameManager.GetInstance().InBounds(currentMovementTarget))
        {
            return false;
        }
        float horizontalDistance = System.Math.Abs(targetPosition.x  - currentMovementTarget.x);
        float verticalDistance = targetPosition.y - currentMovementTarget.y;
        //Debug.Log((targetPosition - currentMovementTarget).magnitude);
        if (verticalDistance < 0 || verticalDistance > verticalRange)
        {
            return false;
        }
        if (horizontalDistance < horizontalMinRange || horizontalDistance > horizontalMaxRange)
        {
            return false;
        }
        return true;
    }
}
