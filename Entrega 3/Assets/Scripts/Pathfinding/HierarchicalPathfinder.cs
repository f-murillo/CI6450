using System.Collections.Generic;
using UnityEngine;

public class HierarchicalPathfinder
{
    public static List<RegionNode> FindRegionPath(RegionNode start, RegionNode goal)
    {
        var open = new SortedList<float, List<RegionNode>>();
        var openSet = new HashSet<RegionNode>();
        var cameFrom = new Dictionary<RegionNode, RegionNode>();
        var gScore = new Dictionary<RegionNode, float> { [start] = 0f };
        var fScore = new Dictionary<RegionNode, float> { [start] = Heuristic(start, goal) };

        AddToOpen(open, fScore[start], start);
        openSet.Add(start);

        var closed = new HashSet<RegionNode>();

        while (open.Count > 0)
        {
            RegionNode current = ExtractLowest(open);
            openSet.Remove(current);

            if (current == goal) return Reconstruct(cameFrom, current);

            closed.Add(current);

            foreach (var neighbor in current.neighbors)
            {
                if (closed.Contains(neighbor)) continue;

                float tentative = gScore[current] + Cost(current, neighbor, goal);

                if (!gScore.ContainsKey(neighbor) || tentative < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentative;
                    fScore[neighbor] = tentative + Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                    {
                        AddToOpen(open, fScore[neighbor], neighbor);
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return null;
    }

    private static float Heuristic(RegionNode node, RegionNode goal)
    {
        return Vector2.SqrMagnitude(To2D(node.center) - To2D(goal.center));
    }

    private static float Cost(RegionNode a, RegionNode b, RegionNode goal)
    {
        Vector2 ca = To2D(a.center);
        Vector2 cb = To2D(b.center);
        Vector2 cg = To2D(goal.center);

        float baseCost = Vector2.SqrMagnitude(cb - ca);
        float before = Vector2.SqrMagnitude(ca - cg);
        float after = Vector2.SqrMagnitude(cb - cg);

        float delta = after - before;
        float penalty = Mathf.Max(0f, delta * 1f); // penalización proporcional

        return baseCost + penalty;
    }


    private static Vector2 To2D(Vector3 v) => new Vector2(v.x, v.y);

    private static void AddToOpen(SortedList<float, List<RegionNode>> open, float score, RegionNode node)
    {
        if (!open.ContainsKey(score)) open[score] = new List<RegionNode>();
        open[score].Add(node);
    }

    private static RegionNode ExtractLowest(SortedList<float, List<RegionNode>> open)
    {
        var first = open.Keys[0];
        var list = open[first];
        var node = list[0];
        list.RemoveAt(0);
        if (list.Count == 0) open.Remove(first);
        return node;
    }

    private static List<RegionNode> Reconstruct(Dictionary<RegionNode, RegionNode> cameFrom, RegionNode current)
    {
        var path = new List<RegionNode> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }
}
