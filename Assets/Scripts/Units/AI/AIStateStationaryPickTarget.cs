using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateStationaryPickTarget : UnitAIState
{
    public int wtaBonus = 5;
    public int minPenaltyCount = 10;
    public int maxPenaltyCount = 60;
    public int penaltyAddPercent = 1;
    public int maxPenalty = 90;
    public static readonly float improveWeaponChance = 0.5f;
    public static readonly float sidegradeWeaponChance = 0.15f;

    public override IEnumerator Run()
    {
        List<Unit> validTargets = parentAI.user.armyManager.GetLivingEnemies(parentAI.user);
        if (validTargets.Count == 0)
        {
            parentAI.nextState = AIState.Idle;
            yield break;
        }

        // Determine if using special attack first
        bool useSpecial = GameManager.GetInstance().rng.Next(100) < parentAI.user.GetSkill();
        ((StationaryUnitAI)parentAI).usingSpecial = useSpecial;

        // Get the attack ranges for the current weapon
        var ranges = useSpecial ?
            AIStateFactory.GetSpecialWeaponRanges(parentAI.user.equippedWeapon) :
            AIStateFactory.GetNormalWeaponRanges(parentAI.user.equippedWeapon);

        // Filter targets to only those in range
        List<Unit> targetsInRange = new List<Unit>();
        foreach (Unit u in validTargets)
        {
            if (u == null) continue;
            if (IsTargetInRange(u, ranges))
            {
                targetsInRange.Add(u);
            }
        }

        // If no targets in range, wait and try again
        if (targetsInRange.Count == 0)
        {
            parentAI.nextState = AIState.Target;
            yield break;
        }

        System.Random rng = GameManager.GetInstance().rng;
        int penalty = 0;
        if (targetsInRange.Count > minPenaltyCount)
        {
            penalty = penaltyAddPercent * (System.Math.Min(targetsInRange.Count, maxPenaltyCount) - minPenaltyCount);
        }

        Unit chosenTarget = null;
        Unit defaultTarget = null;
        int bestPriority = 10000;
        PriorityQueue<Unit> targetQueue = new PriorityQueue<Unit>();

        foreach (Unit u in targetsInRange)
        {
            int priority = (int)(u.transform.position - parentAI.user.transform.position).magnitude;
            if (StatCalcs.IsWeaponTriangleAdvantage(parentAI.user.equippedWeapon, u.equippedWeapon))
            {
                priority -= wtaBonus;
            } else if (StatCalcs.IsWeaponTriangleDisadvantage(parentAI.user.equippedWeapon, u.equippedWeapon))
            {
                priority += wtaBonus;
            }
            targetQueue.Enqueue(u, priority);
            if (priority < bestPriority)
            {
                bestPriority = priority;
                defaultTarget = u;
            }
        }

        while (!chosenTarget)
        {
            if (targetQueue.IsEmpty())
            {
                chosenTarget = defaultTarget;
                break;
            }
            Unit candidate = targetQueue.Dequeue();
            int skipChance = System.Math.Min(maxPenalty, penalty + candidate.GetLuck());
            if (rng.Next(100) >= skipChance)
            {
                chosenTarget = candidate;
            }
        }

        parentAI.target = chosenTarget;
        WeaponEnum weaponSwap = ChooseWeaponSwap(parentAI.user, parentAI.target);
        if (weaponSwap != parentAI.user.equippedWeapon)
        {
            yield return parentAI.user.spriteManager.weaponView.PutAway();
            parentAI.user.spriteManager.weaponFactory.SwitchWeapon(parentAI.user, weaponSwap);
            parentAI.SetWeaponStates();
            yield return parentAI.user.spriteManager.weaponView.TakeOut();
        }
        yield return null;
    }

    private bool IsTargetInRange(Unit target, AIStateFactory.WeaponRangeData ranges)
    {
        Vector3 direction = target.transform.position - parentAI.user.transform.position;
        float distance = direction.magnitude;

        // Apply scale factor if weapon type requires it
        float maxRange = ranges.maxRange;
        float minRange = ranges.minRange;
        if (ranges.scalesWithUnit)
        {
            maxRange *= parentAI.user.scaleFactor;
            minRange *= parentAI.user.scaleFactor;
        }

        if (ranges.isRadial)
        {
            return distance >= minRange && distance <= maxRange;
        } else
        {
            // For boxed ranges, check horizontal and vertical separately
            float horizontalDistance = Mathf.Abs(direction.x);
            float verticalDistance = Mathf.Abs(direction.y);
            return horizontalDistance >= minRange && horizontalDistance <= maxRange &&
                   verticalDistance <= 0.8f;
        }
    }

    public WeaponEnum ChooseWeaponSwap(Unit user, Unit target)
    {
        if (target == null)
        {
            return user.equippedWeapon;
        }
        WeaponEnum newWeapon = user.equippedWeapon;
        WeaponEnum targetWeapon = target.equippedWeapon;
        List<WeaponEnum> betterWeapons = new List<WeaponEnum>();
        List<WeaponEnum> equalWeapons = new List<WeaponEnum>();
        List<WeaponEnum> usableWeapons = user.unitClass.GetUsableWeapons();
        int currentScore = GetWeaponSwapScore(user, target, user.equippedWeapon);
        foreach (WeaponEnum weapon in usableWeapons)
        {
            if (weapon == user.equippedWeapon)
            {
                continue;
            }
            int score = GetWeaponSwapScore(user, target, weapon);
            if (score > currentScore)
            {
                betterWeapons.Add(weapon);
            } else if (score == currentScore)
            {
                equalWeapons.Add(weapon);
            }
        }
        System.Random rng = GameManager.GetInstance().rng;
        if (betterWeapons.Count > 0 && rng.NextDouble() < improveWeaponChance)
        {
            newWeapon = betterWeapons[rng.Next(betterWeapons.Count)];
        } else if (betterWeapons.Count == 0 && equalWeapons.Count > 0 && rng.NextDouble() < sidegradeWeaponChance)
        {
            newWeapon = equalWeapons[rng.Next(equalWeapons.Count)];
        }
        return newWeapon;
    }

    public int GetWeaponSwapScore(Unit user, Unit target, WeaponEnum weapon)
    {
        int score = 0;
        if (StatCalcs.IsWeaponTriangleAdvantage(weapon, target.equippedWeapon))
        {
            score += 1;
        } else if (StatCalcs.IsWeaponTriangleDisadvantage(weapon, target.equippedWeapon))
        {
            score -= 2;
        }
        float distance = (target.transform.position - user.transform.position).magnitude;
        if (StatCalcs.IsRangedWeapon(weapon) && distance > 10f)
        {
            score += 1;
        } else if (!StatCalcs.IsRangedWeapon(weapon) && distance < 10f)
        {
            score += 1;
        }
        return score;
    }
}