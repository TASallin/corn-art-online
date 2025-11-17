using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AIStateFactory
{
    public static UnitAIState NormalSeekFactory(WeaponEnum weaponType)
    {
        switch (weaponType)
        {
            case WeaponEnum.Sword:
                AIStateSeekMelee swordState = new AIStateSeekMelee();
                swordState.attackRadius = 3.0f;
                swordState.attackAngle = 90;
                return swordState;
            case WeaponEnum.Axe:
                AIStateSeekMelee axeState = new AIStateSeekMelee();
                axeState.attackRadius = 4f;
                axeState.attackAngle = 50;
                return axeState;
            case WeaponEnum.Lance:
                AIStateSeekMelee lanceState = new AIStateSeekMelee();
                lanceState.attackRadius = 4.5f;
                lanceState.attackAngle = 90;
                return lanceState;
            case WeaponEnum.Tome:
                AIStateSeekMelee tomeState = new AIStateSeekMelee();
                tomeState.attackRadius = 20f;
                tomeState.attackAngle = 90;
                return tomeState;
            case WeaponEnum.Bow:
                AIStateSeekBoxed bowState = new AIStateSeekBoxed();
                bowState.verticalMinRange = 0f;
                bowState.verticalMaxRange = 0.6f;
                bowState.horizontalMinRange = 5f;
                bowState.horizontalMaxRange = 50f; //Would be 50 but 30 is whole map
                return bowState;
            case WeaponEnum.Shuriken:
                AIStateSeekMelee shurikenState = new AIStateSeekMelee();
                shurikenState.attackRadius = 25f;
                shurikenState.attackAngle = 60;
                shurikenState.minAttackAngle = 30;
                return shurikenState;
            case WeaponEnum.Staff:
                AIStateSeekMelee staffState = new AIStateSeekMelee();
                staffState.attackRadius = 40f;
                staffState.attackAngle = 90;
                return staffState;
            case WeaponEnum.Fist:
                AIStateSeekMelee fistState = new AIStateSeekMelee();
                fistState.attackRadius = 2.5f;
                fistState.attackAngle = 40;
                return fistState;
            default:
                return new AIStateSeekMelee();
        }
    }

    public static UnitAIState SpecialSeekFactory(WeaponEnum weaponType)
    {
        switch (weaponType)
        {
            case WeaponEnum.Sword:
                AIStateSeekMelee swordState = new AIStateSeekMelee();
                swordState.attackRadius = 10f;
                swordState.attackAngle = 180;
                return swordState;
            case WeaponEnum.Axe:
                AIStateSeekMelee axeState = new AIStateSeekMelee();
                axeState.attackRadius = 25f;
                axeState.attackAngle = 20;
                return axeState;
            case WeaponEnum.Lance:
                AIStateSeekFromBelow lanceState = new AIStateSeekFromBelow();
                lanceState.verticalRange = 25f;
                lanceState.horizontalMinRange = 1f;
                lanceState.horizontalMaxRange = 4f;
                return lanceState;
            case WeaponEnum.Tome:
                AIStateSeekMelee tomeState = new AIStateSeekMelee();
                tomeState.attackRadius = 15f;
                tomeState.attackAngle = 45;
                return tomeState;
            case WeaponEnum.Bow:
                AIStateSeekBoxed bowState = new AIStateSeekBoxed();
                bowState.verticalMinRange = 0f;
                bowState.verticalMaxRange = 0.8f;
                bowState.horizontalMinRange = 10f;
                bowState.horizontalMaxRange = 50f; //Would be 50 but 30 is whole map
                return bowState;
            case WeaponEnum.Shuriken:
                AIStateSeekMelee shurikenState = new AIStateSeekMelee();
                shurikenState.attackRadius = 20f;
                shurikenState.attackAngle = 75;
                shurikenState.minAttackAngle = 15;
                return shurikenState;
            case WeaponEnum.Staff:
                AIStateSeekMelee staffState = new AIStateSeekMelee();
                staffState.attackRadius = 30f;
                staffState.attackAngle = 30;
                return staffState;
            case WeaponEnum.Fist:
                AIStateSeekMelee fistState = new AIStateSeekMelee();
                fistState.attackRadius = 7;
                fistState.attackAngle = 30;
                return fistState;
            default:
                return new AIStateSeekMelee();
        }
    }

    public static UnitAIState NormalAttackFactory(WeaponEnum weaponType)
    {
        switch (weaponType)
        {
            case WeaponEnum.Sword:
                return new AIStateSwordAttack();
            case WeaponEnum.Axe:
                return new AIStateAxeAttack();
            case WeaponEnum.Lance:
                return new AIStateLanceAttack();
            case WeaponEnum.Tome:
                return new AIStateTomeAttack();
            case WeaponEnum.Bow:
                return new AIStateBowAttack();
            case WeaponEnum.Shuriken:
                return new AIStateShurikenAttack();
            case WeaponEnum.Staff:
                return new AIStateStaffAttack();
            case WeaponEnum.Fist:
                return new AIStateFistAttack();
            default:
                return new AIStateSwordAttack();
        }
    }

    public static UnitAIState SpecialAttackFactory(WeaponEnum weaponType)
    {
        switch (weaponType)
        {
            case WeaponEnum.Sword:
                return new AIStateSwordSpecial();
            case WeaponEnum.Axe:
                return new AIStateAxeSpecial();
            case WeaponEnum.Lance:
                return new AIStateLanceSpecial();
            case WeaponEnum.Tome:
                return new AIStateTomeSpecial();
            case WeaponEnum.Bow:
                return new AIStateBowSpecial();
            case WeaponEnum.Shuriken:
                return new AIStateShurikenSpecial();
            case WeaponEnum.Staff:
                return new AIStateStaffSpecial();
            case WeaponEnum.Fist:
                return new AIStateFistSpecial();
            default:
                return new AIStateSwordSpecial();
        }
    }

    public static WeaponRangeData GetNormalWeaponRanges(WeaponEnum weaponType)
    {
        switch (weaponType)
        {
            case WeaponEnum.Sword:
                return new WeaponRangeData { minRange = 0f, maxRange = 3.0f, isRadial = true, scalesWithUnit = true };
            case WeaponEnum.Axe:
                return new WeaponRangeData { minRange = 0f, maxRange = 4f, isRadial = true, scalesWithUnit = true };
            case WeaponEnum.Lance:
                return new WeaponRangeData { minRange = 0f, maxRange = 4.5f, isRadial = true, scalesWithUnit = true };
            case WeaponEnum.Tome:
                return new WeaponRangeData { minRange = 0f, maxRange = 20f, isRadial = true, scalesWithUnit = false };
            case WeaponEnum.Bow:
                return new WeaponRangeData { minRange = 5f, maxRange = 30f, isRadial = false, scalesWithUnit = false };
            case WeaponEnum.Shuriken:
                return new WeaponRangeData { minRange = 0f, maxRange = 25f, isRadial = true, scalesWithUnit = false };
            case WeaponEnum.Staff:
                return new WeaponRangeData { minRange = 0f, maxRange = 40f, isRadial = true, scalesWithUnit = false };
            case WeaponEnum.Fist:
                return new WeaponRangeData { minRange = 0f, maxRange = 2.5f, isRadial = true, scalesWithUnit = true };
            default:
                return new WeaponRangeData { minRange = 0f, maxRange = 3.0f, isRadial = true, scalesWithUnit = true };
        }
    }

    public static WeaponRangeData GetSpecialWeaponRanges(WeaponEnum weaponType)
    {
        switch (weaponType)
        {
            case WeaponEnum.Sword:
                return new WeaponRangeData { minRange = 0f, maxRange = 3f, isRadial = true, scalesWithUnit = true };
            case WeaponEnum.Axe:
                return new WeaponRangeData { minRange = 0f, maxRange = 25f, isRadial = true, scalesWithUnit = false };
            case WeaponEnum.Lance:
                return new WeaponRangeData { minRange = 1f, maxRange = 4f, isRadial = true, scalesWithUnit = true };
            case WeaponEnum.Tome:
                return new WeaponRangeData { minRange = 0f, maxRange = 15f, isRadial = true, scalesWithUnit = false };
            case WeaponEnum.Bow:
                return new WeaponRangeData { minRange = 10f, maxRange = 30f, isRadial = false, scalesWithUnit = false };
            case WeaponEnum.Shuriken:
                return new WeaponRangeData { minRange = 0f, maxRange = 20f, isRadial = true, scalesWithUnit = false };
            case WeaponEnum.Staff:
                return new WeaponRangeData { minRange = 0f, maxRange = 30f, isRadial = true, scalesWithUnit = false };
            case WeaponEnum.Fist:
                return new WeaponRangeData { minRange = 0f, maxRange = 7f, isRadial = true, scalesWithUnit = true };
            default:
                return new WeaponRangeData { minRange = 0f, maxRange = 10f, isRadial = true, scalesWithUnit = true };
        }
    }

    public struct WeaponRangeData
    {
        public float minRange;
        public float maxRange;
        public bool isRadial;
        public bool scalesWithUnit;
    }
}
