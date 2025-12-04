using UnityEngine;

public class PathVisualizer : MonoBehaviour
{
    public Transform pathRoot;
    public Color lineColor = Color.cyan;
    public float sphereRadius = 0.15f;

    void OnDrawGizmos()
    {
        if (pathRoot == null || pathRoot.childCount == 0) return;

        Gizmos.color = lineColor;
        Transform prev = pathRoot.GetChild(0);

        foreach (Transform child in pathRoot)
        {
            Gizmos.DrawSphere(child.position, sphereRadius);
            Gizmos.DrawLine(prev.position, child.position);
            prev = child;
        }
    }
}
