using UnityEngine;
using System.Collections.Generic;

public static class NavTacticalPathfinder {
    public static List<Vector3> FindNavFullPath(
        TriNode startTri,
        TriNode goalTri,
        TacticalGraphBuilder tacticalGraph,
        TacticalProfiles.NPCProfile profile,
        Vector3 playerPos,
        LayerMask obstacleMask,
        float visiblePenalty = 4f,   
        int maxSteps = 2000,
        float nodeTacticalRadius = 6f,  
        float edgeTacticalRadius = 4f,  
        float profileScale = 1.0f       
    ) {
        if (startTri == null || goalTri == null) {
            Debug.LogError("NavTacticalPathfinder: startTri o goalTri es null");
            return new List<Vector3>();
        }

        var frontier = new PriorityQueue<TriNode>();
        frontier.Enqueue(startTri, 0f);

        var cameFrom = new Dictionary<TriNode, TriNode>();
        var costSoFar = new Dictionary<TriNode, float> { [startTri] = 0f };
        var visited = new HashSet<TriNode>();

        TriNode current = startTri;
        TriNode bestSoFar = startTri;
        float bestHeuristic = Heuristic(startTri, goalTri);
        int steps = 0;

        while (frontier.Count > 0 && steps < maxSteps) {
            current = frontier.Dequeue();
            steps++;

            visited.Add(current);

            float hCur = Heuristic(current, goalTri);
            if (hCur < bestHeuristic) { bestHeuristic = hCur; bestSoFar = current; }

            if (current == goalTri) break;

            foreach (var next in current.neighbors) {
                if (visited.Contains(next)) continue;

                Vector3 a = current.Centroid();
                Vector3 b = next.Centroid();

                float baseDist = Vector3.Distance(a, b);

                
                float visPenalty = SegmentVisibilityPenalty(a, b, playerPos, obstacleMask, visiblePenalty);

                
                float nodeTactical = EvaluateNodeWithProfile(next, tacticalGraph, profile, nodeTacticalRadius);

                
                float edgeTactical = EvaluateEdgeWithProfile(a, b, tacticalGraph, profile, edgeTacticalRadius);

                
                float tacticalScore = profileScale * (nodeTactical + edgeTactical);

                float newCost = costSoFar[current] + baseDist + visPenalty + tacticalScore;
                newCost = Mathf.Max(0f, newCost);

                if (costSoFar.ContainsKey(next) && newCost >= costSoFar[next]) continue;

                costSoFar[next] = newCost;
                float priority = newCost + Heuristic(next, goalTri);
                frontier.Enqueue(next, priority);
                cameFrom[next] = current;
            }

            if (frontier.Count > 3000) {
                Debug.LogWarning("NavTacticalPathfinder: frontier demasiado grande, abortando");
                break;
            }
        }

        TriNode end = (current == goalTri) ? goalTri : bestSoFar;

        var path = new List<Vector3>();
        int safety = 0;
        while (end != startTri && safety < 800) {
            path.Add(end.Centroid());
            if (!cameFrom.ContainsKey(end)) break;
            end = cameFrom[end];
            safety++;
        }
        path.Add(startTri.Centroid());
        path.Reverse();

        if (path.Count == 0 || path[path.Count - 1] != goalTri.Centroid()) {
            if (path.Count > 0 && !Physics2D.Linecast(path[path.Count - 1], goalTri.Centroid(), obstacleMask)) {
                path.Add(goalTri.Centroid());
            }
        }

        return path;
    }

    private static float Heuristic(TriNode a, TriNode b) {
        return Vector3.Distance(a.Centroid(), b.Centroid());
    }

    private static float SegmentVisibilityPenalty(Vector3 a, Vector3 b, Vector3 player, LayerMask obstacleMask, float penalty) {
        Vector3 m = (a + b) * 0.5f;
        Vector3 q1 = Vector3.Lerp(a, b, 0.25f);
        Vector3 q2 = Vector3.Lerp(a, b, 0.75f);

        int visibleSamples = 0;
        if (!Physics2D.Linecast(player, q1, obstacleMask)) visibleSamples++;
        if (!Physics2D.Linecast(player, m, obstacleMask)) visibleSamples++;
        if (!Physics2D.Linecast(player, q2, obstacleMask)) visibleSamples++;

       
        return visibleSamples * penalty;
    }

    
    private static float EvaluateNodeWithProfile(TriNode node, TacticalGraphBuilder graph, TacticalProfiles.NPCProfile profile, float radius) {
        float score = 0f;
        if (graph.tacticalLocations == null) return 0f;

        Vector3 c = node.Centroid();

        foreach (var tl in graph.tacticalLocations) {
            float d = Vector3.Distance(c, tl.position);
            if (d > radius) continue;

           
            float falloff = Mathf.Pow(Mathf.Clamp01(1f - (d / radius)), 2f);

            foreach (var kv in profile.weights) {
                if (tl.qualities.TryGetValue(kv.Key, out float q)) {
                    float impact = kv.Value * q * falloff;
                   
                    score += Mathf.Clamp(impact, -40f, +40f);
                }
            }
        }
        return score;
    }

   
    private static float EvaluateEdgeWithProfile(Vector3 a, Vector3 b, TacticalGraphBuilder graph, TacticalProfiles.NPCProfile profile, float radius) {
        if (graph.tacticalLocations == null) return 0f;

        float score = 0f;
        
        Vector3[] samples = {
            Vector3.Lerp(a, b, 0.2f),
            Vector3.Lerp(a, b, 0.5f),
            Vector3.Lerp(a, b, 0.8f)
        };

        foreach (var p in samples) {
            foreach (var tl in graph.tacticalLocations) {
                float d = Vector3.Distance(p, tl.position);
                if (d > radius) continue;

                float falloff = Mathf.Pow(Mathf.Clamp01(1f - (d / radius)), 2f);

                foreach (var kv in profile.weights) {
                    if (tl.qualities.TryGetValue(kv.Key, out float q)) {
                        float impact = kv.Value * q * falloff;
                        score += Mathf.Clamp(impact, -20f, +20f);
                    }
                }
            }
        }
        return score;
    }
}
