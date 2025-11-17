using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponView : MonoBehaviour
{
    public Unit owner;
    public GameObject normalHitboxPrefab;
    public GameObject specialHitboxPrefab;
    public GameObject motionEffect;
    public float equipTime;
    public float unequipTime;
    public Vector3 restingPosition;
    public Vector3 restingRotation;
    public Vector3 restingScale;

    public void AttachHitbox(AttackHitbox hitbox, bool projectile = false)
    {
        hitbox.owner = owner;
        if (projectile)
        {
            hitbox.transform.localScale = hitbox.transform.localScale * owner.transform.lossyScale.x;
        }
        hitbox.weaponType = owner.equippedWeapon;
    }

    public virtual IEnumerator TakeOut()
    {
        float countdown = 0f;
        float overshootTime = equipTime / 2;
        float overshootAngle = restingRotation.z + 15f;
        while (countdown < overshootTime)
        {
            countdown += Time.deltaTime;
            float multiplier = countdown / overshootTime;
            transform.localScale = restingScale * multiplier;
            transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, overshootAngle * multiplier);
            transform.localPosition = restingPosition * multiplier;
            yield return null;
        }
        transform.localScale = restingScale;
        transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, overshootAngle);
        transform.localPosition = restingPosition;
        countdown -= overshootTime;
        float restingTime = equipTime * 0.3f;
        while (countdown < restingTime)
        {
            countdown += Time.deltaTime;
            float multiplier = (restingTime - countdown) / restingTime;
            transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, restingRotation.z + (overshootAngle - restingRotation.z) * multiplier);
        }
        transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, restingPosition.z);
        yield return new WaitForSeconds(equipTime / 2 - countdown);
    }

    public virtual IEnumerator PutAway()
    {
        float countdown = 0f;
        while (countdown < unequipTime)
        {
            countdown += Time.deltaTime;
            float multiplier = (unequipTime - countdown) / unequipTime;
            transform.localScale = restingScale * multiplier;
            transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, restingRotation.z * multiplier);
            transform.localPosition = restingPosition * multiplier;
            yield return null;
        }
        transform.localScale = restingScale * 0;
        transform.localRotation = Quaternion.Euler(restingRotation.x, restingRotation.y, restingRotation.z * 0);
        transform.localPosition = restingPosition * 0;
    }

    public virtual void ResetPosition()
    {
        transform.localPosition = restingPosition;
        transform.localRotation = Quaternion.Euler(restingRotation);
        transform.localScale = restingScale;
    }

    public virtual List<SpriteRenderer> GetSprites()
    {
        List<SpriteRenderer> sprites = new List<SpriteRenderer>();
        sprites.Add(gameObject.GetComponent<SpriteRenderer>());
        return sprites;
    }
}
