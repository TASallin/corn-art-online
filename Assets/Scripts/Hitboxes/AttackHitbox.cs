using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{

    public Dictionary<Collider2D, float> iframeDict;
    public float iframeDuration; //-1 = infinite, 0 = none
    public float knockbackMagnitude;
    public float knockbackAngle;
    public float knockbackSpread;
    public float stunDuration;
    public KnockbackType knockbackType;
    public Unit owner;
    public bool destroyOnHit;
    public float lifetime; //-1 = infinite
    protected float countdown;
    public GameObject hitParticles;
    public bool hpDrain;
    public bool freeze;
    public bool hex;
    public WeaponEnum weaponType;
    public AudioClip spawnSfx;
    public AudioClip hitSfx;
    public AudioSource audioSource;
    public GameObject onHitAudioPrefab;

    // Start is called before the first frame update
    void Start()
    {
        iframeDict = new Dictionary<Collider2D, float>();
        if (spawnSfx != null && audioSource != null)
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
            AudioChannelManager.Instance.TryPlayAudio(audioSource, spawnSfx, AudioChannelManager.AudioType.SoundEffect, prio);
        }
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
    }

    public virtual void OnTriggerStay2D(Collider2D other)
    {
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
        force = force * knockbackMagnitude * owner.scaleFactor;
        rb.AddForce(force, ForceMode2D.Force);
        hurtbox.unit.velocityManager.AddStun(stunDuration);
        hurtbox.unit.spriteManager.UseDamagePortrait(stunDuration + 0.2f, false);
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
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }

    public Vector2 GetZeroDegreeVector(Transform target)
    {
        Vector2 zeroVec = new Vector2(1, 0);
        switch (knockbackType)
        {
            case KnockbackType.Fixed:
                zeroVec = Linalg.Vector3ToVector2(transform.right);
                break;
            case KnockbackType.Radial:
                zeroVec = Linalg.Vector3ToVector2(target.position - transform.position);
                break;
            case KnockbackType.XFixed:
                zeroVec = Linalg.Vector3ToVector2(target.position - transform.position);
                if ((zeroVec.x < 0 && transform.right.x > 0) || (zeroVec.x > 0 && transform.right.x < 0))
                {
                    zeroVec = new Vector2(zeroVec.x * -1, zeroVec.y);
                }
                break;
            default:
                break;
        }
        return zeroVec.normalized;
    }

    public virtual void ResolveDamage(Unit target)
    {
        int weaponMight = StatCalcs.DefaultWeaponMight(weaponType);
        int damage = StatCalcs.DamageCalc(owner, target, weaponMight, weaponType);
        bool previouslyAlive = target.GetAlive();
        target.Damage(damage);
        if (previouslyAlive)
        {
            target.observable.ReportDamage(owner, damage);
        }
        if (previouslyAlive && !target.GetAlive())
        {
            target.observable.ReportUnitDestroyed(owner);
        }
        if (hpDrain)
        {
            owner.Heal(damage / 2);
        }
        if (freeze)
        {
            target.Freeze();
        }
        if (hex)
        {
            target.Hex();
        }
    }
}
