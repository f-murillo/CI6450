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
    void Update()
    {
        speedX = Input.GetAxis("Horizontal"); // A, D, arrowLeft, arrowRight
        speedY = Input.GetAxis("Vertical"); // W, S, arrowUp, arrowDown

        velocity = new Vector3(speedX, speedY, 0f) * moveSpeed;
        KinematicMovement.Move(transform, velocity, moveSpeed);
    }
}
