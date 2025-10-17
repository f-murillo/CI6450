using UnityEngine;
using System.Collections.Generic;

public class CollisionAvoidance : MonoBehaviour
{
    public float radius = 2f;
    public float maxAcceleration = 10f;

    public Transform[] targets;

    DynamicMovement movement;

    Vector3 avoidanceVector = Vector3.zero;
    Vector3 previousAvoidance = Vector3.zero;
    float lastDistanceToTarget = Mathf.Infinity;

    void Awake()
    {
        movement = GetComponent<DynamicMovement>();

        GameObject[] groupA = GameObject.FindGameObjectsWithTag("GrupoA");
        GameObject[] groupB = GameObject.FindGameObjectsWithTag("GrupoB");

        List<Transform> validTargets = new List<Transform>();

        foreach (GameObject obj in groupA)
        {
            if (obj != gameObject) validTargets.Add(obj.transform);
        }
        foreach (GameObject obj in groupB)
        {
            if (obj != gameObject) validTargets.Add(obj.transform);
        }

        targets = validTargets.ToArray();
    }

    public SteeringOutput GetSteering()
    {
        SteeringOutput result = new SteeringOutput();
        Vector3 characterPos = transform.position;
        Vector3 characterVel = movement.linearVelocity;

        // 🟥 Separation directa si hay NPCs dentro del radius
        Vector3 separationForce = Vector3.zero;
        foreach (Transform target in targets)
        {
            float dist = Vector3.Distance(characterPos, target.position);
            if (dist < radius)
            {
                Vector3 away = (characterPos - target.position).normalized;
                float strength = Mathf.Clamp01((radius - dist) / radius);
                separationForce += away * strength;
            }
        }

        if (separationForce != Vector3.zero)
        {
            avoidanceVector = separationForce.normalized * maxAcceleration;
            previousAvoidance = avoidanceVector;
            result.linearAcceleration = avoidanceVector;
            result.angularAcceleration = 0f;
            return result;
        }

        // 🟨 Collision Avoidance predictiva
        float shortestTime = Mathf.Infinity;
        Transform firstTarget = null;

        float firstMinSeparation = 0f;
        float firstDistance = 0f;
        Vector3 firstRelativePos = Vector3.zero;
        Vector3 firstRelativeVel = Vector3.zero;

        foreach (Transform target in targets)
        {
            DynamicMovement targetMovement = target.GetComponent<DynamicMovement>();
            if (targetMovement == null) continue;

            Vector3 targetPos = target.position;
            Vector3 targetVel = targetMovement.linearVelocity;

            Vector3 relativePos = targetPos - characterPos;
            Vector3 relativeVel = targetVel - characterVel;
            float relativeSpeed = relativeVel.magnitude;

            if (relativeSpeed == 0f) continue;

            float timeToCollision = Vector3.Dot(relativePos, relativeVel) / (relativeSpeed * relativeSpeed);

            float distance = relativePos.magnitude;
            float minSeparation = distance - relativeSpeed * timeToCollision;

            if (minSeparation > radius) continue;

            if (timeToCollision > 0f && timeToCollision < shortestTime)
            {
                shortestTime = timeToCollision;
                firstTarget = target;
                firstMinSeparation = minSeparation;
                firstDistance = distance;
                firstRelativePos = relativePos;
                firstRelativeVel = relativeVel;
            }
        }

        if (firstTarget == null)
        {
            avoidanceVector = Vector3.zero;
            previousAvoidance = Vector3.zero;
            result.linearAcceleration = Vector3.zero;
            result.angularAcceleration = 0f;
            return result;
        }

        // 🟦 Desactivar evasión si la colisión ya fue evitada
        bool collisionAlreadyAvoided = firstDistance > lastDistanceToTarget && firstDistance > radius * 1.2f;
        lastDistanceToTarget = firstDistance;

        if (collisionAlreadyAvoided)
        {
            avoidanceVector = Vector3.zero;
            previousAvoidance = Vector3.zero;
            result.linearAcceleration = Vector3.zero;
            result.angularAcceleration = 0f;
            return result;
        }

        // 🟩 Dirección de evasión
        Vector3 avoidanceDirection;
        if (firstMinSeparation <= 0f || firstDistance < 2f * radius)
        {
            Vector3 tangent = firstRelativeVel.magnitude < 0.1f
                ? Vector3.right
                : Vector3.Cross(firstRelativeVel.normalized, Vector3.forward);

            avoidanceDirection = tangent;

            if (firstDistance < radius * 0.8f)
            {
                Vector3 separation = (characterPos - firstTarget.position).normalized;
                avoidanceDirection += separation * 0.5f;
            }
        }
        else
        {
            avoidanceDirection = firstRelativePos + firstRelativeVel * shortestTime;
        }

        avoidanceDirection.Normalize();

        // 🔁 Suavizado y limitación de giro
        float maxAngleChange = 45f;
        float angleDiff = Vector3.SignedAngle(previousAvoidance, avoidanceDirection, Vector3.forward);
        if (Mathf.Abs(angleDiff) > maxAngleChange && previousAvoidance != Vector3.zero)
        {
            avoidanceDirection = Quaternion.RotateTowards(
                Quaternion.LookRotation(Vector3.forward, previousAvoidance),
                Quaternion.LookRotation(Vector3.forward, avoidanceDirection),
                maxAngleChange
            ) * Vector3.up;
        }

        float threatFactor = Mathf.Clamp01((2f * radius - firstDistance) / (2f * radius));
        float targetMagnitude = maxAcceleration * threatFactor;

        Vector3 smoothedDirection = Vector3.Lerp(previousAvoidance.normalized, avoidanceDirection, 0.3f);
        float smoothedMagnitude = Mathf.Lerp(previousAvoidance.magnitude, targetMagnitude, 0.3f);

        avoidanceVector = smoothedDirection.normalized * smoothedMagnitude;
        previousAvoidance = avoidanceVector;

        result.linearAcceleration = avoidanceVector;
        result.angularAcceleration = 0f;
        return result;
    }

    void Update()
    {
        if (avoidanceVector.magnitude > 0.1f)
        {
            Debug.DrawLine(transform.position, transform.position + avoidanceVector, Color.cyan);
        }
    }
}
