using UnityEngine;

public struct SteeringOutput
{
    public Vector3 linearAcceleration;
    public float angularAcceleration;
}

public class DynamicMovement : MonoBehaviour
{
    public Vector3 linearVelocity;
    public float angularVelocity;
    public float maxAcceleration;
    public float maxSpeed;

    public void Move(SteeringOutput steering)
    {
        // apply acceleration 
        linearVelocity += steering.linearAcceleration * Time.deltaTime;

        // if the speed is greater than the limit
        if (linearVelocity.magnitude > maxSpeed)
        {
            linearVelocity = linearVelocity.normalized * maxSpeed;
        }

        // apply angular acceleration
        angularVelocity += steering.angularAcceleration * Time.deltaTime;

        transform.position += linearVelocity * Time.deltaTime;
        transform.position = TeletransportUtils.GetWrappedPosition(transform.position);
        transform.Rotate(0f, 0f, angularVelocity * Time.deltaTime);
    }
    



}
