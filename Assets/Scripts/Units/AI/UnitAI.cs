using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UnitAI : MonoBehaviour
{
    public AIState state;
    public AIState nextState;
    public Unit target;
    public Unit user;

    // This will be called when the component is added
    void Awake()
    {
        // Find the Unit component on the same GameObject
        user = GetComponent<Unit>();
        if (user != null)
        {
            // Set this AI as the unit's AI script
            user.aiScript = this;
        } else
        {
            Debug.LogError($"UnitAI component on {gameObject.name} could not find a Unit component!");
        }
        if (state == AIState.None)
        {
            state = AIState.Defend;
        }
    }

    public abstract void EnterState(AIState state);
    public abstract void ExitState(AIState state);
    public abstract void ForceTransition(AIState state);
    public abstract void SetWeaponStates();

    public UnitAIState AttachAIState(UnitAIState state)
    {
        state.parentAI = this;
        return state;
    }
}