using System.IO.Compression;
using Unity.Collections;
using UnityEngine;

public class LookWhereYoureGoing : MonoBehaviour
{
    Align align;
    DynamicMovement movement;

    void Start()
    {
        align = GetComponent<Align>();
        movement = GetComponent<DynamicMovement>();
    }

    void Update()
    {
        Vector3 velocity = movement.linearVelocity;

        if (velocity.sqrMagnitude == 0f)
        {
            align.enabled = false;
            return;
        }

        float desiredOrientation = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f;

        align.useExplicitOrientation = true;
        align.explicitOrientation = desiredOrientation;
        align.enabled = true;
    }
}
