using UnityEngine;

public class SepWander : MonoBehaviour
{
    Separation separation;
    DynamicWandering wander;
    DynamicMovement movement;

    void Start()
    {
        separation = GetComponent<Separation>();
        wander = GetComponent<DynamicWandering>();
        movement = GetComponent<DynamicMovement>();
    }

void Update()
{
    SteeringOutput separationSteering = separation.GetSteering();
    SteeringOutput wanderSteering = wander.GetSteering();

    SteeringOutput finalSteering;

    if (separationSteering.linearAcceleration.sqrMagnitude > 0f)
    {
        finalSteering = separationSteering;
    }
    else
    {
        wanderSteering.linearAcceleration = Vector3.ClampMagnitude(wanderSteering.linearAcceleration, movement.maxAcceleration * 0.5f);
        finalSteering = wanderSteering;
    }

    movement.Move(finalSteering);
}

}

