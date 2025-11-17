using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateShurikenAttack : UnitAIState
{
    static readonly float minAngle = 30;
    static readonly float maxAngle = 60;
    static readonly float cooldownTime = 0.5f;
    static readonly Vector2 recoilForce = new Vector2(-250, 0);

    public override IEnumerator Run()
    {
        Vector2 recoil = recoilForce;
        if (parentAI.target.transform.position.x < parentAI.user.transform.position.x)
        {
            parentAI.user.transform.rotation = Quaternion.Euler(0, 180, 0);
            recoil.x = recoil.x * -1;
        } else
        {
            parentAI.user.transform.rotation = Quaternion.identity;
        }
        WeaponView view = parentAI.user.spriteManager.weaponView;
        GameObject hitbox = view.normalHitboxPrefab;
        hitbox = MonoBehaviour.Instantiate(hitbox, parentAI.user.transform.position, parentAI.user.transform.rotation);
        view.AttachHitbox(hitbox.GetComponent<AttackHitbox>(), true);
        ShurikenProjectile projectile = hitbox.GetComponent<ShurikenProjectile>();
        Vector3 initialVelocity = (parentAI.target.transform.position - parentAI.user.transform.position).normalized;
        float idealAngle = Vector3.Angle(initialVelocity, parentAI.user.transform.right);
        if (idealAngle > maxAngle || idealAngle < minAngle)
        {
            if (idealAngle > maxAngle)
            {
                idealAngle = maxAngle;
            } else
            {
                idealAngle = minAngle;
            }
            if ((parentAI.target.transform.position.y - parentAI.user.transform.position.y) * (parentAI.target.transform.position.x - parentAI.user.transform.position.x) >= 0)
            {
                initialVelocity = Linalg.RotateVector2(parentAI.user.transform.right, idealAngle);
            } else
            {
                initialVelocity = Linalg.RotateVector2(parentAI.user.transform.right, idealAngle * -1);
            }
        }
        projectile.velocityDirection = initialVelocity;
        parentAI.user.velocityManager.rb.velocity = Vector2.zero;
        parentAI.user.velocityManager.rb.AddForce(recoil * parentAI.user.scaleFactor * parentAI.user.scaleFactor);
        yield return new WaitForSeconds(cooldownTime);
    }
}
