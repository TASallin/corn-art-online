using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateLanceSpecial : UnitAIState
{
    static readonly float airTime = 0.4f;
    static readonly float cooldownTime = 1.2f;
    static readonly Vector3 risingLancePosition = new Vector3(0.5f, 0, 0);
    static readonly float risingLanceRotation = -10;
    static readonly float fallingLanceRotation = -90;
    static readonly Vector2 risingVelocity = new Vector2(0, 20f);
    static readonly float risingAcceleration = 80f;
    static readonly float airHorizontalVelocity = 8f;
    static readonly float airVerticalVelocity = -20f;
    static readonly float airAcceleration = 80f;
    static readonly Vector2 fallingVelocity = new Vector2(0, -40f);
    static readonly float fallingAcceleration = 120f;

    public override IEnumerator Run()
    {
        if (parentAI.target.transform.position.x < parentAI.user.transform.position.x)
        {
            parentAI.user.transform.rotation = Quaternion.Euler(0, 180, 0);
        } else
        {
            parentAI.user.transform.rotation = Quaternion.identity;
        }
        LanceView lance = (LanceView)parentAI.user.spriteManager.weaponView;
        Rigidbody2D rb = parentAI.user.velocityManager.rb;
        lance.transform.localPosition = risingLancePosition;
        lance.transform.localRotation = Quaternion.Euler(0, 0, risingLanceRotation);
        GameObject hitbox = lance.specialHitboxPrefab;
        hitbox = MonoBehaviour.Instantiate(hitbox, lance.transform);
        hitbox.transform.localScale = Vector3.one / lance.restingScale.x;
        lance.AttachHitbox(hitbox.GetComponent<AttackHitbox>());
        lance.risingMotionEffect.SetActive(true);
        float attackDuration = hitbox.GetComponent<AttackHitbox>().lifetime;
        float countdown = 0f;
        while (countdown < attackDuration)
        {
            float deltaT = Time.deltaTime;
            countdown += deltaT;
            Vector2 currentVelocity = rb.velocity;
            float maxDeltaV = risingAcceleration * deltaT;
            if ((risingVelocity - currentVelocity).magnitude <= maxDeltaV)
            {
                rb.velocity = risingVelocity;
            } else
            {
                rb.velocity = currentVelocity + (risingVelocity - currentVelocity).normalized * maxDeltaV;
            }
            yield return null;
        }
        countdown = 0f;
        lance.risingMotionEffect.SetActive(false);
        while (countdown < airTime)
        {
            float deltaT = Time.deltaTime;
            countdown += deltaT;
            Vector2 currentVelocity = rb.velocity;
            Vector2 airVelocity = Linalg.Vector3ToVector2(parentAI.user.transform.right) * airHorizontalVelocity + new Vector2(0, airVerticalVelocity);
            float maxDeltaV = airAcceleration * deltaT;
            if ((airVelocity - currentVelocity).magnitude <= maxDeltaV)
            {
                rb.velocity = airVelocity;
            } else
            {
                rb.velocity = currentVelocity + (airVelocity - currentVelocity).normalized * maxDeltaV;
            }
            float lanceAngle = risingLanceRotation + (fallingLanceRotation - risingLanceRotation) * countdown / airTime;
            lance.transform.localRotation = Quaternion.Euler(0, 0, lanceAngle);
            yield return null;
        }
        countdown = 0f;
        lance.transform.localRotation = Quaternion.Euler(0, 0, fallingLanceRotation);
        hitbox = lance.specialHitbox2Prefab;
        hitbox = MonoBehaviour.Instantiate(hitbox, lance.transform);
        hitbox.transform.localScale = Vector3.one / lance.restingScale.x;
        lance.AttachHitbox(hitbox.GetComponent<AttackHitbox>());
        lance.motionEffect.SetActive(true);
        attackDuration = hitbox.GetComponent<AttackHitbox>().lifetime;
        while (countdown < attackDuration)
        {
            float deltaT = Time.deltaTime;
            countdown += deltaT;
            Vector2 currentVelocity = rb.velocity;
            float maxDeltaV = fallingAcceleration * deltaT;
            if ((fallingVelocity - currentVelocity).magnitude <= maxDeltaV)
            {
                rb.velocity = fallingVelocity;
            } else
            {
                rb.velocity = currentVelocity + (fallingVelocity - currentVelocity).normalized * maxDeltaV;
            }
            yield return null;
        }
        countdown = 0f;
        hitbox = lance.specialHitbox3Prefab;
        hitbox = MonoBehaviour.Instantiate(hitbox, parentAI.user.transform);
        lance.AttachHitbox(hitbox.GetComponent<AttackHitbox>());
        GameObject particles = MonoBehaviour.Instantiate(lance.explosionParticles, parentAI.user.transform.position, Quaternion.identity);
        particles.transform.localScale = Vector3.one * parentAI.user.scaleFactor;
        lance.motionEffect.SetActive(false);
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(cooldownTime / 3);
        float retractTime = cooldownTime * 2 / 3;
        while (countdown < retractTime)
        {
            countdown += Time.deltaTime;
            lance.transform.localPosition = lance.restingPosition + (risingLancePosition - lance.restingPosition) * (retractTime - countdown) / retractTime;
            float lanceAngle = fallingLanceRotation * (retractTime - countdown) / retractTime;
            lance.transform.localRotation = Quaternion.Euler(0, 0, lanceAngle);
            yield return null;
        }
        lance.transform.localPosition = lance.restingPosition;
        lance.transform.localRotation = Quaternion.Euler(lance.restingRotation);
    }

    public override void ForceExit()
    {
        LanceView lance = (LanceView)parentAI.user.spriteManager.weaponView;
        lance.motionEffect.SetActive(false);
        lance.risingMotionEffect.SetActive(false);
        lance.transform.localPosition = lance.restingPosition;
        lance.transform.localRotation = Quaternion.Euler(lance.restingRotation);
    }
}
