using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float lifetime = 3f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("El jugador fue alcanzado por un proyectil.");
            Destroy(gameObject);
        }
        else if (other.CompareTag("Enemy"))
        {
            GuardController guard = other.GetComponent<GuardController>();
            if (guard != null)
            {
                guard.TakeDamage(1);
            }
            Destroy(gameObject);
        }
        else if (other.CompareTag("Drone"))
        {
            DroneController drone = other.GetComponent<DroneController>();
            if (drone != null)
                drone.TakeDamage(1);

            Destroy(gameObject);
        }
        else if (other.CompareTag("Boss"))
        {
            BossFSMController boss = other.GetComponent<BossFSMController>();
            if (boss != null)
                boss.TakeDamage(1);

            Destroy(gameObject);
        }


        else if (!other.isTrigger || other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            Destroy(gameObject);
        }
    }

}
