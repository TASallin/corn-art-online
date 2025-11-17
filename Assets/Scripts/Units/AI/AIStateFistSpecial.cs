using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateFistSpecial : UnitAIState
{
    static readonly float cooldownTime = 1.4f;
    static readonly float uppercutAngle = 90f;
    static readonly Vector3 uppercutPosition = new Vector3(0.8f, .6f, 0);
    static readonly Vector2 uppercutForce = new Vector2(0, 400);

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
        view.leftFist.SetActive(false);
        view.rightFist.SetActive(false);
        GameObject hitbox = view.specialHitboxPrefab;
        hitbox = MonoBehaviour.Instantiate(hitbox, parentAI.user.transform);
        view.AttachHitbox(hitbox.GetComponent<AttackHitbox>());
        yield return new WaitForSeconds(hitbox.GetComponent<AttackHitbox>().lifetime);
        view.rightFist.transform.localRotation = Quaternion.Euler(0, 0, uppercutAngle);
        view.rightFist.transform.localPosition = uppercutPosition;
        view.rightFist.SetActive(true);
        view.motionEffect2.SetActive(true);
        hitbox = view.uppercutHitboxPrefab;
        hitbox = MonoBehaviour.Instantiate(hitbox, view.rightFist.transform);
        view.AttachHitbox(hitbox.GetComponent<AttackHitbox>());
        parentAI.user.velocityManager.rb.AddForce(uppercutForce * parentAI.user.scaleFactor * parentAI.user.scaleFactor);
        yield return new WaitForSeconds(hitbox.GetComponent<AttackHitbox>().lifetime);
        view.motionEffect2.SetActive(false);
        float countdown = 0f;
        while (countdown < cooldownTime)
        {
            countdown += Time.deltaTime;
            float angle = uppercutAngle + (view.rightRestingRotation.z - uppercutAngle) * countdown / cooldownTime;
            view.rightFist.transform.localRotation = Quaternion.Euler(0, 0, angle);
            view.rightFist.transform.localPosition = uppercutPosition + (view.rightRestingPosition - uppercutPosition) * countdown / cooldownTime;
            yield return null;
        }
        view.leftFist.SetActive(true);
        view.rightFist.transform.localRotation = Quaternion.Euler(view.rightRestingRotation);
        view.rightFist.transform.localPosition = view.rightRestingPosition;
    }

    public override void ForceExit()
    {
        FistView view = (FistView)parentAI.user.spriteManager.weaponView;
        view.ResetPosition();
        view.rightFist.SetActive(true);
        view.leftFist.SetActive(true);
        view.motionEffect2.SetActive(false);
    }
}
