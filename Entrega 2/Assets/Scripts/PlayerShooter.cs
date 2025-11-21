using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [Header("Disparo")]
    public float shootCooldown = 0.5f;
    private float shootTimer = 0f;

    private ProjectileShooter shooter;

    void Start()
    {
        shooter = GetComponent<ProjectileShooter>();
        if (shooter == null)
            Debug.LogError("PlayerShooter: No se encontró ProjectileShooter en " + gameObject.name);
    }

    void Update()
    {
        shootTimer -= Time.deltaTime;

        // Disparo con clic izquierdo o tecla espacio
        if ((Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space)) && shootTimer <= 0f)
        {
            Vector2 direction = shooter.firePoint.up; // dirección hacia adelante del jugador
            shooter.Shoot(direction, gameObject);
            shootTimer = shootCooldown;
        }
    }
}
