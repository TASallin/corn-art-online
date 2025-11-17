using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitVelocityManager : MonoBehaviour
{
    public Rigidbody2D rb;
    // Base values for reference/scaling purposes
    private const float BASE_MOVEMENT_SPEED = 5f;
    private const float BASE_ACCELERATION = 10f;
    private const float BASE_TURN_SPEED = 80f;

    // Calculated speeds
    public float movementSpeed;
    public float acceleration;
    public float turnSpeed;

    // Min/max values to prevent extreme behavior
    private const float MIN_MOVEMENT_SPEED = 2f;
    private const float MAX_MOVEMENT_SPEED = 12f;
    private const float MIN_ACCELERATION = 5f;
    private const float MAX_ACCELERATION = 25f;
    private const float MIN_TURN_SPEED = 40f;
    private const float MAX_TURN_SPEED = 160f;
    public float stunTime;
    public Vector2 targetPosition;
    public static readonly float maxBrakeMultiplier = 0.4f;
    public static readonly float flipInterval = 0.4f;
    public static readonly float flipMaxAngle = 5f;
    float flipCountdown;
    bool flipped;
    public bool asleep;
    public bool noBrakes;
    public bool frozen;

    // Start is called before the first frame update
    void Start()
    {
        asleep = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (frozen)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        if (stunTime > 0)
        {
            stunTime -= Time.deltaTime;
            return;
        }
        if (asleep)
        {
            return;
        }
        if (flipCountdown > 0)
        {
            flipCountdown -= Time.deltaTime;
        }
        //Quaternion currentRotation = transform.rotation;
        //transform.LookAt(targetPosition);
        //Quaternion targetRotation = transform.rotation;
        //transform.rotation = Quaternion.RotateTowards(currentRotation, targetRotation, turnSpeed * Time.deltaTime);
        if (flipCountdown <= 0)
        {
            if (targetPosition.x < transform.position.x && !flipped && System.Math.Abs(Vector2.Angle(targetPosition - Linalg.Vector3ToVector2(transform.position), Vector2.right) - 90) > flipMaxAngle)
            {
                flipped = true;
                flipCountdown = flipInterval;
            } else if (transform.position.x < targetPosition.x && flipped && System.Math.Abs(Vector2.Angle(targetPosition - Linalg.Vector3ToVector2(transform.position), Vector2.right) - 90) > flipMaxAngle)
            {
                flipped = false;
                flipCountdown = flipInterval;
            }
        }
        float targetAngle = -1 * Vector2.SignedAngle(targetPosition - Linalg.Vector3ToVector2(transform.position), Vector2.right);
        float currentAngle = transform.rotation.eulerAngles.z;
        if (flipped)
        {
            //transform.rotation = Quaternion.Euler(0, 180, transform.rotation.eulerAngles.z);
            //currentAngle = 180 - currentAngle;
            targetAngle = 180 - targetAngle;
        } else
        {
            //transform.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z);
        }
        if (currentAngle > 180)
        {
            currentAngle = -360 + currentAngle;
        }
        if (targetAngle > 180)
        {
            targetAngle = -360 + targetAngle;
        }
        float maxRotation = turnSpeed * Time.deltaTime;
        
        if (System.Math.Abs(targetAngle - currentAngle) % 360 > maxRotation)
        {
            if (targetAngle > currentAngle)
            {
                targetAngle = currentAngle + maxRotation;
            } else
            {
                targetAngle = currentAngle - maxRotation;
            }
        }
        
        if (flipped)
        {
            //transform.rotation = Quaternion.Euler(0, 180, 0);
            //transform.rotation = Quaternion.Euler(0, 0, targetAngle);
            //transform.RotateAround(transform.position, Vector3.forward, 180 + targetAngle);
            transform.rotation = Quaternion.AngleAxis(180, Vector3.up) * Quaternion.AngleAxis(targetAngle, Vector3.forward);
            //Debug.Log(targetAngle);
        } else
        {
            transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        }
        float brakeMultiplier;
        if (noBrakes)
        {
            brakeMultiplier = 0;
        } else
        {
            brakeMultiplier = maxBrakeMultiplier;
        }
        Vector2 currentVelocity = rb.velocity;
        Vector2 targetVelocity = Linalg.Vector3ToVector2(transform.right) * movementSpeed;
        Vector2 currentPosition = Linalg.Vector3ToVector2(transform.position);
        if ((targetPosition - currentPosition).magnitude < brakeMultiplier * movementSpeed)
        {
            targetVelocity = targetVelocity * ((targetPosition - currentPosition).magnitude / (brakeMultiplier * movementSpeed));
        }
        float maxDeltaV = acceleration * Time.deltaTime;
        if ((targetVelocity - currentVelocity).magnitude <= maxDeltaV)
        {
            rb.velocity = targetVelocity;
        } else
        {
            rb.velocity = currentVelocity + (targetVelocity - currentVelocity).normalized * maxDeltaV;
        }
    }

    public bool ReachedDestination(float leniency = 0.1f)
    {
        if (targetPosition == null)
        {
            return true;
        }
        Vector2 currentPosition = new Vector2(transform.position.x, transform.position.y);
        return ((targetPosition - currentPosition).magnitude <= leniency);
    }

    public void SetTarget(Vector2 target)
    {
        targetPosition = target;
        asleep = false;
    }

    public void ClearTarget()
    {
        asleep = true;
    }

    public void AddStun(float stunDuration)
    {
        stunTime = System.Math.Max(stunTime, stunDuration);
    }

    public void SetMovementParameters(int speedStat, MovementType movementType)
    {
        // Normalize speed stat for calculations (0 to 1 range where 5->0 and 40->1)
        float normalizedSpeed = Mathf.Clamp01((speedStat - 5f) / 35f);

        // Calculate base values based on speed stat
        float baseMovement = Mathf.Lerp(BASE_MOVEMENT_SPEED * 0.8f, BASE_MOVEMENT_SPEED * 1.8f, normalizedSpeed);
        float baseTurn = Mathf.Lerp(BASE_TURN_SPEED * 0.7f, BASE_TURN_SPEED * 1.3f, normalizedSpeed);

        // Apply movement type modifiers
        switch (movementType)
        {
            case MovementType.Infantry:
                // Infantry: Slow movement (0.7x), fast turns (1.3x)
                movementSpeed = baseMovement * 0.7f;
                turnSpeed = baseTurn * 1.3f;
                break;

            case MovementType.Cavalry:
                // Cavalry: Fast movement (1.4x), slow turns (0.7x)
                movementSpeed = baseMovement * 1.4f;
                turnSpeed = baseTurn * 0.7f;
                break;

            case MovementType.Flier:
                // Flier: Fast at both (1.2x for both)
                movementSpeed = baseMovement * 1.2f;
                turnSpeed = baseTurn * 1.2f;
                break;

            default:
                // Fallback to balanced values
                movementSpeed = baseMovement;
                turnSpeed = baseTurn;
                break;
        }

        // Set acceleration proportional to movement speed
        acceleration = movementSpeed * 2f;  // Acceleration at 2x movement speed

        // Apply final clamping to prevent extreme values
        movementSpeed = Mathf.Clamp(movementSpeed, MIN_MOVEMENT_SPEED, MAX_MOVEMENT_SPEED);
        acceleration = Mathf.Clamp(acceleration, MIN_ACCELERATION, MAX_ACCELERATION);
        turnSpeed = Mathf.Clamp(turnSpeed, MIN_TURN_SPEED, MAX_TURN_SPEED);

        Debug.Log($"Set movement params for {movementType}: Speed={movementSpeed:F2}, Accel={acceleration:F2}, Turn={turnSpeed:F2}");
    }

    // You might want to call this when the unit's class changes
    public void UpdateMovementBasedOnClass(Unit unit)
    {
        if (unit.unitClass != null)
        {
            SetMovementParameters(unit.speed, unit.unitClass.movementType);
        }
    }
}
