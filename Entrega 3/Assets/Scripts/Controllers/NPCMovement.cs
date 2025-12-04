using UnityEngine;

public class NPCMovement : MonoBehaviour
{
    public float speed = 3f;
    public bool usePhysics = true;
    public bool rotateToDirection = true;
    public LayerMask obstacleMask;

    Rigidbody2D rb;
    Collider2D col;

    void Awake()
    {
        if (usePhysics)
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
        }
    }

    public void MoveTowards(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        Vector3 delta = dir * speed * Time.deltaTime;

        if (usePhysics && rb != null && col != null)
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
                Vector2 projected = Vector2.Dot(dir, slideDir) * slideDir;

                if (projected.magnitude > 0.01f)
                {
                    Vector2 slide = projected.normalized * speed * Time.deltaTime;
                    rb.MovePosition(rb.position + slide);
                }
                else
                {
                    Vector2 escape = normal * 0.05f;
                    rb.MovePosition(rb.position + escape);
                }
            }
        }
        else
        {
            transform.position += delta;
        }

        if (rotateToDirection && dir.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }
}
