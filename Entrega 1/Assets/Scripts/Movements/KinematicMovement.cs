using UnityEngine;
public static class KinematicMovement
{
    // This function will be called from de player and the NPC to move 
    public static void Move(Transform transform, Vector3 velocity, float speed, bool isScaled = false)
    {
        if (velocity.sqrMagnitude > 0.0f)
        {
            Vector3 move = isScaled ? velocity : velocity.normalized * speed; // If the function is called from AIArrive, velocity is already scaled
            transform.position += move * Time.deltaTime;
            transform.position = TeletransportUtils.GetWrappedPosition(transform.position);
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f); // -90° because the sprites are pointing up by default
        }
    }
}