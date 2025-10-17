using UnityEngine;

public class Evade : MonoBehaviour
{
    public Transform target;              // reference to player
    public float maxPrediction = 1.5f;    // max time to predict
    DynamicFlee flee;
    DynamicMovement movement;
    Vector3 direction;
    float distance, speed, prediction;

    void Start()
    {
        flee = GetComponent<DynamicFlee>();
        movement = GetComponent<DynamicMovement>();
    }

    void Update()
    {
        direction = target.position - transform.position;
        distance = direction.magnitude;
        speed = movement.linearVelocity.magnitude;

        prediction = (speed <= distance / maxPrediction) ? maxPrediction : distance / speed;

        // reduce prediction if player es too close
        if (distance < flee.panicRadius * 0.5f)
        {
            prediction *= 0.5f;
        }

        Vector3 playerVelocity = target.GetComponent<Player>().GetVelocity();
        Vector3 futurePosition = target.position + target.GetComponent<Player>().GetVelocity() * prediction;

        // verify if prediction if alined with players movement
        Vector3 toFuture = futurePosition - target.position;
        if (Vector3.Dot(playerVelocity.normalized, toFuture.normalized) < 0.5f)
        {
            // if not, use current position
            futurePosition = target.position;
        }

        // Delegate to Flee
        flee.useExplicitPosition = true;
        flee.targetPosition = futurePosition;
        flee.enabled = true;

        // for debbuging
        Debug.DrawLine(transform.position, futurePosition, Color.cyan);
    }
}
