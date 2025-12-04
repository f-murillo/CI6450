using System.Collections.Generic;
using UnityEngine;

public struct Edge3
{
    public Vector3 A, B;
    public Edge3(Vector3 a, Vector3 b)
    {
        if (a.x != b.x ? a.x < b.x : (a.y != b.y ? a.y < b.y : a.z < b.z))
        { A = a; B = b; }
        else { A = b; B = a; }
    }
    public override int GetHashCode() => A.GetHashCode() ^ (B.GetHashCode() * 486187739);
    public override bool Equals(object obj)
    {
        if (!(obj is Edge3 e)) return false;
        return A == e.A && B == e.B;
    }
}

public class TriNode
{
    public Vector3 v1, v2, v3;
    public int areaId;
    public string areaName;
    public readonly List<TriNode> neighbors = new List<TriNode>();

    public TriNode(Vector3 a, Vector3 b, Vector3 c, int areaId, string areaName)
    { v1 = a; v2 = b; v3 = c; this.areaId = areaId; this.areaName = areaName; }

    public IEnumerable<Edge3> Edges()
    {
        yield return new Edge3(v1, v2);
        yield return new Edge3(v2, v3);
        yield return new Edge3(v3, v1);
    }

    public Vector3 Centroid() => (v1 + v2 + v3) / 3f;
}

public class RegionNode
{
    public int areaId;
    public string areaName;
    public readonly List<TriNode> triangles = new List<TriNode>();
    public readonly List<RegionNode> neighbors = new List<RegionNode>();
    public Vector3 center;

    public RegionNode(int id, string name) { areaId = id; areaName = name; }

    public void ComputeCenter()
    {
        if (triangles.Count == 0) { center = Vector3.zero; return; }
        Vector3 sum = Vector3.zero;
        foreach (var t in triangles) sum += t.Centroid();
        center = sum / triangles.Count;
    }
}
