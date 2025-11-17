using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UnitAIState
{
    public UnitAI parentAI;

    public abstract IEnumerator Run();
    public virtual void ForceExit()
    {

    }
}
