using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateShurikenSpecial : UnitAIState
{
    static readonly float minAngle = 15;
    static readonly float deltaTheta = 15;
    static readonly float projectilesPerSide = 5;
    static readonly float windupTime = 0.3f;
    static readonly float cooldownTime = 0.8f;
    static readonly Vector2 recoilForce = new Vector2(-125, 0);

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
        parentAI.user.velocityManager.rb.velocity = Vector2.zero;
        parentAI.user.velocityManager.rb.AddForce(recoil * parentAI.user.scaleFactor * parentAI.user.scaleFactor);
        yield return new WaitForSeconds(windupTime);
        WeaponView view = parentAI.user.spriteManager.weaponView;
        GameObject hitbox;
        ShurikenProjectile projectile;
        float projAngle;
        Vector3 projVelocity;
        for (int i = 0; i < projectilesPerSide; i++)
        {
            hitbox = view.specialHitboxPrefab;
            hitbox = MonoBehaviour.Instantiate(hitbox, parentAI.user.transform.position, parentAI.user.transform.rotation);
            view.AttachHitbox(hitbox.GetComponent<AttackHitbox>(), true);
            projectile = hitbox.GetComponent<ShurikenProjectile>();
            projAngle = minAngle + deltaTheta * i;
            projVelocity = Linalg.RotateVector2(parentAI.user.transform.right, projAngle);
            projectile.velocityDirection = projVelocity;
            hitbox = view.specialHitboxPrefab;
            hitbox = MonoBehaviour.Instantiate(hitbox, parentAI.user.transform.position, parentAI.user.transform.rotation);
            view.AttachHitbox(hitbox.GetComponent<AttackHitbox>(), true);
            projectile = hitbox.GetComponent<ShurikenProjectile>();
            projAngle = minAngle + deltaTheta * i;
            projVelocity = Linalg.RotateVector2(parentAI.user.transform.right, projAngle * -1);
            projectile.velocityDirection = projVelocity;
        }
        parentAI.user.spriteManager.PlaySoundEffect(CharacterAssetLoader.Instance.LoadSoundEffect("Shuriken Special"));
        yield return new WaitForSeconds(cooldownTime);
    }
}
