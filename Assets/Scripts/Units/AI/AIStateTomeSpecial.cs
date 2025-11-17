using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateTomeSpecial : UnitAIState
{
    static readonly float activeTime = 2f;
    static readonly float cooldownTime = 0.2f;
    static readonly float rotationSpeed = 300;

    public override IEnumerator Run()
    {
        if (parentAI.target.transform.position.x < parentAI.user.transform.position.x)
        {
            parentAI.user.transform.rotation = Quaternion.Euler(0, 180, 0);
        } else
        {
            parentAI.user.transform.rotation = Quaternion.identity;
        }
        parentAI.user.spriteManager.PlaySoundEffect(CharacterAssetLoader.Instance.LoadSoundEffect("Nosferatu"));
        WeaponView tome = parentAI.user.spriteManager.weaponView;
        float restingAngle = tome.restingRotation.z;
        GameObject hitbox = tome.specialHitboxPrefab;
        hitbox = MonoBehaviour.Instantiate(hitbox, parentAI.target.transform.position, Quaternion.identity);
        hitbox.transform.localScale = Vector3.one * parentAI.user.scaleFactor;
        tome.AttachHitbox(hitbox.GetComponentInChildren<AttackHitbox>(true));
        tome.motionEffect.SetActive(true);
        float countdown = 0f;
        while (countdown < activeTime)
        {
            float deltaT = Time.deltaTime;
            countdown += deltaT;
            tome.transform.Rotate(new Vector3(0, 0, rotationSpeed * deltaT));
            yield return null;
        }
        countdown = 0f;
        float alteredAngle = tome.transform.localRotation.eulerAngles.z;
        tome.motionEffect.SetActive(false);
        while (countdown < cooldownTime)
        {
            float deltaT = Time.deltaTime;
            countdown += deltaT;
            float tomeAngle = (alteredAngle - restingAngle) * (cooldownTime - countdown) / cooldownTime + restingAngle;
            tome.transform.localRotation = Quaternion.Euler(0, 0, tomeAngle);
            yield return null;
        }
        tome.transform.localRotation = Quaternion.Euler(0, 0, restingAngle);
    }

    public override void ForceExit()
    {
        parentAI.user.spriteManager.weaponView.motionEffect.SetActive(false);
        parentAI.user.spriteManager.weaponView.transform.localRotation = Quaternion.Euler(parentAI.user.spriteManager.weaponView.restingRotation);
    }
}
