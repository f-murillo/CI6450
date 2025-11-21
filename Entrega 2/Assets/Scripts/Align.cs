using UnityEngine;

public class Align : MonoBehaviour
{
    public float maxAngularAcceleration;
    public float maxRotation;
    public float targetRadius;
    public float slowRadius;
    public float timeToTarget = 0.1f;

    public float explicitOrientation;

    DynamicMovement movement;

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

        float characterOrientation = transform.eulerAngles.z;
        float rotation = MapToRange(explicitOrientation - characterOrientation);
        float rotationSize = Mathf.Abs(rotation);

        if (rotationSize < targetRadius)
        {
            steering.angularAcceleration = 0f;
            steering.linearAcceleration = Vector3.zero;
            movement.angularVelocity = 0f;
            movement.Move(steering);
            return;
        }

        float targetRotation = (rotationSize > slowRadius) ? maxRotation : maxRotation * (rotationSize / slowRadius);
        targetRotation *= rotation / rotationSize;

        steering.angularAcceleration = (targetRotation - movement.angularVelocity) / timeToTarget;

        if (Mathf.Abs(steering.angularAcceleration) > maxAngularAcceleration)
        {
            steering.angularAcceleration = Mathf.Sign(steering.angularAcceleration) * maxAngularAcceleration;
        }

        steering.linearAcceleration = Vector3.zero;
        movement.Move(steering);
    }
}
