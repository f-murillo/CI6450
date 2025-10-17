using UnityEngine;
public class AISeek : MonoBehaviour
{
    public Transform target;  // reference to player
    public Vector3 targetPosition;
    public bool useExplicitPosition = false; // to be used by Pursue
    public float maxSpeed; 
    Vector3 velocity;
    void Update()
    {
        // Kinematic Seek
        velocity = useExplicitPosition 
        ? TeletransportUtils.GetWrappedDirection(transform.position, targetPosition) 
        : TeletransportUtils.GetWrappedDirection(transform.position, target.position);
        KinematicMovement.Move(transform, velocity, maxSpeed);

        //transform.position = TeletransportUtils.GetWrappedPosition(transform.position);
    }
}
