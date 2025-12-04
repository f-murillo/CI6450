using System.Collections.Generic;
using UnityEngine;

public class StandardPathfinder
{
    public static List<TriNode> FindPath(TriNode start, TriNode goal)
    {
        var open = new SortedList<float, List<TriNode>>();
        var openSet = new HashSet<TriNode>();
        var cameFrom = new Dictionary<TriNode, TriNode>();
        var gScore = new Dictionary<TriNode, float> { [start] = 0f };
        var fScore = new Dictionary<TriNode, float> { [start] = Heuristic(start, goal) };

        AddToOpen(open, fScore[start], start);
        openSet.Add(start);

        var closed = new HashSet<TriNode>();

        while (open.Count > 0)
        {
            TriNode current = ExtractLowest(open);
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

    private static float Heuristic(TriNode node, TriNode goal)
    {
        return Vector2.SqrMagnitude(To2D(node.Centroid()) - To2D(goal.Centroid()));
    }

    private static float Cost(TriNode a, TriNode b, TriNode goal)
    {
        Vector2 ca = To2D(a.Centroid());
        Vector2 cb = To2D(b.Centroid());
        Vector2 cg = To2D(goal.Centroid());

        float baseCost = Vector2.SqrMagnitude(cb - ca);
        float before = Vector2.SqrMagnitude(ca - cg);
        float after = Vector2.SqrMagnitude(cb - cg);

        float delta = after - before;
        float penalty = Mathf.Max(0f, delta * 1f); // penalización proporcional

        return baseCost + penalty;
    }

    private static Vector2 To2D(Vector3 v) => new Vector2(v.x, v.y);

    private static void AddToOpen(SortedList<float, List<TriNode>> open, float score, TriNode node)
    {
        if (!open.ContainsKey(score)) open[score] = new List<TriNode>();
        open[score].Add(node);
    }

    private static TriNode ExtractLowest(SortedList<float, List<TriNode>> open)
    {
        var first = open.Keys[0];
        var list = open[first];
        var node = list[0];
        list.RemoveAt(0);
        if (list.Count == 0) open.Remove(first);
        return node;
    }

    private static List<TriNode> Reconstruct(Dictionary<TriNode, TriNode> cameFrom, TriNode current)
    {
        var path = new List<TriNode> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }
}
