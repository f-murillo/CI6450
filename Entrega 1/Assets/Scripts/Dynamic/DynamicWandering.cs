using UnityEngine;

public class DynamicWandering : MonoBehaviour
{
    public float wanderOffset = 2f;       // distancia frente al NPC
    public float wanderRadius = 1f;       // radio del círculo de wander
    public float wanderRate = 20f;        // cambio máximo de orientación por frame (grados)
    public float maxAcceleration = 8f;
    float wanderOrientation;
    Vector3 circleCenter;
    Vector3 targetPosition;
    Vector3 accelerationDirection;

    DynamicMovement movement;
    Face face;
    Transform wanderTarget;

    // Genera una variación aleatoria entre -1 y 1
    float RandomBinomial()
    {
        return Random.value - Random.value;
    }

    // Convierte orientación en grados a vector unitario
    Vector3 OrientationToVector(float orientationDegrees)
    {
        float radians = orientationDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
    }

    // Normaliza ángulos al rango [0°, 360°]
    float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    public SteeringOutput GetSteering()
    {
        float characterOrientation = transform.eulerAngles.z;
        Vector3 accelerationDirection = OrientationToVector(characterOrientation);

        return new SteeringOutput
        {
            linearAcceleration = accelerationDirection * maxAcceleration,
            angularAcceleration = 0f
        };
    }


    void Start()
    {
        movement = GetComponent<DynamicMovement>();
        face = GetComponent<Face>();

        // Crea objeto vacío como target imaginario
        wanderTarget = new GameObject("WanderTarget").transform;
        face.target = wanderTarget;

        // Inicializa orientación absoluta
        wanderOrientation = transform.eulerAngles.z + 90f; // para que apunte en direccion del sprite 
    }

    void Update()
    {
        // Acumula variación aleatoria cada frame
        wanderOrientation += RandomBinomial() * wanderRate;
        wanderOrientation = NormalizeAngle(wanderOrientation);

        // Calcula centro del círculo en dirección de wanderOrientation
        Vector3 wanderDir = OrientationToVector(wanderOrientation);
        circleCenter = transform.position + wanderDir * wanderOffset;

        // Calcula posición del target sobre el círculo
        targetPosition = circleCenter + wanderDir * wanderRadius;

        // Actualiza posición del target imaginario
        wanderTarget.position = targetPosition;
        face.enabled = true;

        // Aceleración hacia el target
        accelerationDirection = (targetPosition - transform.position).normalized;
        SteeringOutput steering = new SteeringOutput
        {
            linearAcceleration = accelerationDirection * maxAcceleration,
            angularAcceleration = 0f
        };

        movement.Move(steering);

        // Visualización para depuración
        Debug.DrawLine(transform.position, targetPosition, Color.magenta);
    }
}
