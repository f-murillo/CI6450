using UnityEngine;
public class AIWanderer : MonoBehaviour
{
    public float maxSpeed, maxRotation;
    float orientation = 90f;
    Vector3 direction, velocity;

    float RandomBinomial()
    {
        return Random.value - Random.value;
    }

    void Update()
    {
        float orientationRad = orientation * Mathf.Deg2Rad; // To calculate cos and sin and get the x and y components

        // Velocity based on the orientation
        direction = new Vector3(Mathf.Cos(orientationRad), Mathf.Sin(orientationRad), 0f);
        velocity = direction * maxSpeed;

        transform.position += velocity * Time.deltaTime;
        transform.position = TeletransportUtils.GetWrappedPosition(transform.position);
        transform.rotation = Quaternion.Euler(0f, 0f, orientation-90f);

        // Random change of orientation
        orientation += RandomBinomial() * maxRotation * Time.deltaTime;
    }
}