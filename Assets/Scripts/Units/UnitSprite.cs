using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UnitSprite : MonoBehaviour
{

    public SpriteRenderer neutralPortrait;
    public SpriteRenderer damagePortrait;
    public SpriteRenderer critPortrait;
    //public SpriteRenderer weaponSprite;
    public SpriteRenderer unitOutline;
    public SpriteRenderer unitBackground;
    public SpriteRenderer healthBar;
    public SpriteRenderer healthBarBackground;
    public SpriteRenderer mountSprite;
    public SpriteRenderer flierSprite;
    public SpriteRenderer corrinHair;
    public SpriteRenderer corrinDetail;
    public Transform unitView;
    public WeaponView weaponView;
    public WeaponFactory weaponFactory;
    public Color hexedHealthBarColor;
    public Color normalHealthBarColor;
    public GameObject frozenEffect;
    private Vector3 baseHealthBarPosition;
    private Vector3 baseHealthBarScale;
    private float portraitCountdown;
    public AudioSource dialogue;
    public AudioSource sfxSource;
    public AudioClip attackAudio;
    public AudioClip damageAudio;
    public AudioClip deadAudio;
    public AudioClip critAudio;
    public AudioClip winAudio;
    public TMP_Text nameText;

    // Start is called before the first frame update
    void Start()
    {
        baseHealthBarPosition = healthBar.GetComponent<FreezeLocalTransform>().GetFrozenPosition();
        baseHealthBarScale = healthBar.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (portraitCountdown > 0)
        {
            portraitCountdown -= Time.deltaTime;
            if (portraitCountdown <= 0)
            {
                damagePortrait.gameObject.SetActive(false);
                critPortrait.gameObject.SetActive(false);
                neutralPortrait.gameObject.SetActive(true);
            }
        }
    }

    public void UpdateHealthBar(int currentHP, int maxHP, bool hexed)
    {
        if (!healthBar.gameObject.activeInHierarchy)
        {
            return;
        }
        float ratio = (float)currentHP / (float)maxHP;
        healthBar.transform.localScale = new Vector3(ratio, baseHealthBarScale.y, baseHealthBarScale.z);
        float xOffset = -1 * baseHealthBarScale.x * (1 - ratio) / 2f;
        Vector3 updatedPosition = new Vector3(baseHealthBarPosition.x + xOffset, baseHealthBarPosition.y, baseHealthBarPosition.z);
        healthBar.GetComponent<FreezeLocalTransform>().SetFrozenPosition(updatedPosition);
        if (hexed)
        {
            ratio = 0.5f;
            healthBar.color = hexedHealthBarColor;
        } else
        {
            ratio = 1f;
            healthBar.color = normalHealthBarColor;
        }
        healthBarBackground.transform.localScale = new Vector3(ratio, baseHealthBarScale.y, baseHealthBarScale.z);
        xOffset = -1 * baseHealthBarScale.x * (1 - ratio) / 2f;
        updatedPosition = new Vector3(baseHealthBarPosition.x + xOffset, baseHealthBarPosition.y, baseHealthBarPosition.z);
        healthBarBackground.GetComponent<FreezeLocalTransform>().SetFrozenPosition(updatedPosition);
    }

    public List<SpriteRenderer> GetAllSprites()
    {
        List<SpriteRenderer> sprites = new List<SpriteRenderer>();
        sprites.Add(neutralPortrait);
        sprites.Add(damagePortrait);
        sprites.Add(critPortrait);
        //sprites.Add(weaponSprite);
        sprites.Add(unitOutline);
        sprites.Add(unitBackground);
        sprites.Add(healthBar);
        sprites.Add(healthBarBackground);
        sprites.Add(mountSprite);
        sprites.Add(flierSprite);
        sprites.Add(corrinHair);
        sprites.Add(corrinDetail);
        foreach (SpriteRenderer sprite in weaponView.GetSprites())
        {
            sprites.Add(sprite);
        }
        return sprites;
    }

    public void UseSpecialPortrait(float duration, bool forceOverride)
    {
        if (!forceOverride && !neutralPortrait.gameObject.activeInHierarchy)
        {
            return;
        }
        portraitCountdown = duration;
        neutralPortrait.gameObject.SetActive(false);
        damagePortrait.gameObject.SetActive(false);
        critPortrait.gameObject.SetActive(true);
    }

    public void UseDamagePortrait(float duration, bool forceOverride)
    {
        if (!forceOverride && !neutralPortrait.gameObject.activeInHierarchy)
        {
            return;
        }
        portraitCountdown = duration;
        neutralPortrait.gameObject.SetActive(false);
        critPortrait.gameObject.SetActive(false);
        damagePortrait.gameObject.SetActive(true);
    }

    public void UseNormalPortrait()
    {
        damagePortrait.gameObject.SetActive(false);
        critPortrait.gameObject.SetActive(false);
        neutralPortrait.gameObject.SetActive(true);
        portraitCountdown = 0;
    }

    public void PlayVoiceClip(VoiceType voiceType)
    {
        AudioClip clip = null;
        int prio = 0;
        if (gameObject.GetComponent<Unit>().IsBoss())
        {
            prio += 1;
            if (voiceType == VoiceType.Dead)
            {
                prio += 1;
            }
        }
        if (AudioChannelManager.Instance.IsAudioSourceOnScreen(dialogue))
        {
            prio += 1;
        }
        switch (voiceType)
        {
            case VoiceType.Attack:
                clip = attackAudio;
                break;
            case VoiceType.Damage:
                clip = damageAudio;
                break;
            case VoiceType.Dead:
                clip = deadAudio;
                break;
            case VoiceType.Crit:
                clip = critAudio;
                break;
            case VoiceType.Win:
                //clip = winAudio;
                break;
            default:
                break;
        }
        if (clip == null) { return; }
        //dialogue.clip = clip;
        AudioChannelManager.Instance.TryPlayAudio(dialogue, clip, AudioChannelManager.AudioType.Voice, prio);
    }

    public void PlaySoundEffect(AudioClip sfx)
    {
        int prio = 0;
        if (gameObject.GetComponent<Unit>().IsBoss())
        {
            prio += 1;
        }
        if (AudioChannelManager.Instance.IsAudioSourceOnScreen(dialogue))
        {
            prio += 1;
        }
        AudioChannelManager.Instance.TryPlayAudio(sfxSource, sfx, AudioChannelManager.AudioType.SoundEffect, prio);
    }
}
