using UnityEngine;

public class KinematicSeek : MonoBehaviour
{
    public Transform target;              // referencia opcional
    public Vector3 targetPosition;        // posición explícita (ej. waypoint)
    public bool useExplicitPosition = false;
    public float maxSpeed = 2f;

    private Vector3 velocity;

    void FixedUpdate() // usamos FixedUpdate porque KinematicMovement usa física
    {
        if (target == null && !useExplicitPosition) return;

        // Elegimos el objetivo según el flag
        Vector3 chosenTarget = useExplicitPosition ? targetPosition : target.position;

        // Dirección hacia el objetivo
        velocity = chosenTarget - transform.position;

        // Llamamos a KinematicMovement para aplicar el movimiento
        KinematicMovement.Move(transform, velocity, maxSpeed);
        Debug.Log("NPC aplicando movimiento");
    }
}
