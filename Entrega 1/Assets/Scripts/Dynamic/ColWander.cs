using UnityEngine;

public class ColWander : MonoBehaviour
{
    CollisionAvoidance avoidance;
    DynamicWandering wander;
    DynamicMovement movement;

    void Start()
    {
        avoidance = GetComponent<CollisionAvoidance>();
        wander = GetComponent<DynamicWandering>();
        movement = GetComponent<DynamicMovement>();
    }

    void Update()
    {
        SteeringOutput avoidSteering = avoidance.GetSteering();
        SteeringOutput wanderSteering = wander.GetSteering();

        if (avoidSteering.linearAcceleration == Vector3.zero)
        {
            // No threats, wander takes full control
            movement.Move(wanderSteering);
        }
        else
        {
            // Threats, collision avoidance takes control
            movement.Move(avoidSteering);
        }
    }
}
