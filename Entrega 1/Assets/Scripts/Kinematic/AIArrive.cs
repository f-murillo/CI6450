using UnityEngine;
public class AIArrive : MonoBehaviour
{
    public Transform target; // reference to player
    public float maxSpeed, radius, timeToTarget;            
    Vector3 velocity;
    void Update()
    {
        // Kinematic Arrive
        velocity = TeletransportUtils.GetWrappedDirection(transform.position, target.position);

        // Velocity to get in timeToTarget seconds
        velocity /= timeToTarget;

        // If the npc gets inside of radius, it stops
        if (velocity.magnitude < radius)
            velocity = Vector3.zero;

        // If it goes too fast, we scale it
        if (velocity.magnitude > maxSpeed)
            velocity = velocity.normalized * maxSpeed;

        
    
        KinematicMovement.Move(transform, velocity, maxSpeed, true);
    }
}
