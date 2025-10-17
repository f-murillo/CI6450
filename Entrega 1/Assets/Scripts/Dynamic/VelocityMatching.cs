using UnityEngine;

public class VelocityMatch : MonoBehaviour
{
    public Transform target;               // reference to player
    public float maxAcceleration;    
    public float timeToTarget = 0.1f;      

    DynamicMovement movement;
    Player player;

    void Start()
    {
        movement = GetComponent<DynamicMovement>();
        player = target.GetComponent<Player>();
    }

    void Update()
    {
        SteeringOutput steering = new SteeringOutput();

        // player velocity 
        Vector3 targetVelocity = player.GetVelocity();

        steering.linearAcceleration = (targetVelocity - movement.linearVelocity) / timeToTarget;

        if (steering.linearAcceleration.magnitude > maxAcceleration)
        {
            steering.linearAcceleration = steering.linearAcceleration.normalized * maxAcceleration;
        }

        steering.angularAcceleration = 0f;

        // finally, we apply the movement
        movement.Move(steering);
    }
}

