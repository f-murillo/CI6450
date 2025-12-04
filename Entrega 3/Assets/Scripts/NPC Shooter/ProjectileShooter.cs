using UnityEngine;

public class ProjectileShooter : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 6f;

    public void Shoot(Vector2 direction, GameObject ignoreCollisionWith = null)
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("ProjectileShooter: Prefab o FirePoint no asignado");
            return;
        }

        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(Vector3.forward, direction));

        // Ignorar colisión con quien dispara
        if (ignoreCollisionWith != null)
        {
            Collider2D bulletCol = bullet.GetComponent<Collider2D>();
            Collider2D shooterCol = ignoreCollisionWith.GetComponent<Collider2D>();
            if (bulletCol != null && shooterCol != null)
                Physics2D.IgnoreCollision(bulletCol, shooterCol);
        }

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = direction.normalized * projectileSpeed;
        }

        Debug.Log("ProjectileShooter: Proyectil disparado hacia " + direction);
    }
}
