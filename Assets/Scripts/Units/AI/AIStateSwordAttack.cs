using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateSwordAttack : UnitAIState
{
    

    public override IEnumerator Run()
    {
        WeaponView sword = parentAI.user.spriteManager.weaponView;
        GameObject hitbox = sword.normalHitboxPrefab;
        hitbox = MonoBehaviour.Instantiate(hitbox, sword.transform);
        hitbox.transform.localScale = Vector3.one / sword.restingScale.x;
        hitbox.transform.localRotation = Quaternion.Euler(0, 0, 45);
        sword.AttachHitbox(hitbox.GetComponent<AttackHitbox>());
        sword.motionEffect.SetActive(true);
        float attackDuration = hitbox.GetComponent<AttackHitbox>().lifetime;
        float countdown = 0f;
        while (countdown < attackDuration)
        {
            float deltaT = Time.deltaTime;
            countdown += deltaT;
            float deltaR = 360 * deltaT / attackDuration;
            parentAI.user.spriteManager.transform.Rotate(new Vector3(0, 0, deltaR));
            yield return null;
        }
        yield return new WaitForSeconds(0.05f);
        sword.motionEffect.SetActive(false);
        yield return new WaitForSeconds(0.35f);
    }

    public override void ForceExit()
    {
        parentAI.user.spriteManager.weaponView.motionEffect.SetActive(false);
    }
}
