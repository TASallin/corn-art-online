using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeizeUnitAI : UnitAI
{
    public UnitAIState normalAttack;
    public UnitAIState specialAttack;
    public UnitAIState seek;
    public UnitAIState normalSeek;
    public UnitAIState specialSeek;
    public UnitAIState defend;
    public UnitAIState idle;
    public UnitAIState pickTarget;
    public UnitAIState seekTargetPoint;
    public UnitAIState currentStateBehavior;
    public IEnumerator currentStateRun;
    public bool usingSpecial;

    // Start is called before the first frame update
    void Start()
    {
        idle = AttachAIState(new AIStateIdle());
        defend = AttachAIState(new AIStateDefend());
        pickTarget = AttachAIState(new AIStatePickTarget());
        seekTargetPoint = AttachAIState(new AIStateSeekTargetPoint());
        SetWeaponStates();
        EnterState(state);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public override void EnterState(AIState state)
    {
        this.state = state;
        switch (state)
        {
            case AIState.Target:
                currentStateBehavior = pickTarget;
                break;
            case AIState.Seek:
                currentStateBehavior = seek;
                break;
            case AIState.Attack:
                user.spriteManager.PlayVoiceClip(VoiceType.Attack);
                currentStateBehavior = normalAttack;
                break;
            case AIState.Special:
                user.spriteManager.PlayVoiceClip(VoiceType.Crit);
                currentStateBehavior = specialAttack;
                break;
            case AIState.Defend:
            case AIState.Dead:
                currentStateBehavior = defend;
                break;
            case AIState.SeekTarget:
                currentStateBehavior = seekTargetPoint;
                break;
            default:
                currentStateBehavior = idle;
                break;
        }
        StartCoroutine("WaitForStateChange");
    }

    public override void ExitState(AIState state)
    {
        AIState stateChange = AIState.None;
        if (nextState != AIState.None)
        {
            stateChange = nextState;
            nextState = AIState.None;
        } else
        {
            switch (state)
            {
                case AIState.Target:
                    if (GameManager.GetInstance().rng.Next(100) < user.GetSkill())
                    {
                        usingSpecial = true;
                        seek = specialSeek;
                    } else
                    {
                        usingSpecial = false;
                        seek = normalSeek;
                    }
                    stateChange = AIState.Seek;
                    break;
                case AIState.Seek:
                    if (usingSpecial)
                    {
                        stateChange = AIState.Special;
                    } else
                    {
                        stateChange = AIState.Attack;
                    }
                    if (target == null)
                    {
                        stateChange = AIState.Target;
                    }
                    break;
                case AIState.Attack:
                case AIState.Special:
                    stateChange = AIState.Defend;
                    break;
                case AIState.Defend:
                    // 10% chance to seek target point before picking target
                    if (GameManager.GetInstance().rng.Next(100) < 10)
                    {
                        stateChange = AIState.SeekTarget;
                    } else
                    {
                        stateChange = AIState.Target;
                    }
                    break;
                case AIState.Idle:
                    // 50% chance to seek target point, otherwise pick target
                    if (GameManager.GetInstance().rng.Next(100) < 50)
                    {
                        stateChange = AIState.SeekTarget;
                    } else
                    {
                        stateChange = AIState.Target;
                    }
                    break;
                case AIState.SeekTarget:
                    stateChange = AIState.Target;
                    break;
                case AIState.Dead:
                    stateChange = AIState.Dead;
                    break;
                default:
                    stateChange = AIState.Idle;
                    break;
            }
        }
        EnterState(stateChange);
    }

    public IEnumerator WaitForStateChange()
    {
        currentStateRun = currentStateBehavior.Run();
        yield return currentStateRun;
        ExitState(state);
    }

    public override void ForceTransition(AIState state)
    {
        nextState = AIState.None;
        if (currentStateRun != null)
        {
            StopCoroutine(currentStateRun);
            currentStateBehavior.ForceExit();
        }
        StopCoroutine("WaitForStateChange");
        EnterState(state);
    }

    public override void SetWeaponStates()
    {
        normalSeek = AttachAIState(AIStateFactory.NormalSeekFactory(user.equippedWeapon));
        specialSeek = AttachAIState(AIStateFactory.SpecialSeekFactory(user.equippedWeapon));
        normalAttack = AttachAIState(AIStateFactory.NormalAttackFactory(user.equippedWeapon));
        specialAttack = AttachAIState(AIStateFactory.SpecialAttackFactory(user.equippedWeapon));
    }
}