using UnityEngine;

public class DynamicFlee : MonoBehaviour
{
    public Transform target;
    public Vector3 targetPosition;
    public bool useExplicitPosition = false;
    public float maxAcceleration = 10f;
    public float panicRadius = 4f;
    Vector3 direction;

    DynamicMovement movement;

    void Start()
    {
        movement = GetComponent<DynamicMovement>();
    }

    void Update()
    {
        SteeringOutput steering = new SteeringOutput();

        direction = useExplicitPosition
            ? TeletransportUtils.GetWrappedDirection(targetPosition, transform.position)
            : TeletransportUtils.GetWrappedDirection(target.position, transform.position);

        float distance = direction.magnitude;

        if (distance < panicRadius)
        {
            steering.linearAcceleration = direction.normalized * maxAcceleration;
        }
        else
        {
            steering.linearAcceleration = Vector3.zero;
            movement.linearVelocity = Vector3.zero;
            movement.angularVelocity = 0f;
        }

        steering.angularAcceleration = 0f;
        movement.Move(steering);
    }
}
