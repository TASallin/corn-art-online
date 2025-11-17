using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatCalcs : MonoBehaviour
{
    public static int DamageCalc(Unit attacker, Unit target, int weaponMight, WeaponEnum attackerWeaponType)
    {
        WeaponEnum targetWeaponType = target.equippedWeapon;
        int offensiveStat;
        int defensiveStat;
        if (IsMagicalAttack(attackerWeaponType))
        {
            offensiveStat = attacker.GetMagic();
            defensiveStat = target.GetResistance();
        } else
        {
            offensiveStat = attacker.GetStrength();
            defensiveStat = target.GetDefense();
        }
        int effectiveWeaponMight = weaponMight;
        if (attackerWeaponType == WeaponEnum.Bow && target.unitClass.movementType == MovementType.Flier)
        {
            effectiveWeaponMight = weaponMight * 3;
        }
        int weaponTriangleBonus = WeaponTriangleDamageBonus(attackerWeaponType, targetWeaponType);
        int damage = offensiveStat + effectiveWeaponMight + weaponTriangleBonus - defensiveStat;
        if (attackerWeaponType == WeaponEnum.Staff)
        {
            damage = damage / 3;
        }
        damage = (int)Mathf.Round(damage * attacker.scaleFactor);
        damage = System.Math.Max(damage, 1);
        return damage;
    }

    public static bool IsMagicalAttack(WeaponEnum weaponType)
    {
        return (weaponType == WeaponEnum.Tome || weaponType == WeaponEnum.Staff);
    }

    public static bool IsWeaponTriangleAdvantage(WeaponEnum attackerWeapon, WeaponEnum targetWeapon)
    {
        bool attackerRed = attackerWeapon == WeaponEnum.Sword || attackerWeapon == WeaponEnum.Tome;
        bool attackerGreen = attackerWeapon == WeaponEnum.Axe || attackerWeapon == WeaponEnum.Bow;
        bool attackerBlue = attackerWeapon == WeaponEnum.Lance || attackerWeapon == WeaponEnum.Shuriken;
        bool targetRed = targetWeapon == WeaponEnum.Sword || targetWeapon == WeaponEnum.Tome;
        bool targetGreen = targetWeapon == WeaponEnum.Axe || targetWeapon == WeaponEnum.Bow;
        bool targetBlue = targetWeapon == WeaponEnum.Lance || targetWeapon == WeaponEnum.Shuriken;
        return (attackerRed && targetGreen) || (attackerGreen && targetBlue) || (attackerBlue && targetRed);
    }

    public static bool IsWeaponTriangleDisadvantage(WeaponEnum attackerWeapon, WeaponEnum targetWeapon)
    {
        return IsWeaponTriangleAdvantage(targetWeapon, attackerWeapon);
    }

    public static bool IsRangedWeapon(WeaponEnum weapon)
    {
        return weapon == WeaponEnum.Tome || weapon == WeaponEnum.Bow || weapon == WeaponEnum.Shuriken || weapon == WeaponEnum.Staff;
    }

    //5 damage WTA bonus for now
    public static int WeaponTriangleDamageBonus(WeaponEnum attackerWeapon, WeaponEnum targetWeapon)
    {
        if (IsWeaponTriangleAdvantage(attackerWeapon, targetWeapon))
        {
            return 5;
        } else if (IsWeaponTriangleDisadvantage(attackerWeapon, targetWeapon))
        {
            return -5;
        } else
        {
            return 0;
        }
    }

    public static int DefaultWeaponMight(WeaponEnum weaponType)
    {
        switch (weaponType)
        {
            case WeaponEnum.Sword:
                return 9;
            case WeaponEnum.Lance:
                return 10;
            case WeaponEnum.Axe:
                return 12;
            case WeaponEnum.Bow:
                return 11;
            case WeaponEnum.Tome:
            case WeaponEnum.Shuriken:
                return 7;
            case WeaponEnum.Staff:
                return 1;
            case WeaponEnum.Fist:
                return 8;
            default:
                return 0;
        }
    }
}
