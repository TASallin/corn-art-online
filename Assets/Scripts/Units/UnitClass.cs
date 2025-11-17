using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class UnitClass
{
    public WeaponEnum preferredWeapon;
    public MovementType movementType;
    public bool[] weaponMask;
    public int level;
    public bool promoted;
    public int baseHP;
    public int baseStr;
    public int baseMag;
    public int baseSkl;
    public int baseSpd;
    public int baseLck;
    public int baseDef;
    public int baseRes;
    public int growthHP;
    public int growthStr;
    public int growthMag;
    public int growthSkl;
    public int growthSpd;
    public int growthLck;
    public int growthDef;
    public int growthRes;
    public int capHP;
    public int capStr;
    public int capMag;
    public int capSkl;
    public int capSpd;
    public int capLck;
    public int capDef;
    public int capRes;
    public string name;
    public string flags;

    public UnitClass()
    {
        weaponMask = new bool[9];
    }

    public static void FillDebugClass(UnitClass c)
    {
        c.preferredWeapon = WeaponEnum.Fist;
        c.weaponMask[1] = true;
        c.weaponMask[2] = true;
        c.weaponMask[3] = true;
        c.weaponMask[4] = true;
        c.weaponMask[5] = true;
        c.weaponMask[6] = true;
        c.weaponMask[7] = true;
        c.weaponMask[8] = true;
    }

    public List<WeaponEnum> GetUsableWeapons()
    {
        List<WeaponEnum> weapons = new List<WeaponEnum>();
        foreach (WeaponEnum weapon in System.Enum.GetValues(typeof(WeaponEnum)))
        {
            if (weaponMask[(int)weapon]) weapons.Add(weapon);
        }
        return weapons;
    }

    public static UnitClass CreateFromCSVRow(string[] rowData)
    {
        if (rowData.Length < 38)
        {
            throw new ArgumentException("Row data does not contain enough columns");
        }

        UnitClass classData = new UnitClass
        {
            name = rowData[0],
            promoted = bool.Parse(rowData[1]), // Parse promoted status from column 2
            weaponMask = new bool[Enum.GetNames(typeof(WeaponEnum)).Length],
            baseHP = int.Parse(rowData[13]),
            baseStr = int.Parse(rowData[14]),
            baseMag = int.Parse(rowData[15]),
            baseSkl = int.Parse(rowData[16]),
            baseSpd = int.Parse(rowData[17]),
            baseLck = int.Parse(rowData[18]),
            baseDef = int.Parse(rowData[19]),
            baseRes = int.Parse(rowData[20]),
            growthHP = int.Parse(rowData[21]),
            growthStr = int.Parse(rowData[22]),
            growthMag = int.Parse(rowData[23]),
            growthSkl = int.Parse(rowData[24]),
            growthSpd = int.Parse(rowData[25]),
            growthLck = int.Parse(rowData[26]),
            growthDef = int.Parse(rowData[27]),
            growthRes = int.Parse(rowData[28]),
            capHP = int.Parse(rowData[29]),
            capStr = int.Parse(rowData[30]),
            capMag = int.Parse(rowData[31]),
            capSkl = int.Parse(rowData[32]),
            capSpd = int.Parse(rowData[33]),
            capLck = int.Parse(rowData[34]),
            capDef = int.Parse(rowData[35]),
            capRes = int.Parse(rowData[36])
        };

        // Process weapon information (columns 3-5, indices 2-4)
        for (int i = 2; i <= 4; i++)
        {
            if (i < rowData.Length && !string.IsNullOrWhiteSpace(rowData[i]))
            {
                WeaponEnum weapon = ConvertToWeaponEnum(rowData[i]);

                // Set the first found weapon as preferred
                if (i == 2)
                {
                    classData.preferredWeapon = weapon;
                }

                // Mark this weapon as usable in the mask (if it's not None)
                if (weapon != WeaponEnum.None)
                {
                    classData.weaponMask[(int)weapon] = true;
                }
            }
        }

        // Process movement type from flags (column 9, index 8)
        if (rowData.Length > 8 && !string.IsNullOrEmpty(rowData[8]))
        {
            string flags = rowData[8].ToLower();
            classData.flags = rowData[8]; // Store flags for other uses

            if (flags.Contains("flying"))
            {
                classData.movementType = MovementType.Flier;
            } else if (flags.Contains("mounted"))
            {
                classData.movementType = MovementType.Cavalry;
            } else
            {
                classData.movementType = MovementType.Infantry;
            }
        } else
        {
            classData.movementType = MovementType.Infantry; // Default
        }

        return classData;
    }

    private static WeaponEnum ConvertToWeaponEnum(string weaponStr)
    {
        weaponStr = weaponStr.Trim();

        // Handle the naming differences
        if (string.Equals(weaponStr, "Dagger", StringComparison.OrdinalIgnoreCase))
        {
            return WeaponEnum.Shuriken;
        } else if (string.Equals(weaponStr, "Stone", StringComparison.OrdinalIgnoreCase))
        {
            return WeaponEnum.Fist;
        }

        // Try to match with the enum
        foreach (WeaponEnum weapon in Enum.GetValues(typeof(WeaponEnum)))
        {
            if (string.Equals(weapon.ToString(), weaponStr, StringComparison.OrdinalIgnoreCase))
            {
                return weapon;
            }
        }

        Debug.LogWarning($"Unknown weapon type: {weaponStr}. Defaulting to None.");
        return WeaponEnum.None;
    }

    public bool IsRangedClass()
    {
        return StatCalcs.IsRangedWeapon(preferredWeapon);
    }
}
