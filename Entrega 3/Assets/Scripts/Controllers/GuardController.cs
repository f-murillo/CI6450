using UnityEngine;

public enum GuardState { Patrol, Chase, Attack, Return, Flee }

public class GuardController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public NPCPathFollower patrol;
    public NPCPathController pathController;
    public LayerMask obstacleMask;

    [Header("Ataque")]
    public float attackRange = 3f;
    public float attackCooldown = 1.5f;
    private float attackTimer = 0f;

    [Header("Estado de salud")]
    public int guardHealth = 10;
    [Header("Huida")]
    public float fleeSpeed = 3f;

    [Header("Sensores")]
    public float visionRange = 10f;
    public float visionAngle = 60f;
    public float lostSightDuration = 2f;

    private GuardState currentState = GuardState.Patrol;
    private GuardState previousState = GuardState.Patrol;
    private bool playerDetected = false;
    private float lostSightTimer = 0f;
    private Rigidbody2D rb;

    private ProjectileShooter shooter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (pathController == null)
            pathController = GetComponent<NPCPathController>();

        shooter = GetComponent<ProjectileShooter>();
        if (shooter == null)
            Debug.LogError("GuardController: No se encontró ProjectileShooter en " + gameObject.name);

        pathController.enabled = false;
    }

    void Update()
    {
        switch (currentState)
        {
            case GuardState.Patrol:
                patrol.enabled = true;
                pathController.enabled = false;
                break;

            case GuardState.Chase:
                patrol.enabled = false;
                pathController.enabled = true;

                if (previousState != GuardState.Chase)
                {
                    Debug.Log("TRANSICIÓN: Guard entra en CHASE");
                    pathController.SetTarget(player);
                }

                pathController.TryRecalculatePath(1f);

                float distToPlayer = Vector3.Distance(transform.position, player.position);
                if (distToPlayer <= attackRange)
                {
                    Debug.Log("TRANSICIÓN: Guard cambia a ATTACK (distancia = " + distToPlayer + ")");
                    currentState = GuardState.Attack;
                    attackTimer = 0f;
                }
                break;

            case GuardState.Attack:
                patrol.enabled = false;
                pathController.enabled = false;

                Vector3 dirToPlayer = (player.position - transform.position).normalized;
                transform.up = dirToPlayer;

                if (attackTimer <= 0f)
                {
                    ShootAtPlayer();
                    attackTimer = attackCooldown;
                }
                else
                {
                    attackTimer -= Time.deltaTime;
                }

                if (Vector3.Distance(transform.position, player.position) > attackRange + 1f)
                {
                    Debug.Log("TRANSICIÓN: Jugador se aleja, vuelve a CHASE");
                    currentState = GuardState.Chase;
                }
                break;

            case GuardState.Flee:
                patrol.enabled = false;
                pathController.enabled = false;

                Vector2 fleeDir = (transform.position - player.position).normalized;
                transform.up = fleeDir;

                Vector2 newPos = rb.position + fleeDir * fleeSpeed * Time.deltaTime;
                rb.MovePosition(newPos);
                break;

            case GuardState.Return:
                patrol.enabled = true;
                pathController.enabled = false;

                Debug.Log("TRANSICIÓN: Guard en RETURN, retomando patrulla");
                patrol.ResumeFromClosestPoint(transform.position);
                currentState = GuardState.Patrol;
                break;
        }

        EvaluateTransitions();
        previousState = currentState;
    }

    void EvaluateTransitions()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, player.position);
        float angle = Vector3.Angle(transform.up, dirToPlayer);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, dist, obstacleMask);
        bool seesPlayer = dist < visionRange &&
                          angle < visionAngle / 2f &&
                          hit.collider == null;

        if (seesPlayer)
        {
            if (!playerDetected)
                Debug.Log("DETECCIÓN: Guardia ve al jugador");

            playerDetected = true;
            lostSightTimer = 0f;
        }
        else if (playerDetected)
        {
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer >= lostSightDuration)
            {
                Debug.Log("PERDIDA: Guardia pierde de vista al jugador");
                playerDetected = false;
            }
        }

        switch (currentState)
        {
            case GuardState.Patrol:
                if (playerDetected)
                {
                    Debug.Log("TRANSICIÓN: De PATROL a CHASE");
                    currentState = GuardState.Chase;
                }
                break;

            case GuardState.Chase:
                if (!playerDetected)
                {
                    Debug.Log("TRANSICIÓN: De CHASE a RETURN");
                    currentState = GuardState.Return;
                }
                break;
        }
    }

    void ShootAtPlayer()
    {
        if (shooter == null || player == null)
            return;

        Vector2 dir = (player.position - shooter.firePoint.position).normalized;
        shooter.Shoot(dir, gameObject);
    }

    public void TakeDamage(int amount)
    {
        guardHealth -= amount;
        Debug.Log("Guardia recibió daño. Salud restante: " + guardHealth);

        if (guardHealth < 5 && currentState != GuardState.Flee)
        {
            Debug.Log("Guardia entra en estado de HUIDA");
            currentState = GuardState.Flee;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Vector3 left = Quaternion.Euler(0, 0, -visionAngle / 2f) * transform.up;
        Vector3 right = Quaternion.Euler(0, 0, visionAngle / 2f) * transform.up;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, left * visionRange);
        Gizmos.DrawRay(transform.position, right * visionRange);
    }
}
