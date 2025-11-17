using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateFistAttack : UnitAIState
{
    static readonly float cooldownTime = 0.7f;
    static readonly float maxAttackAngle = 20f;
    static readonly float attackLength = 1.2f;

    public override IEnumerator Run()
    {
        if (parentAI.target.transform.position.x < parentAI.user.transform.position.x)
        {
            parentAI.user.transform.rotation = Quaternion.Euler(0, 180, 0);
        } else
        {
            parentAI.user.transform.rotation = Quaternion.identity;
        }
        FistView view = (FistView)parentAI.user.spriteManager.weaponView;
        Transform fist;
        GameObject arc;
        Vector3 restingPosition;
        Vector3 restingRotation;
        float idealAngle = Vector2.Angle(Linalg.Vector3ToVector2(parentAI.target.transform.position - parentAI.user.transform.position), Vector2.right);
        idealAngle = System.Math.Min(idealAngle, maxAttackAngle);
        if (parentAI.target.transform.position.y < parentAI.user.transform.position.y)
        {
            fist = view.leftFist.transform;
            arc = view.motionEffect;
            restingPosition = view.restingPosition;
            restingRotation = view.restingRotation;
            idealAngle = idealAngle * -1;
        } else
        {
            fist = view.rightFist.transform;
            arc = view.motionEffect2;
            restingPosition = view.rightRestingPosition;
            restingRotation = view.rightRestingRotation;
        }
        fist.localRotation = Quaternion.Euler(0, 0, idealAngle);
        Vector3 directionVector = Linalg.RotateVector2(Vector3.right, idealAngle);
        arc.SetActive(true);
        GameObject hitbox = view.normalHitboxPrefab;
        hitbox = MonoBehaviour.Instantiate(hitbox, fist);
        view.AttachHitbox(hitbox.GetComponent<AttackHitbox>());
        float attackDuration = hitbox.GetComponent<AttackHitbox>().lifetime;
        float countdown = 0f;
        while (countdown < attackDuration)
        {
            countdown += Time.deltaTime;
            fist.localPosition = restingPosition + directionVector * attackLength * countdown / attackDuration;
            yield return null;
        }
        countdown = 0f;
        fist.localPosition = restingPosition + directionVector * attackLength;
        yield return new WaitForSeconds(cooldownTime / 3);
        arc.SetActive(false);
        float readyTime = cooldownTime * 2 / 3;
        while (countdown < readyTime)
        {
            countdown += Time.deltaTime;
            fist.localPosition = restingPosition + directionVector * attackLength * (readyTime - countdown) / readyTime;
            yield return null;
        }
        fist.localRotation = Quaternion.Euler(restingRotation);
        fist.localPosition = restingPosition;
    }

    public override void ForceExit()
    {
        FistView view = (FistView)parentAI.user.spriteManager.weaponView;
        view.ResetPosition();
        view.motionEffect.SetActive(false);
        view.motionEffect2.SetActive(false);
    }
}
