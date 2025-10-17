using UnityEngine;

public class Align : MonoBehaviour
{
    public Transform target;                  // reference to player
    public float maxAngularAcceleration;      
    public float maxRotation;                 
    public float targetRadius;                // internal radius 
    public float slowRadius;                  // external radius
    public float timeToTarget = 0.1f;         

    public bool useExplicitOrientation = false; // to be use by other algorithms
    public float explicitOrientation;

    DynamicMovement movement;

    // To map an angle in degrees to [-180, 180] range 
    float MapToRange(float angle)
    {
        angle = (angle + 180f) % 360f;
        if (angle < 0f) angle += 360f;
        return angle - 180f;
    }

    void Start()
    {
        movement = GetComponent<DynamicMovement>();
    }

    void Update()
    {
        SteeringOutput steering = new SteeringOutput();

        // current orientation of npc
        float characterOrientation = transform.eulerAngles.z;

        float targetOrientation = useExplicitOrientation ? explicitOrientation : target.eulerAngles.z;

        // difference of orientation and maping to [-180, 180] range
        float rotation = MapToRange(targetOrientation - characterOrientation);
        float rotationSize = Mathf.Abs(rotation);

        // if player gets inside internal radius, stop
        if (rotationSize < targetRadius)
        {
            steering.angularAcceleration = 0f;
            steering.linearAcceleration = Vector3.zero;
            movement.angularVelocity = 0f;
            movement.Move(steering);
            return;
        }

        // angular velocity
        float targetRotation = (rotationSize > slowRadius) ? maxRotation : maxRotation * (rotationSize / slowRadius);
        targetRotation *= rotation / rotationSize;

        // angular acceleration
        steering.angularAcceleration = (targetRotation - movement.angularVelocity) / timeToTarget;

        // if angular acceleration exceeds max
        float angularAccel = Mathf.Abs(steering.angularAcceleration);
        if (angularAccel > maxAngularAcceleration)
        {
            steering.angularAcceleration = Mathf.Sign(steering.angularAcceleration) * maxAngularAcceleration;
        }

        steering.linearAcceleration = Vector3.zero;

        // finally, we apply the movement
        movement.Move(steering);
    }
}
