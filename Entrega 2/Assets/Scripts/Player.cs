using UnityEngine;
public class Player : MonoBehaviour
{
    public float moveSpeed;
    float speedX, speedY;
    Vector3 velocity;

    public Vector3 GetVelocity()
    {
        return velocity;
    }
    void FixedUpdate()
    {
        speedX = Input.GetAxis("Horizontal");
        speedY = Input.GetAxis("Vertical");
        velocity = new Vector3(speedX, speedY, 0f) * moveSpeed;
        KinematicMovement.Move(transform, velocity, moveSpeed);
    }


}