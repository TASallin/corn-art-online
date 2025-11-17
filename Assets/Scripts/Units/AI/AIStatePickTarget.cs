using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStatePickTarget : UnitAIState
{
    public float preferredRange;
    public int wtaBonus = 5;
    public int minPenaltyCount = 10;
    public int maxPenaltyCount = 60;
    public int penaltyAddPercent = 1;
    public int maxPenalty = 90;
    public static readonly float rangedPreferredDistance = 10;
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
        System.Random rng = GameManager.GetInstance().rng;
        int penalty = 0;
        if (validTargets.Count > minPenaltyCount)
        {
            penalty = penaltyAddPercent * (System.Math.Min(validTargets.Count, maxPenaltyCount) - minPenaltyCount);
        }
        //TODO more specific ranges
        if (StatCalcs.IsRangedWeapon(parentAI.user.equippedWeapon))
        {
            preferredRange = 15;
        } else
        {
            preferredRange = 0;
        }
        Unit chosenTarget = null;
        Unit defaultTarget = null;
        int bestPriority = 10000;
        PriorityQueue<Unit> targetQueue = new PriorityQueue<Unit>();
        foreach (Unit u in validTargets)
        {
            if (u == null)
            {
                continue;
            }
            int priority = (int)System.Math.Abs((u.transform.position - parentAI.user.transform.position).magnitude - preferredRange);
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
        if (betterWeapons.Count > 0 && rng.NextDouble() < improveWeaponChance) {
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
        if (StatCalcs.IsRangedWeapon(weapon) && distance > rangedPreferredDistance)
        {
            score += 1;
        } else if (!StatCalcs.IsRangedWeapon(weapon) && distance < rangedPreferredDistance)
        {
            score += 1;
        }
        return score;
    }
}
