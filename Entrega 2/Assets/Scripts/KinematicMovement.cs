using UnityEngine;

public static class KinematicMovement
{
    public static LayerMask obstacleMask;

    public static void Move(Transform transform, Vector3 velocity, float speed, bool isScaled = false)
    {
        if (velocity.sqrMagnitude == 0f) return;

        Vector3 move = isScaled ? velocity : velocity.normalized * speed;
        Vector3 delta = move * Time.fixedDeltaTime;

        Rigidbody2D rb = transform.GetComponent<Rigidbody2D>();
        Collider2D col = transform.GetComponent<Collider2D>();

        if (rb != null && col != null)
        {
            RaycastHit2D[] hits = new RaycastHit2D[1];
            int count = col.Cast(delta.normalized, hits, delta.magnitude, true);

            if (count == 0)
            {
                rb.MovePosition(rb.position + (Vector2)delta);
            }
            else
            {
                Vector2 normal = hits[0].normal;
                Vector2 slideDir = Vector2.Perpendicular(normal);
                Vector2 projected = Vector2.Dot(move, slideDir) * slideDir;

                if (projected.magnitude > 0.01f)
                {
                    rb.MovePosition(rb.position + projected * Time.fixedDeltaTime);
                }
                else
                {
                    Vector2 escape = normal * 0.05f;
                    rb.MovePosition(rb.position + escape);
                }
            }
        }

        if (velocity.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }
}
