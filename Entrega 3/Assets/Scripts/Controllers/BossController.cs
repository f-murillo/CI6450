using UnityEngine;

public enum BossState { Idle, Chase, Attack, Heal, Return }

public class BossFSMController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public NPCMovement mover;
    public NPCPathController pathController;
    public LayerMask obstacleMask;

    [Header("Parámetros de visión")]
    public float detectionRadius = 6f;
    public float lostSightDuration = 2f;
    public float rotationSpeed = 45f;

    [Header("Ataque")]
    public float attackRange = 4f;
    public float attackCooldown = 1.2f;
    private float attackTimer = 0f;

    [Header("Salud")]
    public int maxHealth = 6;
    public int currentHealth = 6;
    private bool hasHealed = false;

    private BossState currentState = BossState.Idle;
    private bool playerDetected = false;
    private float lostSightTimer = 0f;
    private Vector3 initialPosition;

    private ProjectileShooter shooter;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Transform bossHome;

    void Start()
    {
        initialPosition = transform.position;

        if (mover == null)
            mover = GetComponent<NPCMovement>();

        if (pathController == null)
            pathController = GetComponent<NPCPathController>();

        shooter = GetComponent<ProjectileShooter>();
        if (shooter == null)
            Debug.LogError("BossFSMController: No se encontró ProjectileShooter");

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        GameObject home = new GameObject("Boss Home");
        home.transform.position = initialPosition;
        bossHome = home.transform;
        pathController.enabled = false;
    }

    void Update()
    {
        switch (currentState)
        {
            case BossState.Idle:
                pathController.enabled = false;
                RotateInPlace();
                break;

            case BossState.Chase:
                mover.MoveTowards(player.position);
                break;

            case BossState.Attack:
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
                    currentState = BossState.Chase;
                break;


            case BossState.Return:
                Debug.Log("Jefe volviendo");
                pathController.enabled = true;
                pathController.target = bossHome;

                pathController.ComputePath();
                /*
                if (pathController != null)
                {
                    pathController.SetTargetPosition(initialPosition);
                }
                */

                if (Vector3.Distance(transform.position, initialPosition) < 0.2f)
                    currentState = BossState.Idle;
                break;

            case BossState.Heal:
                Debug.Log("Jefe se está curando...");
                currentHealth = maxHealth;
                hasHealed = true;

                if (spriteRenderer != null)
                    spriteRenderer.color = Color.red;

                currentState = BossState.Chase;
                break;
        }

        EvaluateTransitions();
        
    }

    void RotateInPlace()
    {
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
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
            case BossState.Idle:
                if (playerDetected)
                    currentState = BossState.Chase;
                break;

            case BossState.Chase:
                if (!playerDetected)
                {
                    currentState = BossState.Return;
                }
                else if (dist <= attackRange)
                {
                    if (currentHealth < 3 && !hasHealed)
                        currentState = BossState.Heal;
                    else
                        currentState = BossState.Attack;
                }
                break;

            case BossState.Attack:
                if (currentHealth < 3 && !hasHealed)
                {
                    currentState = BossState.Heal;
                }
                else if (!playerDetected)
                {
                    currentState = BossState.Return;
                }
                else if (dist > attackRange + 1f)
                {
                    currentState = BossState.Chase;
                }
                break;
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log("Jefe recibió daño. Salud actual: " + currentHealth);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}