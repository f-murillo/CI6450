using UnityEngine;

public class DynamicWandering : MonoBehaviour
{
    public float wanderOffset = 2f;       // distance in front of the NPC
    public float wanderRadius = 1f;       // wander circle radius
    public float wanderRate = 20f;        // max orientation change per frame (degrees)
    public float maxAcceleration = 8f;
    float wanderOrientation;
    Vector3 circleCenter;
    Vector3 targetPosition;
    Vector3 accelerationDirection;

    DynamicMovement movement;
    Face face;
    Transform wanderTarget;

    // To generate a value between -1 and 1
    float RandomBinomial()
    {
        return Random.value - Random.value;
    }

    // To convert an orientation (in degrees) into a unit vector
    Vector3 OrientationToVector(float orientationDegrees)
    {
        float radians = orientationDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
    }

    // To normalize angles into [0, 360] range
    float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    public SteeringOutput GetSteering()
    {
        float characterOrientation = transform.eulerAngles.z;
        Vector3 accelerationDirection = OrientationToVector(characterOrientation);

        return new SteeringOutput
        {
            linearAcceleration = accelerationDirection * maxAcceleration,
            angularAcceleration = 0f
        };
    }

    void Start()
    {
        movement = GetComponent<DynamicMovement>();
        face = GetComponent<Face>();

        // Empty object (to use as imaginary target) 
        wanderTarget = new GameObject("WanderTarget").transform;
        face.target = wanderTarget;

        // Starting orientation
        wanderOrientation = transform.eulerAngles.z + 90f; // + 90 to point in the direction of sprite 
    }

    void Update()
    {
        // first we acumulate random variation
        wanderOrientation += RandomBinomial() * wanderRate;
        wanderOrientation = NormalizeAngle(wanderOrientation);

        // next, we calculate the circle center towards wanderOrientation direction
        Vector3 wanderDir = OrientationToVector(wanderOrientation);
        circleCenter = transform.position + wanderDir * wanderOffset;

        // now, we calculate the target position over the circle
        targetPosition = circleCenter + wanderDir * wanderRadius;

        // update the imaginary target position, and delegate to face
        wanderTarget.position = targetPosition;
        face.enabled = true; 

        // apply acceleration to towards the target
        accelerationDirection = (targetPosition - transform.position).normalized;
        SteeringOutput steering = new SteeringOutput
        {
            linearAcceleration = accelerationDirection * maxAcceleration,
            angularAcceleration = 0f
        };

        // finally, apply the move
        movement.Move(steering);

        // for debuggin
        Debug.DrawLine(transform.position, targetPosition, Color.magenta);
    }
}
