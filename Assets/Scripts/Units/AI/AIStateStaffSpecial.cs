using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateStaffSpecial : UnitAIState
{
    static readonly float windupTime = 1f;
    static readonly float swingTime = 0.25f;
    static readonly float activeTime = 2f;
    static readonly float cooldownTime = 2f;
    static readonly Vector3 addPosition = new Vector3(0.4f, 0, 0);
    static readonly float windupAngle = 10f;
    static readonly float swingAngle = -90f;

    public override IEnumerator Run()
    {
        WeaponView staff = parentAI.user.spriteManager.weaponView;
        float countdown = 0f;
        Vector3 targetPosition = parentAI.user.transform.position + parentAI.user.transform.right * 10;
        while (countdown < windupTime)
        {
            countdown += Time.deltaTime;
            float angle = staff.restingRotation.z + (windupAngle - staff.restingRotation.z) * countdown / windupTime;
            staff.transform.localRotation = Quaternion.Euler(0, 0, angle);
            yield return null;
        }
        staff.transform.localRotation = Quaternion.Euler(0, 0, windupAngle);
        countdown = 0f;
        while (countdown < swingTime)
        {
            countdown += Time.deltaTime;
            float angle = windupAngle + (swingAngle - windupAngle) * countdown / swingTime;
            staff.transform.localRotation = Quaternion.Euler(0, 0, angle);
            staff.transform.localPosition = staff.restingPosition + addPosition * countdown / swingTime;
            yield return null;
        }
        staff.transform.localRotation = Quaternion.Euler(0, 0, swingAngle);
        staff.transform.localPosition = staff.restingPosition + addPosition;
        parentAI.user.spriteManager.PlaySoundEffect(CharacterAssetLoader.Instance.LoadSoundEffect("Hexing Rod"));
        GameObject hitbox = staff.specialHitboxPrefab;
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
            float angle = swingAngle + (staff.restingRotation.z - swingAngle) * countdown / cooldownTime;
            staff.transform.localRotation = Quaternion.Euler(0, 0, angle);
            staff.transform.localPosition = staff.restingPosition + addPosition * (cooldownTime - countdown) / cooldownTime;
            yield return null;
        }
        staff.transform.localPosition = staff.restingPosition;
    }

    public override void ForceExit()
    {
        parentAI.user.spriteManager.weaponView.motionEffect.SetActive(false);
        parentAI.user.spriteManager.weaponView.transform.localPosition = parentAI.user.spriteManager.weaponView.restingPosition;
        parentAI.user.spriteManager.weaponView.transform.localRotation = Quaternion.Euler(parentAI.user.spriteManager.weaponView.restingRotation);
    }
}
