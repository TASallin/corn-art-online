using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateTomeAttack : UnitAIState
{
    static readonly float windupTime = 0.4f;
    static readonly float cooldownTime = 2f;
    static readonly float attackAngle = 90;

    public override IEnumerator Run()
    {
        if (parentAI.target.transform.position.x < parentAI.user.transform.position.x)
        {
            parentAI.user.transform.rotation = Quaternion.Euler(0, 180, 0);
        } else
        {
            parentAI.user.transform.rotation = Quaternion.identity;
        }
        WeaponView tome = parentAI.user.spriteManager.weaponView;
        float restingAngle = tome.restingRotation.z;
        Vector3 restingPosition = tome.restingPosition;
        float positionMagnitude = restingPosition.magnitude;
        float countdown = 0f;
        while (countdown < windupTime)
        {
            float deltaT = Time.deltaTime;
            countdown += deltaT;
            float tomeAngle = (attackAngle - restingAngle) * countdown / windupTime + restingAngle;
            tome.transform.localRotation = Quaternion.Euler(0, 0, tomeAngle);
            tome.transform.localPosition = Linalg.RotateVector2(parentAI.user.transform.right, tomeAngle) * positionMagnitude;
            yield return null;
        }
        countdown = 0f;
        tome.transform.localRotation = Quaternion.Euler(0, 0, attackAngle);
        tome.transform.localPosition = Linalg.RotateVector2(parentAI.user.transform.right, attackAngle) * positionMagnitude;
        GameObject hitbox = tome.normalHitboxPrefab;
        hitbox = MonoBehaviour.Instantiate(hitbox, tome.transform.position, Quaternion.Euler(0, 0, attackAngle));
        tome.AttachHitbox(hitbox.GetComponent<AttackHitbox>(), true);
        hitbox.gameObject.GetComponent<HomingProjectile>().target = parentAI.target;
        tome.motionEffect.SetActive(true);
        yield return new WaitForSeconds(cooldownTime / 2);
        tome.motionEffect.SetActive(false);
        float retractTime = cooldownTime / 2;
        while (countdown < retractTime)
        {
            float deltaT = Time.deltaTime;
            countdown += deltaT;
            float tomeAngle = (attackAngle - restingAngle) * (cooldownTime - countdown) / cooldownTime + restingAngle;
            tome.transform.localRotation = Quaternion.Euler(0, 0, tomeAngle);
            tome.transform.localPosition = Linalg.RotateVector2(parentAI.user.transform.right, tomeAngle) * positionMagnitude;
            yield return null;
        }
        tome.transform.localRotation = Quaternion.Euler(0, 0, restingAngle);
        tome.transform.localPosition = tome.restingPosition;
    }

    public override void ForceExit()
    {
        parentAI.user.spriteManager.weaponView.motionEffect.SetActive(false);
        parentAI.user.spriteManager.weaponView.transform.localRotation = Quaternion.Euler(parentAI.user.spriteManager.weaponView.restingRotation);
        parentAI.user.spriteManager.weaponView.transform.localPosition = parentAI.user.spriteManager.weaponView.restingPosition;
    }
}
