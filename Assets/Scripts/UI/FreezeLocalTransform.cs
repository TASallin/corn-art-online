using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreezeLocalTransform : MonoBehaviour
{
    Vector3 frozenPosition;
    // Start is called before the first frame update
    void Awake()
    {
        frozenPosition = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.Euler(0.0f, 0.0f, transform.parent.rotation.z * -1.0f);
        transform.position = transform.parent.position + frozenPosition * transform.parent.localScale.x;
    }

    public void SetFrozenPosition(Vector3 newPosition)
    {
        frozenPosition = newPosition;
        transform.position = transform.parent.position + frozenPosition;
    }

    public Vector3 GetFrozenPosition()
    {
        return frozenPosition;
    }
}
