using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

public class ClassDataManager : MonoBehaviour
{
    private const int CORRIN_GROWTH_HP = 45;
    private const int CORRIN_GROWTH_STR = 45;
    private const int CORRIN_GROWTH_MAG = 30;
    private const int CORRIN_GROWTH_SKL = 40;
    private const int CORRIN_GROWTH_SPD = 45;
    private const int CORRIN_GROWTH_LCK = 45;
    private const int CORRIN_GROWTH_DEF = 35;
    private const int CORRIN_GROWTH_RES = 25;
    private const int CORRIN_BASE_HP = 2;
    private const int CORRIN_BASE_STR = 0;
    private const int CORRIN_BASE_MAG = 1;
    private const int CORRIN_BASE_SKL = 3;
    private const int CORRIN_BASE_SPD = 1;
    private const int CORRIN_BASE_LCK = 3;
    private const int CORRIN_BASE_DEF = 1;
    private const int CORRIN_BASE_RES = 0;

    private List<UnitClass> classes;
    private Dictionary<string, UnitClass> classesByName;

    [SerializeField] private TextAsset csvFile; // Assign in inspector

    private static ClassDataManager instance;
    public static ClassDataManager Instance
    {
        get { return instance; }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadClassData();
        } else
        {
            Destroy(gameObject);
        }
    }

    private void LoadClassData()
    {
        classes = new List<UnitClass>();
        classesByName = new Dictionary<string, UnitClass>();

        if (csvFile == null)
        {
            Debug.LogError("CSV file not assigned in the inspector!");
            return;
        }

        string[] lines = csvFile.text.Split('\n', '\r');

        // Skip header row (assuming first row is header)
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] rowData = SplitCSVLine(lines[i]);

            try
            {
                UnitClass classData = UnitClass.CreateFromCSVRow(rowData);
                classes.Add(classData);
                classesByName[classData.name] = classData;
            } catch (Exception e)
            {
                Debug.LogError($"Error parsing row {i + 1}: {e.Message}");
            }
        }

        Debug.Log($"Loaded {classes.Count} classes");
    }

    // Helper method to handle CSV parsing (handles quoted fields)
    private string[] SplitCSVLine(string line)
    {
        List<string> fields = new List<string>();
        int position = 0;
        bool insideQuotes = false;
        string currentField = "";

        while (position < line.Length)
        {
            char c = line[position];

            if (c == '"')
            {
                insideQuotes = !insideQuotes;
            } else if (c == ',' && !insideQuotes)
            {
                fields.Add(currentField);
                currentField = "";
            } else
            {
                currentField += c;
            }

            position++;
        }

        fields.Add(currentField); // Add the last field
        return fields.ToArray();
    }

    public UnitClass GetClassDataByIndex(int index)
    {
        if (index >= 0 && index < classes.Count)
        {
            return classes[index];
        }
        return null;
    }

    public UnitClass GetClassDataByName(string className)
    {
        if (classesByName.TryGetValue(className, out UnitClass classData))
        {
            return classData;
        }
        return null;
    }

    public int GetClassCount()
    {
        return classes.Count;
    }

    // Method to apply class and level to a unit
    public void ApplyClassAndLevel(Unit unit, string className, int level)
    {
        UnitClass classData = GetClassDataByName(className);
        if (classData == null)
        {
            Debug.LogError($"Class {className} not found!");
            return;
        }

        ApplyClassAndLevel(unit, classData, level);
    }

    public void ApplyClassAndLevel(Unit unit, UnitClass classData, int level)
    {
        // Set the unit's class
        unit.unitClass = classData;

        // Apply base stats (level 1)
        unit.maxHP = classData.baseHP + CORRIN_BASE_HP;
        unit.strength = classData.baseStr + CORRIN_BASE_STR;
        unit.magic = classData.baseMag + CORRIN_BASE_MAG;
        unit.skill = classData.baseSkl + CORRIN_BASE_SKL;
        unit.speed = classData.baseSpd + CORRIN_BASE_SPD;
        unit.luck = classData.baseLck + CORRIN_BASE_LCK;
        unit.defense = classData.baseDef + CORRIN_BASE_DEF;
        unit.resistance = classData.baseRes + CORRIN_BASE_RES;

        // Apply level-up growths if level > 1
        if (level > 1)
        {
            int levelUps = level - 1;

            // HP
            ApplyGrowth(ref unit.maxHP, classData.growthHP + CORRIN_GROWTH_HP, classData.capHP, levelUps);
            

            // Other stats
            ApplyGrowth(ref unit.strength, classData.growthStr + CORRIN_GROWTH_STR, classData.capStr, levelUps);
            ApplyGrowth(ref unit.magic, classData.growthMag + CORRIN_GROWTH_MAG, classData.capMag, levelUps);
            ApplyGrowth(ref unit.skill, classData.growthSkl + CORRIN_GROWTH_SKL, classData.capSkl, levelUps);
            ApplyGrowth(ref unit.speed, classData.growthSpd + CORRIN_GROWTH_SPD, classData.capSpd, levelUps);
            ApplyGrowth(ref unit.luck, classData.growthLck + CORRIN_GROWTH_LCK, classData.capLck, levelUps);
            ApplyGrowth(ref unit.defense, classData.growthDef + CORRIN_GROWTH_DEF, classData.capDef, levelUps);
            ApplyGrowth(ref unit.resistance, classData.growthRes + CORRIN_GROWTH_RES, classData.capRes, levelUps);
        }

        if (unit.scaleFactor > 1)
        {
            unit.maxHP = (int)Mathf.Round(unit.maxHP * Mathf.Exp(unit.scaleFactor));
        } else
        {
            unit.maxHP = (int)Mathf.Round(unit.maxHP * unit.scaleFactor * unit.scaleFactor);
        }
        unit.hp = unit.maxHP;

        unit.UpdateMovementParameters();
    }

    // Helper method to apply growth to a stat
    private void ApplyGrowth(ref int stat, int growthRate, int cap, int levelUps)
    {
        // Calculate total growth points
        int growthPoints = growthRate * levelUps;

        // Calculate stat increases (with special rounding rule)
        int statIncrease = growthPoints / 100;

        // If we have leftover points between 50 and 99, add one more point
        int remainder = growthPoints % 100;
        if (remainder >= 50)
        {
            statIncrease += 1;
        }

        // Apply the increase (up to the cap)
        stat = Mathf.Min(stat + statIncrease, cap);
    }

    /// <summary>
    /// Get all unique flags from all classes
    /// </summary>
    public List<string> GetAllClassFlags()
    {
        HashSet<string> uniqueFlags = new HashSet<string>();

        foreach (var unitClass in classes)
        {
            // Parse flags from the class data (assuming flags are in a specific field)
            // You'll need to add a flags field to UnitClass if not already present
            if (!string.IsNullOrEmpty(unitClass.flags))
            {
                string[] flagArray = unitClass.flags.Split(',');
                foreach (string flag in flagArray)
                {
                    string trimmedFlag = flag.Trim();
                    if (!string.IsNullOrEmpty(trimmedFlag))
                    {
                        uniqueFlags.Add(trimmedFlag);
                    }
                }
            }
        }

        return uniqueFlags.ToList();
    }

    /// <summary>
    /// Get all classes that have a specific flag
    /// </summary>
    public List<UnitClass> GetClassesWithFlag(string flag)
    {
        List<UnitClass> matchingClasses = new List<UnitClass>();

        foreach (var unitClass in classes)
        {
            if (!string.IsNullOrEmpty(unitClass.flags) && unitClass.flags.Contains(flag))
            {
                matchingClasses.Add(unitClass);
            }
        }

        return matchingClasses;
    }

    /// <summary>
    /// Get all classes of a specific movement type
    /// </summary>
    public List<UnitClass> GetClassesByMovementType(MovementType movementType)
    {
        return classes.Where(c => c.movementType == movementType).ToList();
    }

    /// <summary>
    /// Get classes by promotion status
    /// </summary>
    public List<UnitClass> GetClassesByPromotionStatus(bool promoted)
    {
        return classes.Where(c => c.promoted == promoted).ToList();
    }

    /// <summary>
    /// Get classes by preferred weapon
    /// </summary>
    public List<UnitClass> GetClassesByPreferredWeapon(WeaponEnum weapon)
    {
        return classes.Where(c => c.preferredWeapon == weapon).ToList();
    }

    /// <summary>
    /// Get all unique weapon types used as preferred weapons
    /// </summary>
    public List<WeaponEnum> GetAllPreferredWeapons()
    {
        return classes.Select(c => c.preferredWeapon)
                      .Where(w => w != WeaponEnum.None)
                      .Distinct()
                      .ToList();
    }

    /// <summary>
    /// Get classes in the top 25% for a specific base stat
    /// </summary>
    public List<UnitClass> GetClassesWithHighBaseStat(StatType statType)
    {
        if (classes.Count == 0) return new List<UnitClass>();

        // Get all stat values for this type
        var statValues = classes.Select(c => GetBaseStat(c, statType)).OrderByDescending(x => x).ToList();

        // Calculate 75th percentile threshold
        int top25PercentIndex = Mathf.Max(0, Mathf.FloorToInt(statValues.Count * 0.25f));
        float threshold = statValues[top25PercentIndex];

        // Return classes that meet or exceed the threshold
        return classes.Where(c => GetBaseStat(c, statType) >= threshold).ToList();
    }

    /// <summary>
    /// Get all stat types that have meaningful variation (for stat-based themes)
    /// </summary>
    public List<StatType> GetAvailableStatTypes()
    {
        return new List<StatType>
    {
        StatType.HP, StatType.Str, StatType.Mag, StatType.Skl,
        StatType.Spd, StatType.Lck, StatType.Def, StatType.Res
    };
    }

    /// <summary>
    /// Helper method to get base stat value by type
    /// </summary>
    private int GetBaseStat(UnitClass unitClass, StatType statType)
    {
        switch (statType)
        {
            case StatType.HP: return unitClass.baseHP;
            case StatType.Str: return unitClass.baseStr;
            case StatType.Mag: return unitClass.baseMag;
            case StatType.Skl: return unitClass.baseSkl;
            case StatType.Spd: return unitClass.baseSpd;
            case StatType.Lck: return unitClass.baseLck;
            case StatType.Def: return unitClass.baseDef;
            case StatType.Res: return unitClass.baseRes;
            default: return 0;
        }
    }

    // Define StatType enum if it doesn't exist
    public enum StatType
    {
        HP, Str, Mag, Skl, Spd, Lck, Def, Res
    }

}