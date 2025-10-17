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
            // No hay amenaza → wander toma control completo
            movement.Move(wanderSteering);
        }
        else
        {
            // Hay amenaza → evasión toma control
            movement.Move(avoidSteering);
        }
    }
}
