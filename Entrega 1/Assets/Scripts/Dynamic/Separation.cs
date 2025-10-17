using UnityEngine;
using System.Collections.Generic;

public class Separation : MonoBehaviour
{
    public float maxAcceleration = 8f;
    public float threshold = 2f;
    public float decayCoefficient = 10f;
    public List<Transform> targets = new List<Transform>();
    Vector3 direction;
    float distance, strength;

    void Start()
    {
        GameObject[] allNPCs = GameObject.FindGameObjectsWithTag("NPC");

        foreach (GameObject npc in allNPCs)
        {
            if (npc != gameObject) // to avoid add himself
            {
                targets.Add(npc.transform);
            }
        }
    }


    public SteeringOutput GetSteering()
    {
        SteeringOutput result = new SteeringOutput();

        foreach (Transform target in targets)
        {
            direction = transform.position - target.position;
            distance = direction.magnitude;

            if (distance < threshold && distance > 0f)
            {
                strength = Mathf.Min(decayCoefficient / (distance * distance), maxAcceleration);
                direction.Normalize();
                result.linearAcceleration += direction * strength;
            }
        }
        Debug.DrawLine(transform.position, transform.position + result.linearAcceleration, Color.cyan);
        return result;
    }
}
