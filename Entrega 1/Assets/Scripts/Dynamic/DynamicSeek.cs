using UnityEngine;

public class DynamicSeek : MonoBehaviour
{
    public Transform target;  // reference to player
    public Vector3 targetPosition;
    public bool useExplicitPosition = false; // this to be used by Pursue
    public float maxAcceleration;
    Vector3 direction;
    DynamicMovement movement;
    

    void Start()
    {
        movement = GetComponent<DynamicMovement>();
    }

    void Update()
    {
        // Dynamic Seek
        SteeringOutput steering = new SteeringOutput();

        direction = useExplicitPosition 
        ? TeletransportUtils.GetWrappedDirection(transform.position, targetPosition) 
        : TeletransportUtils.GetWrappedDirection(transform.position, target.position);

        steering.linearAcceleration = direction.normalized * maxAcceleration;

        steering.angularAcceleration = 0f;

        // finally, we apply the movement
        movement.Move(steering);
    }
}