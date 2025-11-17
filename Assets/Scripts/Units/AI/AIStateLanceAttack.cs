using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateLanceAttack : UnitAIState
{
    static readonly float windupTime = 0.4f;
    static readonly float cooldownTime = 0.5f;
    static readonly float thrustLength = 1f;
    static readonly float rotateSpeed = 500f;

    public override IEnumerator Run()
    {
        bool flipped;
        if (parentAI.target.transform.position.x < parentAI.user.transform.position.x)
        {
            parentAI.user.transform.rotation = Quaternion.Euler(0, 180, parentAI.user.transform.localRotation.eulerAngles.z);
            flipped = true;
        } else
        {
            parentAI.user.transform.rotation = Quaternion.Euler(0, 0, parentAI.user.transform.localRotation.eulerAngles.z);
            flipped = false;
        }
        WeaponView lance = parentAI.user.spriteManager.weaponView;
        float countdown = 0f;
        while (countdown < windupTime)
        {
            if (parentAI.target == null)
            {
                break;
            }
            float deltaT = Time.deltaTime;
            countdown += deltaT;
            float targetAngle = -1 * Vector2.SignedAngle(Linalg.Vector3ToVector2(parentAI.target.transform.position - parentAI.user.transform.position), Vector2.right);
            float currentAngle = parentAI.user.transform.rotation.eulerAngles.z;
            if (flipped)
            {
                targetAngle = 180 - targetAngle;
            }
            if (targetAngle > 180)
            {
                targetAngle = -360 + targetAngle;
            }
            float maxRotation = rotateSpeed * deltaT;
            if (System.Math.Abs(targetAngle - currentAngle) % 360 > maxRotation)
            {
                if (targetAngle > currentAngle)
                {
                    targetAngle = currentAngle + maxRotation;
                } else
                {
                    targetAngle = currentAngle - maxRotation;
                }
            }
            if (flipped)
            {
                parentAI.user.transform.rotation = Quaternion.AngleAxis(180, Vector3.up) * Quaternion.AngleAxis(targetAngle, Vector3.forward);
            } else
            {
                parentAI.user.transform.rotation = Quaternion.Euler(0, 0, targetAngle);
            }
            yield return null;
        }
        countdown = 0f;
        GameObject hitbox = lance.normalHitboxPrefab;
        hitbox = MonoBehaviour.Instantiate(hitbox, lance.transform);
        hitbox.transform.localScale = Vector3.one / lance.restingScale.x;
        lance.AttachHitbox(hitbox.GetComponent<AttackHitbox>());
        lance.motionEffect.SetActive(true);
        float attackDuration = hitbox.GetComponent<AttackHitbox>().lifetime;
        while (countdown < attackDuration)
        {
            countdown += Time.deltaTime;
            lance.transform.localPosition = lance.restingPosition + Vector3.right * thrustLength * countdown / attackDuration;
            yield return null;
        }
        countdown = 0f;
        lance.transform.localPosition = lance.restingPosition + Vector3.right * thrustLength;
        yield return new WaitForSeconds(cooldownTime / 3);
        lance.motionEffect.SetActive(false);
        float retractTime = cooldownTime * 2 / 3;
        while (countdown < retractTime)
        {
            countdown += Time.deltaTime;
            lance.transform.localPosition = lance.restingPosition + Vector3.right * thrustLength * (retractTime - countdown) / retractTime;
            yield return null;
        }
        lance.transform.localPosition = lance.restingPosition;
    }

    public override void ForceExit()
    {
        parentAI.user.spriteManager.weaponView.motionEffect.SetActive(false);
        parentAI.user.spriteManager.weaponView.transform.localPosition = parentAI.user.spriteManager.weaponView.restingPosition;
    }
}
