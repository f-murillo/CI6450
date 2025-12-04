using UnityEngine;

public class DynamicArrive : MonoBehaviour
{
    public Transform target;             
    public Vector3 targetPosition;
    public bool useExplicitPosition = false;

    public float maxAcceleration;
    public float maxSpeed;
    public float targetRadius;
    public float slowRadius;
    public float timeToTarget = 0.1f;

    Vector3 direction;
    Vector3 targetVelocity;
    float distance;
    float targetSpeed;

    DynamicMovement movement;

    void Start()
    {
        movement = GetComponent<DynamicMovement>();
    }

    void Update()
    {
        SteeringOutput steering = new SteeringOutput();

        Vector3 chosenTarget = useExplicitPosition ? targetPosition : target.position;

        direction = chosenTarget - transform.position;
        distance = direction.magnitude;

        if (distance < targetRadius)
        {
            steering.linearAcceleration = Vector3.zero;
            steering.angularAcceleration = 0f;
            movement.linearVelocity = Vector3.zero;
            movement.angularVelocity = 0f;
        }
        else
        {
            targetSpeed = (distance > slowRadius) ? maxSpeed : maxSpeed * (distance / slowRadius);
            targetVelocity = direction.normalized * targetSpeed;

            steering.linearAcceleration = (targetVelocity - movement.linearVelocity) / timeToTarget;

            if (steering.linearAcceleration.magnitude > maxAcceleration)
            {
                steering.linearAcceleration = steering.linearAcceleration.normalized * maxAcceleration;
            }

            steering.angularAcceleration = 0f;
        }

        movement.Move(steering);
    }
}
