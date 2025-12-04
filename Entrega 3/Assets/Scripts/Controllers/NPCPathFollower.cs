using System.Collections.Generic;
using UnityEngine;

public class NPCPathFollower : MonoBehaviour
{
    public Transform pathRoot;
    public float threshold = 0.2f;
    public bool loopPath = true;

    private List<Vector3> waypoints = new();
    private int currentIndex = 0;
    private NPCMove mover;  

    void Start()
    {
        mover = GetComponent<NPCMove>();

        foreach (Transform child in pathRoot)
            waypoints.Add(child.position);

        ResumeFromClosestPoint(transform.position);
    }

    void Update()
    {
        if (waypoints.Count == 0 || mover == null) return;

        Vector3 target = waypoints[currentIndex];
        mover.MoveTowards(target);   // delegamos el movimiento al script reutilizable

        float dist = Vector3.Distance(transform.position, target);
        if (dist < threshold)
        {
            currentIndex++;
            if (currentIndex >= waypoints.Count)
                currentIndex = loopPath ? 0 : waypoints.Count - 1;
        }
    }

    public void ResumeFromClosestPoint(Vector3 npcPosition)
    {
        float minDist = float.MaxValue;
        int closest = 0;

        for (int i = 0; i < waypoints.Count; i++)
        {
            float dist = Vector3.Distance(npcPosition, waypoints[i]);
            if (dist < minDist)
            {
                minDist = dist;
                closest = i;
            }
        }

        currentIndex = closest;
    }
}
