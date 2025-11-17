using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateAxeSpecial : UnitAIState
{
    static readonly float windupTime = 0.7f;
    static readonly float cooldownTime = 1.5f;
    static readonly float attackAngle = 110f;
    static readonly float maxThrowAngle = 10f;

    public override IEnumerator Run()
    {
        Quaternion initialHandaxeAngle;
        Vector3 targetPosition = parentAI.target.transform.position;
        if (parentAI.target.transform.position.x < parentAI.user.transform.position.x)
        {
            parentAI.user.transform.rotation = Quaternion.Euler(0, 180, 0);
            initialHandaxeAngle = Quaternion.Euler(0, 180, attackAngle);
        } else
        {
            parentAI.user.transform.rotation = Quaternion.identity;
            initialHandaxeAngle = Quaternion.Euler(0, 0, attackAngle);
        }
        WeaponView axe = parentAI.user.spriteManager.weaponView;
        float restingAngle = axe.restingRotation.z;
        float countdown = 0f;
        while (countdown < windupTime)
        {
            float deltaT = Time.deltaTime;
            countdown += deltaT;
            float axeAngle = (attackAngle - restingAngle) * (float)System.Math.Sqrt(countdown / windupTime) + restingAngle;
            axe.transform.localRotation = Quaternion.Euler(0, 0, axeAngle);
            yield return null;
        }
        countdown = 0f;
        axe.transform.localRotation = Quaternion.Euler(0, 0, attackAngle);
        GameObject hitbox = axe.specialHitboxPrefab;
        hitbox = MonoBehaviour.Instantiate(hitbox, parentAI.user.transform.position, initialHandaxeAngle);
        axe.AttachHitbox(hitbox.GetComponent<AttackHitbox>(), true);
        axe.gameObject.SetActive(false);
        HandAxeHitbox handaxe = hitbox.GetComponent<HandAxeHitbox>();
        if (parentAI.target != null)
        {
            targetPosition = parentAI.target.transform.position;
        }
        Vector3 initialVelocity = (targetPosition - parentAI.user.transform.position).normalized;
        float idealAngle = Vector3.Angle(initialVelocity, parentAI.user.transform.right);
        if (idealAngle > maxThrowAngle)
        {
            if ((targetPosition.y - parentAI.user.transform.position.y) * (targetPosition.x - parentAI.user.transform.position.x) >= 0)
            {
                initialVelocity = Linalg.RotateVector2(parentAI.user.transform.right, maxThrowAngle);
            } else
            {
                initialVelocity = Linalg.RotateVector2(parentAI.user.transform.right, maxThrowAngle * -1);
            }
        }
        handaxe.velocityDirection = initialVelocity;
        while (hitbox != null)
        {
            yield return null;
        }
        axe.gameObject.SetActive(true);
        countdown = 0f;
        axe.transform.localRotation = Quaternion.Euler(0, 0, attackAngle * -1);
        while (countdown < cooldownTime)
        {
            float deltaT = Time.deltaTime;
            countdown += deltaT;
            float axeAngle = (restingAngle - attackAngle) * countdown / cooldownTime + attackAngle;
            axe.transform.localRotation = Quaternion.Euler(0, 0, axeAngle);
            yield return null;
        }
        axe.transform.localRotation = Quaternion.Euler(0, 0, restingAngle);
    }

    public override void ForceExit() {
        parentAI.user.spriteManager.weaponView.gameObject.SetActive(true);
        parentAI.user.spriteManager.weaponView.transform.localRotation = Quaternion.Euler(parentAI.user.spriteManager.weaponView.restingRotation);
    }
}
