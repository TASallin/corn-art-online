using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomingProjectile : MonoBehaviour
{
    public float movementSpeed;
    public float turnSpeed;
    public float acceleration;
    public Unit target;
    Vector2 velocity;
    
    // Start is called before the first frame update
    void Start()
    {
        velocity = Vector2.zero;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 targetPosition = transform.position + transform.right;
        if (target != null)
        {
            targetPosition = target.transform.position;
        }
        Vector3 rotateDir = Vector3.RotateTowards(transform.right, targetPosition - transform.position, turnSpeed * Time.deltaTime, 0f);
        transform.rotation = Quaternion.Euler(0, 0, -1 * Vector2.SignedAngle(Linalg.Vector3ToVector2(rotateDir), Vector2.right));
        Vector2 targetVelocity = Linalg.Vector3ToVector2(transform.right) * movementSpeed;
        float maxDeltaV = acceleration * Time.deltaTime;
        if ((targetVelocity - velocity).magnitude <= maxDeltaV)
        {
            velocity = targetVelocity;
        } else
        {
            velocity = velocity + (targetVelocity - velocity).normalized * maxDeltaV;
        }
        transform.Translate(velocity * Time.deltaTime, Space.World);
    }
}
