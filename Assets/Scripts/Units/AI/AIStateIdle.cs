using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateIdle : UnitAIState
{
    public override IEnumerator Run()
    {
        yield return new WaitForSeconds(5);
    }
}
