using UnityEngine;

public class DynamicSeek : MonoBehaviour
{
    public Transform target;              // referencia al objetivo (ej. jugador o waypoint)
    public Vector3 targetPosition;        // posición explícita (ej. punto del pathfinding)
    public bool useExplicitPosition = false;
    public float maxAcceleration = 5f;

    private DynamicMovement movement;

    void Start()
    {
        movement = GetComponent<DynamicMovement>();
    }

    void Update()
    {
        SteeringOutput steering = new SteeringOutput();

        // Calculamos dirección sin envolvimiento
        Vector3 direction = useExplicitPosition 
            ? (targetPosition - transform.position) 
            : (target.position - transform.position);

        // Normalizamos y aplicamos aceleración máxima
        steering.linearAcceleration = direction.normalized * maxAcceleration;

        // No aplicamos rotación automática aquí
        steering.angularAcceleration = 0f;

        // Aplicamos el movimiento
        movement.Move(steering);
    }
}
