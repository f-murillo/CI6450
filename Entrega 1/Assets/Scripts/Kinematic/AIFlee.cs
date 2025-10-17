using UnityEngine;

public class AIFlee : MonoBehaviour
{
    public Transform target;
    public Vector3 targetPosition;
    public bool useExplicitPosition = false;
    public float maxSpeed;
    public float panicRadius;
    Vector3 velocity;

    void Update()
    {
        Vector3 fleeVector = useExplicitPosition
            ? TeletransportUtils.GetWrappedDirection(targetPosition, transform.position)
            : TeletransportUtils.GetWrappedDirection(target.position, transform.position);

        float distance = fleeVector.magnitude;

        velocity = distance < panicRadius ? fleeVector : Vector3.zero;

        KinematicMovement.Move(transform, velocity, maxSpeed);
    }
}
