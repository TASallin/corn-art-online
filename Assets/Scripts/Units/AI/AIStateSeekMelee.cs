using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateSeekMelee : UnitAIState
{
    public static readonly float targetRefreshRate = 0.3f;
    public float attackRadius = 2.0f;
    public float attackAngle = 90;
    public float minAttackAngle = 0;

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
                    Vector2 movementTarget = targetPosition + (currentPosition - targetPosition).normalized * (attackRadius / 2);
                    float targetAngle = System.Math.Min(Vector2.Angle(movementTarget - targetPosition, Vector2.right), Vector2.Angle(movementTarget - targetPosition, Vector2.left));
                    if (targetAngle > attackAngle || targetAngle < minAttackAngle)
                    {
                        Vector2 adjustmentVector = Vector2.right;
                        if (parentAI.target.transform.position.y < parentAI.user.transform.position.y)
                        {
                            adjustmentVector = Linalg.RotateVector2(adjustmentVector, attackAngle * 3 / 4);
                        } else
                        {
                            adjustmentVector = Linalg.RotateVector2(adjustmentVector, attackAngle * -3 / 4);
                        }
                        if (currentPosition.x < targetPosition.x)
                        {
                            adjustmentVector.x = adjustmentVector.x * -1;
                        }
                        if (!gm.InBounds(targetPosition + adjustmentVector * (attackRadius / 2)))
                        {
                            Vector3 outOfBoundsTarget = targetPosition + adjustmentVector * (attackRadius / 2);
                            if (System.Math.Abs(outOfBoundsTarget.x) > gm.xBound)
                            {
                                adjustmentVector.x = adjustmentVector.x * -1;
                            }
                            if (System.Math.Abs(outOfBoundsTarget.y) > gm.yBound)
                            {
                                adjustmentVector.y = adjustmentVector.y * -1;
                            }
                        }
                        movementTarget = targetPosition + adjustmentVector * (attackRadius / 2);
                    }
                    parentAI.user.velocityManager.SetTarget(movementTarget);
                    //Debug.DrawLine(parentAI.user.transform.position, movementTarget, Color.white, 1f);
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

        float scaledAttackRadius = attackRadius;
        // Check if this weapon type should scale with unit size
        var rangeData = AIStateFactory.GetNormalWeaponRanges(parentAI.user.equippedWeapon);
        if (rangeData.scalesWithUnit)
        {
            scaledAttackRadius *= parentAI.user.scaleFactor;
        }

        if ((targetPosition - currentMovementTarget).magnitude > scaledAttackRadius)
        {
            return false;
        }

        float targetAngle = System.Math.Min(Vector2.Angle(currentMovementTarget - targetPosition, Vector2.right),
                                          Vector2.Angle(currentMovementTarget - targetPosition, Vector2.left));

        return targetAngle <= attackAngle && targetAngle >= minAttackAngle;
    }
}
