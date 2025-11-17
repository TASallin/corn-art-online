using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandAxeHitbox : AttackHitbox
{
    public float returnTime;
    public bool returning;
    public float speed;
    public float bounceAngle;
    public float rotateSpeed;
    float lifeCountdown;
    public Vector3 velocityDirection;
    public float returnedDistance;

    // Start is called before the first frame update
    void Start()
    {
        lifeCountdown = 0;
        iframeDict = new Dictionary<Collider2D, float>();
    }

    // Update is called once per frame
    void Update()
    {
        if (iframeDuration >= 0)
        {
            List<Collider2D> toRemove = new List<Collider2D>();
            List<Collider2D> allKeys = new List<Collider2D>();
            foreach (Collider2D col in iframeDict.Keys)
            {
                allKeys.Add(col);
            }
            foreach (Collider2D col in allKeys)
            {
                iframeDict[col] -= Time.deltaTime;
                if (iframeDict[col] <= 0)
                {
                    toRemove.Add(col);
                }
            }
            foreach (Collider2D col in toRemove)
            {
                iframeDict.Remove(col);
            }
        }
        if (lifetime > 0)
        {
            countdown += Time.deltaTime;
            if (countdown >= lifetime)
            {
                Destroy(gameObject);
            }
        }
        lifeCountdown += Time.deltaTime;
        transform.Translate(velocityDirection * Time.deltaTime * speed, Space.World);
        transform.Rotate(new Vector3(0, 0, rotateSpeed * Time.deltaTime));
        if (returning)
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }
            velocityDirection = (owner.transform.position - transform.position).normalized;
            if ((owner.transform.position - transform.position).magnitude <= returnedDistance)
            {
                Destroy(gameObject);
            }
        }
    }

    public override void OnTriggerStay2D(Collider2D other)
    {
        WallHitbox wallhit = other.gameObject.GetComponent<WallHitbox>();
        if (wallhit != null)
        {
            //TODO round wall logic
            if (returning)
            {
                return;
            }
            if (lifeCountdown >= returnTime)
            {
                returning = true;
                if (owner == null)
                {
                    Destroy(gameObject);
                    return;
                }
                velocityDirection = (owner.transform.position - transform.position).normalized;
            } else
            {
                if ((velocityDirection.x < 0 && wallhit.repelDirection.x > 0) || (velocityDirection.x > 0 && wallhit.repelDirection.x < 0))
                {
                    velocityDirection.x = velocityDirection.x * -1;
                }
                if ((velocityDirection.y < 0 && wallhit.repelDirection.y > 0) || (velocityDirection.y > 0 && wallhit.repelDirection.y < 0))
                {
                    velocityDirection.y = velocityDirection.y * -1;
                }
            }
            return;
        }
        UnitHurtbox hurtbox = other.gameObject.GetComponent<UnitHurtbox>();
        if (hurtbox == null || owner == null)
        {
            return;
        }
        //TODO add weapon triangle based attack breaking
        if (iframeDict.ContainsKey(other))
        {
            return;
        }
        if (!ArmyManager.IsEnemy(owner, hurtbox.unit))
        {
            return;
        }
        System.Random rng = GameManager.GetInstance().rng;
        iframeDict.Add(other, iframeDuration);
        ResolveDamage(hurtbox.unit);
        Rigidbody2D rb = other.gameObject.GetComponent<Rigidbody2D>();
        Vector2 force = GetZeroDegreeVector(other.transform);
        float calculatedAngle = knockbackAngle + (float)rng.NextDouble() * knockbackSpread * 2 - knockbackSpread;
        force = Linalg.RotateVector2(force, calculatedAngle);
        force = force * knockbackMagnitude;
        rb.AddForce(force, ForceMode2D.Force);
        hurtbox.unit.velocityManager.AddStun(stunDuration);
        hurtbox.unit.spriteManager.UseDamagePortrait(stunDuration, false);
        if (hitParticles != null)
        {
            Vector3 particlePosition = other.transform.position;
            Vector2 randomizedNoise = new Vector2(1, 0) * (float)rng.NextDouble() * 0.5f * other.transform.lossyScale.x;
            randomizedNoise = Linalg.RotateVector2(randomizedNoise, (float)rng.NextDouble() * 360);
            particlePosition = particlePosition + Linalg.Vector2ToVector3(randomizedNoise);
            GameObject particles = Instantiate(hitParticles, particlePosition, Quaternion.identity);
            particles.transform.Rotate(new Vector3(0, 0, Vector2.SignedAngle(force, new Vector2(1, 0))));
            particles.transform.localScale = owner.transform.lossyScale;
        }
        if (hitSfx != null && onHitAudioPrefab != null)
        {
            int prio = 0;
            if (owner.IsBoss())
            {
                prio += 1;
            }
            if (AudioChannelManager.Instance.IsAudioSourceOnScreen(audioSource))
            {
                prio += 1;
            }
            GameObject audioObject = Instantiate(onHitAudioPrefab, transform.position, Quaternion.identity);
            AudioChannelManager.Instance.TryPlayAudio(audioObject.GetComponent<AudioSource>(), hitSfx, AudioChannelManager.AudioType.SoundEffect, prio);
        }
        if (returning)
        {
            return;
        }
        if (lifeCountdown >= returnTime)
        {
            returning = true;
            velocityDirection = (owner.transform.position - transform.position).normalized;
        } else
        {
            velocityDirection = Linalg.RotateVector2(force.normalized, bounceAngle);
        }
    }
}
