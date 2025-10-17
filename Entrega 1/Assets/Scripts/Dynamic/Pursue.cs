using UnityEngine;
public class Pursue : MonoBehaviour
{
    public Transform target;              // reference to player
    public float maxPrediction = 1.5f;    // max time to predict
    DynamicSeek seek;
    DynamicMovement movement;
    Vector3 direction;
    float distance, speed, prediction;

    void Start()
    {
        seek = GetComponent<DynamicSeek>();
        movement = GetComponent<DynamicMovement>();
    }

    void Update()
    {
        direction = target.position - transform.position;
        distance = direction.magnitude;
        speed = movement.linearVelocity.magnitude;

        prediction = (speed <= distance / maxPrediction) ? maxPrediction : distance / speed;

        Vector3 futurePosition = target.position + target.GetComponent<Player>().GetVelocity() * prediction;

        // delegate to seek
        seek.useExplicitPosition = true;
        seek.targetPosition = futurePosition;
        seek.enabled = true;

        // for debbugin
        Debug.DrawLine(transform.position, futurePosition, Color.cyan);
    }
}

