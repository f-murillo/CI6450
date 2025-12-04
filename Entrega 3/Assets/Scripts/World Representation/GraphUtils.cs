using System.Collections.Generic;
using UnityEngine;

public static class GraphUtils
{

    // Para construir las conexiones entre triangulos vecinos
    public static void BuildTriangleAdjacency(List<TriNode> triangles)
    {
        for (int i = 0; i < triangles.Count; i++)
        {
            TriNode a = triangles[i];
            for (int j = i + 1; j < triangles.Count; j++)
            {
                TriNode b = triangles[j];

                if (ShareEdge(a, b))
                {
                    a.neighbors.Add(b);
                    b.neighbors.Add(a);
                }
            }
        }
    }


    // Para determinar si dos triangulos comparten al menos dos vertices (que seria una arista)
    private static bool ShareEdge(TriNode a, TriNode b)
    {
        int shared = 0;
        if (ApproximatelyEqual(a.v1, b.v1) || ApproximatelyEqual(a.v1, b.v2) || ApproximatelyEqual(a.v1, b.v3)) shared++;
        if (ApproximatelyEqual(a.v2, b.v1) || ApproximatelyEqual(a.v2, b.v2) || ApproximatelyEqual(a.v2, b.v3)) shared++;
        if (ApproximatelyEqual(a.v3, b.v1) || ApproximatelyEqual(a.v3, b.v2) || ApproximatelyEqual(a.v3, b.v3)) shared++;
        return shared >= 2;
    }


    // Para comparar dos puntos con tolerancia (para evitar errores por precision flotante)
    private static bool ApproximatelyEqual(Vector3 a, Vector3 b)
    {
        return Vector3.SqrMagnitude(a - b) < 0.001f;
    }


    // Para construir las conexiones entre regiones si comparten al menos una arista entre sus triangulos
    public static void BuildRegionAdjacencyByBoundary(List<RegionNode> regions)
    {
        for (int i = 0; i < regions.Count; i++)
        {
            RegionNode A = regions[i];
            for (int j = i + 1; j < regions.Count; j++)
            {
                RegionNode B = regions[j];

                if (ShareBoundary(A, B))
                {
                    A.neighbors.Add(B);
                    B.neighbors.Add(A);
                }
            }
        }
    }


    // Para determinar si dos regiones comparten al menos una arista entre sus triangulos (si comparten limite)
    private static bool ShareBoundary(RegionNode A, RegionNode B)
    {
        var edgesA = new HashSet<Edge3>();
        foreach (var t in A.triangles)
            foreach (var e in t.Edges())
                edgesA.Add(e);

        foreach (var t in B.triangles)
            foreach (var e in t.Edges())
                if (edgesA.Contains(e)) return true;

        return false;
    }
}
