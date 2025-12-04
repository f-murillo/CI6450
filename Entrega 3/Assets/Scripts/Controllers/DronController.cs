using UnityEngine;

public enum DroneState { Patrol, Chase, Attack, AggressiveAttack, Return }

public class DroneController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public NPCPathFollower patrol;
    public NPCMovement mover;
    public NPCPathController pathController;
    public LayerMask obstacleMask;

    [Header("Sensores")]
    public float detectionRadius = 8f;
    public float lostSightDuration = 1.5f;

    [Header("Ataque")]
    public float attackRange = 6f;
    public float attackCooldown = 1f;
    private float attackTimer = 0f;

    [Header("Salud")]
    public int droneHealth = 5;

    [Header("Ataque agresivo")]
    public float spinSpeed = 180f; // grados por segundo

    private DroneState currentState = DroneState.Patrol;
    private DroneState previousState = DroneState.Patrol;
    private bool playerDetected = false;
    private float lostSightTimer = 0f;

    private ProjectileShooter shooter;

    void Start()
    {
        if (mover == null)
            mover = GetComponent<NPCMovement>();

        if (pathController == null)
            pathController = GetComponent<NPCPathController>();

        shooter = GetComponent<ProjectileShooter>();
        if (shooter == null)
            Debug.LogError("DroneController: No se encontró ProjectileShooter");
    }

    void Update()
    {
        switch (currentState)
        {
            case DroneState.Patrol:
                patrol.enabled = true;
                pathController.enabled = false;
                break;

            case DroneState.Chase:
                patrol.enabled = false;
                pathController.enabled = true;

                if (previousState != DroneState.Chase)
                {
                    pathController.SetTarget(player);
                }

                pathController.TryRecalculatePath(1f);
                break;

            case DroneState.Attack:
                patrol.enabled = false;
                pathController.enabled = false;

                Vector2 dirToPlayer = (player.position - transform.position).normalized;
                transform.up = dirToPlayer;

                if (attackTimer <= 0f)
                {
                    shooter.Shoot(dirToPlayer, gameObject);
                    attackTimer = attackCooldown;
                }
                else
                {
                    attackTimer -= Time.deltaTime;
                }

                if (Vector2.Distance(transform.position, player.position) > attackRange + 1f)
                    currentState = DroneState.Chase;
                break;

            case DroneState.AggressiveAttack:
                patrol.enabled = false;
                pathController.enabled = false;

                transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);

                if (attackTimer <= 0f)
                {
                    Vector2 shootDir = transform.up;
                    shooter.Shoot(shootDir, gameObject);
                    attackTimer = attackCooldown;
                }
                else
                {
                    attackTimer -= Time.deltaTime;
                }

                if (Vector2.Distance(transform.position, player.position) > attackRange + 1f)
                    currentState = DroneState.Chase;
                break;

            case DroneState.Return:
                patrol.enabled = true;
                pathController.enabled = false;

                patrol.ResumeFromClosestPoint(transform.position);
                currentState = DroneState.Patrol;
                break;
        }

        EvaluateTransitions();
        previousState = currentState;
    }

    void EvaluateTransitions()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, player.position);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, dist, obstacleMask);
        bool seesPlayer = dist < detectionRadius && hit.collider == null;

        if (seesPlayer)
        {
            playerDetected = true;
            lostSightTimer = 0f;
        }
        else if (playerDetected)
        {
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer >= lostSightDuration)
            {
                playerDetected = false;
            }
        }

        switch (currentState)
        {
            case DroneState.Patrol:
                if (playerDetected)
                    currentState = DroneState.Chase;
                break;

            case DroneState.Chase:
                if (!playerDetected)
                {
                    currentState = DroneState.Return;
                }
                else if (dist <= attackRange)
                {
                    currentState = (droneHealth < 3) ? DroneState.AggressiveAttack : DroneState.Attack;
                }
                break;

            case DroneState.Attack:
                if (droneHealth < 3)
                {
                    Debug.Log("Dron cambia a ataque agresivo");
                    currentState = DroneState.AggressiveAttack;
                }
                else if (!playerDetected)
                {
                    currentState = DroneState.Return;
                }
                else if (dist > attackRange + 1f)
                {
                    currentState = DroneState.Chase;
                }
                break;
        }
    }

    public void TakeDamage(int amount)
    {
        droneHealth -= amount;
        Debug.Log("Dron recibió daño. Salud restante: " + droneHealth);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
