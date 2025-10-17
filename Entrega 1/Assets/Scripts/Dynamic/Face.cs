using UnityEngine;

public class Face : MonoBehaviour
{
    public Transform target; // reference to player
    Align align;

    void Start()
    {
        align = GetComponent<Align>();
    }

    void Update()
    {
        // direction towards the target
        Vector3 direction = target.position - transform.position;

        // if there is no direction
        if (direction.sqrMagnitude == 0f)
        {
            align.enabled = false;
            return;
        }

        // orientation in degrees
        float desiredOrientation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        // delegate to align
        align.useExplicitOrientation = true;
        align.explicitOrientation = desiredOrientation;
        align.enabled = true;
    }
}
