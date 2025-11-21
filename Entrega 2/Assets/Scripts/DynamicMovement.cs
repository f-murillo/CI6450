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

    public LayerMask obstacleMask;

    public void Move(SteeringOutput steering)
    {
        // Aplicar aceleración lineal
        linearVelocity += steering.linearAcceleration * Time.deltaTime;

        // Limitar velocidad
        if (linearVelocity.magnitude > maxSpeed)
        {
            linearVelocity = linearVelocity.normalized * maxSpeed;
        }

        // Aplicar aceleración angular
        angularVelocity += steering.angularAcceleration * Time.deltaTime;

        // Movimiento con detección de colisión
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Collider2D col = GetComponent<Collider2D>();

        if (rb != null && col != null)
        {
            Vector3 delta = linearVelocity * Time.deltaTime;
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
                Vector2 projected = Vector2.Dot(linearVelocity, slideDir) * slideDir;
                rb.MovePosition(rb.position + projected.normalized * maxSpeed * Time.deltaTime);
            }
        }
        else
        {
            // Fallback sin físicas
            transform.position += linearVelocity * Time.deltaTime;
        }

        // Rotación hacia la dirección de movimiento
        if (linearVelocity.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(linearVelocity.y, linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
        else
        {
            transform.Rotate(0f, 0f, angularVelocity * Time.deltaTime);
        }
    }
}
