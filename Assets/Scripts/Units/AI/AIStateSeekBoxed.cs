using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateSeekBoxed : UnitAIState
{
    public static readonly float targetRefreshRate = 0.3f;
    public float verticalMinRange;
    public float verticalMaxRange;
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
        GameManager gm = GameManager.GetInstance();
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
                    float verticalDistance = System.Math.Abs(targetPosition.y - targetY);
                    if (verticalDistance < verticalMinRange || verticalDistance > verticalMaxRange)
                    {
                        if (targetY > targetPosition.y)
                        {
                            targetY = targetPosition.y - (verticalMinRange + (verticalMaxRange - verticalMinRange) / 2);
                        } else
                        {
                            targetY = targetPosition.y + (verticalMinRange + (verticalMaxRange - verticalMinRange) / 2);
                        }
                    }
                    float targetX = currentPosition.x;
                    float horizontalDistance = System.Math.Abs(targetPosition.x - targetX);
                    if (horizontalDistance < horizontalMinRange || horizontalDistance > horizontalMaxRange)
                    {
                        if (targetX > targetPosition.x)
                        {
                            targetX = targetPosition.x - (horizontalMinRange + (horizontalMaxRange - horizontalMinRange) / 2);
                        } else
                        {
                            targetX = targetPosition.x + (horizontalMinRange + (horizontalMaxRange - horizontalMinRange) / 2);
                        }
                    }
                    Vector2 movementTarget = new Vector2(targetX, targetY);
                    if (!gm.InBounds(movementTarget))
                    {
                        if (System.Math.Abs(movementTarget.x) > gm.xBound)
                        {
                            movementTarget.x = targetPosition.x + (movementTarget.x - targetPosition.x) * -1;
                        }
                        if (System.Math.Abs(movementTarget.y) > gm.yBound)
                        {
                            movementTarget.y = targetPosition.y + (movementTarget.y - targetPosition.y) * -1;
                        }
                    }
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
        float horizontalDistance = System.Math.Abs(targetPosition.x - currentMovementTarget.x);
        float verticalDistance = System.Math.Abs(targetPosition.y - currentMovementTarget.y);
        //Debug.Log((targetPosition - currentMovementTarget).magnitude);
        if (verticalDistance < verticalMinRange || verticalDistance > verticalMaxRange)
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
