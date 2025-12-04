using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshGraphBuilder : MonoBehaviour
{
    [Header("Visualizacion del grafo")]
    public bool showTriangles = true;
    public bool showRegionCenters = true;
    public bool showRegionGraph = true;
    public Color triColor = new Color(1f, 0.9f, 0.2f, 1f);
    public Color regionCenterColor = Color.green;
    public Color regionEdgeColor = Color.cyan;
    [Header("Visualizacion de subgrafos")]
    public bool showTriangleCentroids = true;
    public bool showTriangleGraph = true;
    public Color triCentroidColor = Color.blue;
    public Color triGraphColor = Color.magenta;


    public List<TriNode> triangles = new List<TriNode>();
    public List<RegionNode> regions = new List<RegionNode>();

    private static readonly Dictionary<int, string> areaNames = new Dictionary<int, string>
    {
        {0, "Walkable"},
        {1, "Not Walkable"},
        {2, "Jump"},
        {3, "RoomA"},
        {4, "RoomB"},
        {5, "RoomC"},
        {6, "RoomD"},
        {7, "RoomE"},
        {8, "RoomF"},
        {9, "RoomG"},
        {10, "RoomH"},
        {11, "RoomI"},
        {12, "RoomJ"},
        {13, "Hallway"}
    };

    void Awake()
    {
        BuildFromNavMesh();
    }

    public void BuildFromNavMesh()
    {
        triangles = new List<TriNode>();
        regions = new List<RegionNode>();

        var tri = NavMesh.CalculateTriangulation();
        var verts = tri.vertices;
        var indices = tri.indices;
        var areas = tri.areas;

        for (int i = 0; i < indices.Length; i += 3)
        {
            Vector3 v1 = verts[indices[i]];
            Vector3 v2 = verts[indices[i + 1]];
            Vector3 v3 = verts[indices[i + 2]];
            int areaId = areas[i / 3];
            string areaName = areaNames.ContainsKey(areaId) ? areaNames[areaId] : "Unknown";

            var node = new TriNode(v1, v2, v3, areaId, areaName);
            triangles.Add(node);
        }

        // Primero hay que limpiar a los vecinos de los triangulos antes de reconstruir conexiones
        foreach (var t in triangles)
            t.neighbors.Clear();

        GraphUtils.BuildTriangleAdjacency(triangles);

        var regionMap = new Dictionary<int, RegionNode>();
        foreach (var t in triangles)
        {
            if (!regionMap.TryGetValue(t.areaId, out var r))
            {
                r = new RegionNode(t.areaId, t.areaName);
                regionMap[t.areaId] = r;
            }
            r.triangles.Add(t);
        }

        regions = new List<RegionNode>(regionMap.Values);
        foreach (var r in regions) r.ComputeCenter();

        foreach (var r in regions)
            r.neighbors.Clear();

        GraphUtils.BuildRegionAdjacencyByBoundary(regions);

    }

    public void ResetGraph()
    {
        triangles = new List<TriNode>();
        regions = new List<RegionNode>();
        BuildFromNavMesh();
    }

    public TriNode FindClosestTriNode(Vector3 position) {
        if (triangles == null || triangles.Count == 0) return null;

        TriNode closest = null;
        float minDist = float.MaxValue;

        foreach (var tri in triangles) {
            Vector3 centroid = tri.Centroid();
            float dist = Vector3.Distance(position, centroid);

            if (dist < minDist) {
                minDist = dist;
                closest = tri;
            }
        }

        return closest;
    }


    void OnDrawGizmos()
    {
        if (showTriangles)
        {
            Gizmos.color = triColor;
            foreach (var t in triangles)
            {
                Gizmos.DrawLine(t.v1, t.v2);
                Gizmos.DrawLine(t.v2, t.v3);
                Gizmos.DrawLine(t.v3, t.v1);
            }
        }

        if (showRegionCenters)
        {
            Gizmos.color = regionCenterColor;
            foreach (var r in regions)
            {
                Gizmos.DrawSphere(r.center, 0.15f);
            }
        }

        if (showRegionGraph)
        {
            Gizmos.color = regionEdgeColor;
            foreach (var r in regions)
            {
                foreach (var n in r.neighbors)
                {
                    Gizmos.DrawLine(r.center, n.center);
                }
            }
        }

        if (showTriangleCentroids)
        {
            Gizmos.color = triCentroidColor;
            foreach (var t in triangles)
            {
                Gizmos.DrawSphere(t.Centroid(), 0.08f);
            }
        }

        if (showTriangleGraph)
        {
            Gizmos.color = triGraphColor;
            foreach (var t in triangles)
            {
                foreach (var n in t.neighbors)
                {
                    Gizmos.DrawLine(t.Centroid(), n.Centroid());
                }
            }
        }

    }
}
