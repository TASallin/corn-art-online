using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateStaffAttack : UnitAIState
{
    static readonly float windupTime = 0.4f;
    static readonly float activeTime = 2f;
    static readonly float cooldownTime = 1.2f;
    static readonly Vector3 addPosition = new Vector3(0.1f, 0.2f, 0);

    public override IEnumerator Run()
    {
        WeaponView staff = parentAI.user.spriteManager.weaponView;
        float countdown = 0f;
        Vector3 targetPosition = parentAI.user.transform.position + parentAI.user.transform.right * 10;
        while (countdown < windupTime)
        {
            countdown += Time.deltaTime;
            staff.transform.localPosition = staff.restingPosition + addPosition * countdown / windupTime;
            yield return null;
        }
        staff.transform.localPosition = staff.restingPosition + addPosition;
        countdown = 0f;
        GameObject hitbox = staff.normalHitboxPrefab;
        if (parentAI.target != null)
        {
            targetPosition = parentAI.target.transform.position;
        }
        hitbox = MonoBehaviour.Instantiate(hitbox, targetPosition, Quaternion.identity);
        hitbox.transform.localScale = Vector3.one * parentAI.user.scaleFactor;
        staff.AttachHitbox(hitbox.GetComponentInChildren<AttackHitbox>(true));
        staff.motionEffect.SetActive(true);
        yield return new WaitForSeconds(activeTime);
        staff.motionEffect.SetActive(false);
        while (countdown < cooldownTime)
        {
            countdown += Time.deltaTime;
            staff.transform.localPosition = staff.restingPosition + addPosition * (cooldownTime - countdown) / cooldownTime;
            yield return null;
        }
        staff.transform.localPosition = staff.restingPosition;
    }

    public override void ForceExit()
    {
        parentAI.user.spriteManager.weaponView.motionEffect.SetActive(false);
        parentAI.user.spriteManager.weaponView.transform.localPosition = parentAI.user.spriteManager.weaponView.restingPosition;
    }
}
