using System.Collections.Generic;
using UnityEngine;

public enum PathfindingMode { Standard, Hierarchical }

public class NPCPathController : MonoBehaviour
{
    public PathfindingMode mode = PathfindingMode.Standard;
    public NavMeshGraphBuilder graphBuilder;
    public Transform target;
    public LayerMask obstaclesMask;

    private List<Vector3> waypoints = new();
    private int currentIndex = 0;

    [SerializeField] float speed = 2f;
    [SerializeField] float avoidanceRadius = 0.5f;
    [SerializeField] float avoidanceStrength = 1.5f;
    [SerializeField] float waypointThreshold = 0.1f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        graphBuilder.ResetGraph();
        ComputePath();
        lastTargetPosition = target.position;
    }


    void Update()
    {
        Vector3 currentPos = transform.position;
        Vector3 goalPos = target.position;

        
        
        if (HasLineOfSight(currentPos, goalPos))
        {
            Vector3 toGoal = (goalPos - currentPos).normalized;
            Vector3 avoidance = ComputeAvoidance(currentPos);
            Vector3 finalDir = (toGoal + avoidance).normalized;

            transform.up = finalDir; 
            rb.MovePosition(rb.position + (Vector2)(finalDir * speed * Time.deltaTime));
            return;
        }
        


        if (waypoints == null || waypoints.Count == 0 || currentIndex >= waypoints.Count)
            return;

        
        

        for (int i = waypoints.Count - 1; i > currentIndex; i--)
        {
            if (HasLineOfSight(currentPos, waypoints[i]))
            {
                currentIndex = i;
                break;
            }
        }
        

        Vector3 targetPos = waypoints[currentIndex];
        Vector3 toTarget = (targetPos - currentPos).normalized;
        Vector3 avoidanceForce = ComputeAvoidance(currentPos);
        Vector3 finalDirection = (toTarget + avoidanceForce).normalized;

        transform.up = finalDirection; 
        rb.MovePosition(rb.position + (Vector2)(finalDirection * speed * Time.deltaTime));



        if (Vector3.Distance(currentPos, targetPos) < waypointThreshold)
        {
            currentIndex++;
            if (currentIndex >= waypoints.Count)
                currentIndex = waypoints.Count - 1;
        }
    }

    Vector3 ComputeAvoidance(Vector3 origin)
    {
        Vector3 avoidance = Vector3.zero;
        Collider2D[] obstacles = Physics2D.OverlapCircleAll(origin, avoidanceRadius, obstaclesMask);

        foreach (var col in obstacles)
        {
            Vector3 obstaclePos = col.ClosestPoint(origin);
            Vector3 away = origin - obstaclePos;
            float distance = away.magnitude;

            if (distance > 0.01f)
                avoidance += away.normalized * (avoidanceStrength / distance);
        }

        return avoidance;
    }

    void ComputePath()
    {
        TriNode startTri = FindContainingTriangle(transform.position);
        TriNode goalTri = FindContainingTriangle(target.position);

        if (startTri == null || goalTri == null)
        {
            Debug.LogWarning("NPCPathController: No se encontraron triángulos para inicio o destino.");
            return;
        }

        waypoints.Clear();

        if (mode == PathfindingMode.Standard)
        {
            var triPath = StandardPathfinder.FindPath(startTri, goalTri);
            AddTriPath(triPath, target.position);
        }
        else if (mode == PathfindingMode.Hierarchical)
        {
            var startRegion = graphBuilder.regions.Find(r => r.areaId == startTri.areaId);
            var goalRegion = graphBuilder.regions.Find(r => r.areaId == goalTri.areaId);

            if (startRegion == null || goalRegion == null) return;

            if (startRegion == goalRegion)
            {
                var triPath = StandardPathfinder.FindPath(startTri, goalTri);
                AddTriPath(triPath, target.position);
            }
            else
            {
                var regionPath = HierarchicalPathfinder.FindRegionPath(startRegion, goalRegion);
                if (regionPath == null) return;

                TriNode currentTri = startTri;

                for (int i = 0; i < regionPath.Count; i++)
                {
                    RegionNode region = regionPath[i];

                    if (region == goalRegion)
                    {
                        var triPath = StandardPathfinder.FindPath(currentTri, goalTri);
                        AddTriPath(triPath, target.position);
                    }
                    else
                    {
                        RegionNode nextRegion = regionPath[i + 1];
                        Edge3? portal = FindPortal(region, nextRegion);

                        if (portal.HasValue)
                        {
                            Vector3 portalMid = (portal.Value.A + portal.Value.B) / 2f;
                            TriNode portalTri = ClosestTriangleToEdge(region.triangles, portal.Value);

                            var triPath = StandardPathfinder.FindPath(currentTri, portalTri);
                            AddTriPath(triPath, portalMid);

                            currentTri = ClosestTriangleToEdge(nextRegion.triangles, portal.Value);
                        }
                    }
                }
            }
        }

        currentIndex = 0;
    }

    void AddTriPath(List<TriNode> triPath, Vector3 goal)
    {
        if (triPath == null) return;

        Vector3 currentPos = transform.position;

        foreach (var t in triPath)
        {
            Vector3 centroid = t.Centroid();

            if (Physics2D.OverlapPoint(new Vector2(centroid.x, centroid.y), obstaclesMask) != null)
                continue;

            if (HasLineOfSight(currentPos, goal)) break;

            waypoints.Add(centroid);
            currentPos = centroid;
        }

        waypoints.Add(goal);
    }

    Edge3? FindPortal(RegionNode A, RegionNode B)
    {
        var edgesA = new HashSet<Edge3>();
        foreach (var t in A.triangles)
            foreach (var e in t.Edges())
                edgesA.Add(e);

        foreach (var t in B.triangles)
            foreach (var e in t.Edges())
                if (edgesA.Contains(e)) return e;

        return null;
    }

    TriNode ClosestTriangleToEdge(List<TriNode> tris, Edge3 e)
    {
        Vector3 mid = (e.A + e.B) / 2f;
        TriNode best = tris[0];
        float bd = float.MaxValue;

        foreach (var t in tris)
        {
            float d = Vector3.Distance(t.Centroid(), mid);
            if (d < bd) { bd = d; best = t; }
        }

        return best;
    }

    TriNode FindContainingTriangle(Vector3 pos)
    {
        foreach (var t in graphBuilder.triangles)
            if (PointInTriangle(pos, t.v1, t.v2, t.v3)) return t;

        return null;
    }

    bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector2 p2 = new(p.x, p.y), a2 = new(a.x, a.y), b2 = new(b.x, b.y), c2 = new(c.x, c.y);
        float area = Mathf.Abs(a2.x * (b2.y - c2.y) + b2.x * (c2.y - a2.y) + c2.x * (a2.y - b2.y));
        float area1 = Mathf.Abs(p2.x * (a2.y - b2.y) + a2.x * (b2.y - p2.y) + b2.x * (p2.y - a2.y));
        float area2 = Mathf.Abs(p2.x * (b2.y - c2.y) + b2.x * (c2.y - p2.y) + c2.x * (p2.y - b2.y));
        float area3 = Mathf.Abs(p2.x * (c2.y - a2.y) + c2.x * (a2.y - p2.y) + a2.x * (p2.y - c2.y));
        return Mathf.Abs(area - (area1 + area2 + area3)) < 0.01f;
    }

    bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        Debug.DrawLine(from, to, Color.cyan, 0.2f);
        return Physics2D.Linecast(from, to, obstaclesMask).collider == null;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        ComputePath();
    }

    public void TryRecalculatePath(float threshold = 1f)
    {
        if (target == null) return;

        float dist = Vector3.Distance(lastTargetPosition, target.position);
        if (dist > threshold)
        {
            ComputePath();
            lastTargetPosition = target.position;
        }
    }

    private Vector3 lastTargetPosition;


    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        Gizmos.color = Color.magenta;
        Vector3 prev = transform.position;
        foreach (var wp in waypoints)
        {
            Gizmos.DrawLine(prev, wp);
            Gizmos.DrawSphere(wp, 0.1f);
            prev = wp;
        }
    }
}
