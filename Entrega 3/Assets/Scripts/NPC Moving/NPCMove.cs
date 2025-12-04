using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NPCMove : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] float speed = 2f;
    [SerializeField] float rotationSpeed = 180f; // grados por segundo

    [Header("Avoidance")]
    [SerializeField] float avoidanceRadius = 0.7f;   // un poco más amplio
    [SerializeField] float avoidanceStrength = 1.5f;
    [SerializeField] LayerMask obstaclesMask;
    public LayerMask ObstaclesMask => obstaclesMask;

    private Rigidbody2D rb;
    private Vector3 previousAvoidance = Vector3.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Mueve el NPC hacia una dirección deseada, aplicando avoidance y rotación suave.
    /// </summary>
    public void MoveTowards(Vector3 targetPos)
    {
        Vector3 currentPos = transform.position;
        Vector3 toTarget = (targetPos - currentPos).normalized;

        // Calcular avoidance
        Vector3 avoidance = ComputeAvoidance(currentPos, toTarget);

        // Prioridad al avoidance si existe
        Vector3 finalDir = (avoidance != Vector3.zero) ? avoidance.normalized : toTarget;

        // Rotación suave en 2D
        float angle = Mathf.Atan2(finalDir.y, finalDir.x) * Mathf.Rad2Deg - 90f;
        Quaternion desiredRotation = Quaternion.Euler(0f, 0f, angle);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            desiredRotation,
            rotationSpeed * Time.deltaTime
        );

        // Movimiento lineal
        rb.MovePosition(rb.position + (Vector2)(finalDir * speed * Time.deltaTime));
    }

    private Vector3 ComputeAvoidance(Vector3 origin, Vector3 forwardDir)
    {
        Vector3 avoidance = Vector3.zero;

        // 🔹 Raycast frontal
        RaycastHit2D hit = Physics2D.Raycast(origin, forwardDir, avoidanceRadius, obstaclesMask);
        if (hit.collider != null)
        {
            Vector3 away = origin - (Vector3)hit.point;
            avoidance = away.normalized * avoidanceStrength;

            // Si está demasiado cerca, empuje fuerte hacia atrás
            if (hit.distance < 0.2f)
                avoidance = -forwardDir * avoidanceStrength * 2f;
        }

        // 🔹 Rayos laterales para esquivar esquinas
        RaycastHit2D leftHit = Physics2D.Raycast(origin, Quaternion.Euler(0, 0, 30) * forwardDir, avoidanceRadius, obstaclesMask);
        RaycastHit2D rightHit = Physics2D.Raycast(origin, Quaternion.Euler(0, 0, -30) * forwardDir, avoidanceRadius, obstaclesMask);

        if (leftHit.collider != null && rightHit.collider == null) {
            avoidance += (Vector3)(Quaternion.Euler(0, 0, -90) * forwardDir) * avoidanceStrength;
        }
        else if (rightHit.collider != null && leftHit.collider == null) {
            avoidance += (Vector3)(Quaternion.Euler(0, 0, 90) * forwardDir) * avoidanceStrength;
        }

        // 🔹 Suavizar cambios bruscos
        avoidance = Vector3.Lerp(previousAvoidance, avoidance, 0.25f);
        previousAvoidance = avoidance;

        return avoidance;
    }
}
