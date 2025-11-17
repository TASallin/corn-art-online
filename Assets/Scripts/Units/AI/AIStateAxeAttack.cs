using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateAxeAttack : UnitAIState
{
    static readonly float windupTime = 0.2f;
    static readonly float cooldownTime = 1f;
    static readonly float attackAngle = 60f;

    public override IEnumerator Run()
    {
        if (parentAI.target.transform.position.x < parentAI.user.transform.position.x)
        {
            parentAI.user.transform.rotation = Quaternion.Euler(0, 180, 0);
        } else
        {
            parentAI.user.transform.rotation = Quaternion.identity;
        }
        WeaponView axe = parentAI.user.spriteManager.weaponView;
        float restingAngle = axe.restingRotation.z;
        float countdown = 0f;
        while (countdown < windupTime)
        {
            float deltaT = Time.deltaTime;
            countdown += deltaT;
            float axeAngle = (attackAngle - restingAngle) * countdown / windupTime + restingAngle;
            axe.transform.localRotation = Quaternion.Euler(0, 0, axeAngle);
            yield return null;
        }
        countdown = 0f;
        axe.transform.localRotation = Quaternion.Euler(0, 0, attackAngle);
        GameObject hitbox = axe.normalHitboxPrefab;
        hitbox = MonoBehaviour.Instantiate(hitbox, parentAI.user.spriteManager.transform);
        axe.AttachHitbox(hitbox.GetComponent<AttackHitbox>());
        axe.motionEffect.SetActive(true);
        float attackDuration = hitbox.GetComponent<AttackHitbox>().lifetime;
        while (countdown < attackDuration)
        {
            float deltaT = Time.deltaTime;
            countdown += deltaT;
            float axeAngle = -2 * attackAngle * countdown / attackDuration + attackAngle;
            axe.transform.localRotation = Quaternion.Euler(0, 0, axeAngle);
            yield return null;
        }
        countdown = 0f;
        axe.transform.localRotation = Quaternion.Euler(0, 0, attackAngle * -1);
        yield return new WaitForSeconds(0.1f);
        axe.motionEffect.SetActive(false);
        while (countdown < cooldownTime)
        {
            float deltaT = Time.deltaTime;
            countdown += deltaT;
            float axeAngle = (attackAngle + restingAngle) * countdown / cooldownTime - attackAngle;
            axe.transform.localRotation = Quaternion.Euler(0, 0, axeAngle);
            yield return null;
        }
        axe.transform.localRotation = Quaternion.Euler(0, 0, restingAngle);
    }

    public override void ForceExit()
    {
        parentAI.user.spriteManager.weaponView.motionEffect.SetActive(false);
        parentAI.user.spriteManager.weaponView.transform.localRotation = Quaternion.Euler(parentAI.user.spriteManager.weaponView.restingRotation);
    }
}
