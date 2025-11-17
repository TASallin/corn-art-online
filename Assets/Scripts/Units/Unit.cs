using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{

    public bool aiEnabled;
    public UnitClass unitClass;
    public UnitSprite spriteManager;
    public ArmyManager armyManager;
    public UnitVelocityManager velocityManager;
    public UnitDeathBehavior deathBehavior;
    public int maxHP;
    public int hp;
    public int strength;
    public int magic;
    public int skill;
    public int speed;
    public int luck;
    public int defense;
    public int resistance;
    public UnitAI aiScript;
    public WeaponEnum equippedWeapon;
    public string unitName;
    public string playerName;
    public int teamID; //0 = friendly fire on
    public float frozenTime;
    public bool hexed;
    public UnitObservableBehavior observable;
    public int unitID;
    public float scaleFactor;
    private float boundsCheckTimer = 0f;
    private float boundsCheckInterval = 2f; // Check every 2 seconds
    public bool corrinIsMale;
    public int corrinBodyType;
    public int corrinFace;
    public int corrinHair;
    public int corrinDetail;
    public Color corrinHairColor;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (frozenTime > 0)
        {
            frozenTime -= Time.deltaTime;
            if (frozenTime <= 0)
            {
                Unfreeze();
            }
        }

        // Check if unit is out of bounds
        boundsCheckTimer += Time.deltaTime;
        if (boundsCheckTimer >= boundsCheckInterval)
        {
            boundsCheckTimer = 0f;
            CheckAndFixBounds();
        }
    }

    private void CheckAndFixBounds()
    {
        if (!GetAlive()) return;

        GameManager gm = GameManager.GetInstance();
        if (gm == null) return;

        Vector2 currentPos = transform.position;
        float xBound = gm.xBound;
        float yBound = gm.yBound;
        bool wasOutOfBounds = false;

        // Check if unit is out of bounds
        if (currentPos.x < -xBound || currentPos.x > xBound ||
            currentPos.y < -yBound || currentPos.y > yBound)
        {
            // Calculate new position within bounds
            Vector2 newPos = currentPos;

            // Add some margin to ensure unit is comfortably within bounds
            float margin = 0.5f;

            if (currentPos.x < -xBound)
                newPos.x = -xBound + margin;
            else if (currentPos.x > xBound)
                newPos.x = xBound - margin;

            if (currentPos.y < -yBound)
                newPos.y = -yBound + margin;
            else if (currentPos.y > yBound)
                newPos.y = yBound - margin;

            // Teleport the unit to the new position
            transform.position = newPos;

            // If the unit has a rigidbody, stop its velocity to prevent it from immediately moving out again
            if (velocityManager != null && velocityManager.rb != null)
            {
                velocityManager.rb.velocity = Vector2.zero;
            }

            Debug.Log($"Teleported {unitName} from {currentPos} to {newPos} (out of bounds)");
            wasOutOfBounds = true;
        }
    }

    public bool GetAlive()
    {
        return hp > 0;
    }

    public int GetStrength()
    {
        return strength;
    }

    public int GetMagic()
    {
        return magic;
    }

    public int GetSkill()
    {
        return skill;
    }

    public int GetSpeed()
    {
        return speed;
    }

    public int GetLuck()
    {
        return luck;
    }

    public int GetDefense()
    {
        return defense;
    }

    public int GetResistance()
    {
        return resistance;
    }

    public void SetHP(int newHP)
    {
        hp = newHP;
        spriteManager.UpdateHealthBar(hp, maxHP, hexed);
    }

    public void Damage(int damage)
    {
        int oldHP = hp;
        int newHP = System.Math.Max(0, hp - damage);
        SetHP(newHP);
        if (hp <= 0 && oldHP > 0)
        {
            deathBehavior.OnDeath(this);
            spriteManager.PlayVoiceClip(VoiceType.Dead);
        } else if (hp > 0)
        {
            spriteManager.PlayVoiceClip(VoiceType.Damage);
        }
    }

    public void Heal(int healing)
    {
        int newHP = System.Math.Min(GetMaxHP(), hp + healing);
        SetHP(newHP);
    }

    public void Freeze()
    {
        if (frozenTime > 0)
        {
            return;
        }
        frozenTime = 7f;
        Instantiate(spriteManager.frozenEffect, spriteManager.transform);
        velocityManager.frozen = true;
    }

    public void Unfreeze()
    {
        velocityManager.frozen = false;
    }

    public void Hex()
    {
        if (hexed)
        {
            return;
        }
        hexed = true;
        if (hp > GetMaxHP())
        {
            hp = GetMaxHP();
        }
        spriteManager.UpdateHealthBar(hp, maxHP, hexed);
    }

    public int GetMaxHP()
    {
        if (hexed)
        {
            return maxHP / 2;
        }
        return maxHP;
    }

    public void UpdateMovementParameters()
    {
        if (velocityManager != null)
        {
            velocityManager.UpdateMovementBasedOnClass(this);
        }
        spriteManager.mountSprite.gameObject.SetActive(unitClass.movementType == MovementType.Cavalry);
        spriteManager.flierSprite.gameObject.SetActive(unitClass.movementType == MovementType.Flier);
    }

    public bool IsBoss()
    {
        return scaleFactor >= 1.5f;
    }

    public bool IsElite()
    {
        return scaleFactor > 1f && !IsBoss();
    }

    public void SetPlayerName(string name)
    {
        playerName = name;
        spriteManager.nameText.gameObject.SetActive(true);
        spriteManager.nameText.text = playerName;
    }

    public void ScaleHP(float scaleFactor)
    {
        maxHP = (int)(maxHP * scaleFactor);
        hp = maxHP;
    }
}
