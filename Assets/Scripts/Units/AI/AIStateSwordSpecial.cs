using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateSwordSpecial : UnitAIState
{
    public float rotationTime = 0.3f;
    public float targetDistance = 4.5f;
    public float targetAngle = 10f;
    public float retargetInterval = 0.4f;
    public float initialRetargetInterval = 1.1f;
    float storedTurnSpeed;
    float storedMovementSpeed;
    bool usingStoredSpeed;

    public override IEnumerator Run()
    {
        WeaponView sword = parentAI.user.spriteManager.weaponView;
        GameObject hitbox = sword.specialHitboxPrefab;
        hitbox = MonoBehaviour.Instantiate(hitbox, sword.transform);
        hitbox.transform.localScale = Vector3.one / sword.restingScale.x;
        hitbox.transform.localRotation = Quaternion.Euler(0, 0, 45);
        sword.AttachHitbox(hitbox.GetComponent<AttackHitbox>());
        float attackDuration = hitbox.GetComponent<AttackHitbox>().lifetime;
        float countdown = 0f;
        float retargetCountdown = retargetInterval - initialRetargetInterval;
        UnitVelocityManager vm = parentAI.user.velocityManager;
        storedTurnSpeed = vm.turnSpeed;
        storedMovementSpeed = vm.movementSpeed;
        usingStoredSpeed = true;
        vm.turnSpeed = vm.turnSpeed / 2;
        vm.movementSpeed = vm.movementSpeed * 1.2f;
        Vector2 initialTargetPosition = Linalg.Vector3ToVector2(parentAI.target.transform.position - vm.transform.position);
        initialTargetPosition = Linalg.Vector3ToVector2(vm.transform.position) + Linalg.RotateVector2(initialTargetPosition.normalized, targetAngle * -2) * targetDistance;
        vm.SetTarget(initialTargetPosition);
        //Debug.DrawLine(vm.transform.position, new Vector3(initialTargetPosition.x, initialTargetPosition.y, 0), Color.white, 60f, false);
        parentAI.user.velocityManager.noBrakes = true;
        parentAI.user.spriteManager.UseSpecialPortrait(attackDuration, true);
        sword.motionEffect.SetActive(true);
        while (countdown < attackDuration)
        {
            float deltaT = Time.deltaTime;
            countdown += deltaT;
            retargetCountdown += deltaT;
            float deltaR = 360 * deltaT / rotationTime;
            parentAI.user.spriteManager.unitView.Rotate(new Vector3(0, 0, deltaR));
            if (retargetCountdown >= retargetInterval)
            {
                Vector2 velocityVector = vm.rb.velocity;
                Vector2 targetPosition = Linalg.Vector3ToVector2(vm.transform.position) + Linalg.RotateVector2(velocityVector.normalized, targetAngle) * targetDistance;
                //Debug.DrawLine(vm.transform.position, new Vector3(targetPosition.x, targetPosition.y, 0), Color.white, 60f, false);
                vm.SetTarget(targetPosition);
                retargetCountdown -= retargetInterval;
                //TODO if out of bounds rebound
            }
            yield return null;
        }
        parentAI.user.velocityManager.noBrakes = false;
        yield return new WaitForSeconds(0.1f);
        sword.motionEffect.SetActive(false);
        yield return new WaitForSeconds(0.7f);
        vm.turnSpeed = storedTurnSpeed;
        vm.movementSpeed = storedMovementSpeed;
        vm.ClearTarget();
        parentAI.user.spriteManager.unitView.localRotation = Quaternion.identity;
        usingStoredSpeed = false;
    }

    public override void ForceExit()
    {
        parentAI.user.velocityManager.ClearTarget();
        parentAI.user.velocityManager.noBrakes = false;
        parentAI.user.spriteManager.unitView.localRotation = Quaternion.identity;
        parentAI.user.spriteManager.weaponView.motionEffect.SetActive(false);
        if (usingStoredSpeed)
        {
            parentAI.user.velocityManager.turnSpeed = storedTurnSpeed;
            parentAI.user.velocityManager.movementSpeed = storedMovementSpeed;
            usingStoredSpeed = false;
        }
    }
}
