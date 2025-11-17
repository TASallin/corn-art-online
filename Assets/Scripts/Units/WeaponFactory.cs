using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponFactory : MonoBehaviour
{
    public GameObject swordPrefab;
    public GameObject axePrefab;
    public GameObject lancePrefab;
    public GameObject tomePrefab;
    public GameObject bowPrefab;
    public GameObject shurikenPrefab;
    public GameObject staffPrefab;
    public GameObject fistPrefab;

    public void SwitchWeapon(Unit unit, WeaponEnum weapon)
    {
        UnitSprite spriteManager = unit.spriteManager;
        GameObject newWeapon;
        switch (weapon)
        {
            case WeaponEnum.Sword:
                newWeapon = swordPrefab;
                break;
            case WeaponEnum.Axe:
                newWeapon = axePrefab;
                break;
            case WeaponEnum.Lance:
                newWeapon = lancePrefab;
                break;
            case WeaponEnum.Tome:
                newWeapon = tomePrefab;
                break;
            case WeaponEnum.Bow:
                newWeapon = bowPrefab;
                break;
            case WeaponEnum.Shuriken:
                newWeapon = shurikenPrefab;
                break;
            case WeaponEnum.Staff:
                newWeapon = staffPrefab;
                break;
            case WeaponEnum.Fist:
                newWeapon = fistPrefab;
                break;
            default:
                newWeapon = swordPrefab;
                break;
        }
        if (spriteManager.weaponView != null)
        {
            Destroy(spriteManager.weaponView.gameObject);
        }
        spriteManager.weaponView = Instantiate(newWeapon, transform).GetComponent<WeaponView>();
        spriteManager.weaponView.ResetPosition();
        spriteManager.weaponView.owner = unit;
        unit.equippedWeapon = weapon;
    }
}
