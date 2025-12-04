using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TacticalPathfinder {
    public class NPCProfile {
        public Dictionary<string, float> weights = new Dictionary<string, float>();
        public float distanciaSegura = 5f;
        public float umbralAlarmaCercana = 3f;
    }

    public static NPCProfile GuardiaProfile() {
        return new NPCProfile {
            weights = new Dictionary<string, float> {
                {"curacion", 0f},
                {"coberturaFija", -2f},
                {"alarma", -4f},
                {"coberturaRelativa", -2f},
                {"visiblePorJugador", +5f} 
            },
            distanciaSegura = 10f,
            umbralAlarmaCercana = 10f
        };
    }

    public static NPCProfile DronProfile() {
        return new NPCProfile {
            weights = new Dictionary<string, float> {
                {"curacion", +1f},
                {"coberturaFija", +4f},
                {"alarma", -2f},
                {"coberturaRelativa", 0f}
            }
        };
    }

    public static NPCProfile JefeProfile() {
        return new NPCProfile {
            weights = new Dictionary<string, float> {
                {"curacion", -4f},
                {"coberturaFija", -4f},
                {"alarma", -1f},
                {"coberturaRelativa", -4f}
            }
        };
    }

    // 🔹 Cálculo de costo táctico entre TacticalLocations
    public static float CalculateTacticalCost(
        TacticalGraphBuilder.TacticalLocation a,
        TacticalGraphBuilder.TacticalLocation b,
        NPCProfile profile) 
    {
        float D = Vector3.Distance(a.position, b.position);

        float tacticalCost = 0f;
        foreach (var kvp in profile.weights) {
            string quality = kvp.Key;
            if (!a.qualities.ContainsKey(quality) || !b.qualities.ContainsKey(quality)) continue;

            float avgValue = (a.qualities[quality] + b.qualities[quality]) / 2f;
            tacticalCost += kvp.Value * avgValue;
        }

        return D + tacticalCost;
    }


    private static float Heuristic(Vector3 a, Vector3 b) {
        return Vector3.Distance(a, b);
    }


    public static List<Vector3> FindPath(
        TacticalGraphBuilder.TacticalLocation start,
        TacticalGraphBuilder.TacticalLocation goal,
        NPCProfile profile,
        NavMeshGraphBuilder navMeshGraph,
        List<TacticalGraphBuilder.TacticalLocation> tacticalLocations) 
    {
        var frontier = new PriorityQueue<object>();
        frontier.Enqueue(start, 0);

        var cameFrom = new Dictionary<object, object>();
        var costSoFar = new Dictionary<object, float>();
        costSoFar[start] = 0;

        while (frontier.Count > 0) {
            var current = frontier.Dequeue();

            if (current == goal) break;

            if (current is TacticalGraphBuilder.TacticalLocation loc) {
                // Vecinos tácticos directos
                foreach (var neighbor in loc.neighbors) {
                    float newCost = costSoFar[current] + CalculateTacticalCost(loc, neighbor, profile);
                    UpdateFrontier(frontier, cameFrom, costSoFar, current, neighbor, newCost, goal.position);
                }

                // Conexión al NavMesh
                if (loc.closestTriNode != null) {
                    foreach (var triNeighbor in loc.closestTriNode.neighbors) {
                        float newCost = costSoFar[current] + Vector3.Distance(loc.position, triNeighbor.Centroid());
                        UpdateFrontier(frontier, cameFrom, costSoFar, current, triNeighbor, newCost, goal.position);
                    }
                }
            }
            else if (current is TriNode tri) {
                // Vecinos del NavMesh
                foreach (var triNeighbor in tri.neighbors) {
                    float newCost = costSoFar[current] + Vector3.Distance(tri.Centroid(), triNeighbor.Centroid());
                    UpdateFrontier(frontier, cameFrom, costSoFar, current, triNeighbor, newCost, goal.position);
                }

                // Conexión a TacticalLocations que usan este TriNode
                foreach (var tactical in tacticalLocations.Where(t => t.closestTriNode == tri)) {
                    float newCost = costSoFar[current] + Vector3.Distance(tri.Centroid(), tactical.position);
                    UpdateFrontier(frontier, cameFrom, costSoFar, current, tactical, newCost, goal.position);
                }
            }
        }


        List<Vector3> path = new List<Vector3>();
        object node = goal;
        while (node != start) {
            if (node is TacticalGraphBuilder.TacticalLocation tl) path.Add(tl.position);
            else if (node is TriNode tri) path.Add(tri.Centroid());

            if (!cameFrom.ContainsKey(node)) break;
            node = cameFrom[node];
        }
        if (start is TacticalGraphBuilder.TacticalLocation st) path.Add(st.position);
        path.Reverse();

        return path;
    }

    public static List<Vector3> FindPartialPath(
        TacticalGraphBuilder.TacticalLocation start,
        TacticalGraphBuilder.TacticalLocation goal,
        NPCProfile profile,
        NavMeshGraphBuilder navMeshGraph,
        List<TacticalGraphBuilder.TacticalLocation> tacticalLocations,
        Vector3 playerPos,
        int maxSteps = 5
    ){
        var frontier = new PriorityQueue<object>();
        frontier.Enqueue(start, 0);

        var cameFrom = new Dictionary<object, object>();
        var costSoFar = new Dictionary<object, float>();
        costSoFar[start] = 0;

        int steps = 0;
        object current = start;

        while (frontier.Count > 0 && steps < maxSteps) {
            current = frontier.Dequeue();
            steps++;

            if (current == goal) break;

            if (current is TacticalGraphBuilder.TacticalLocation loc) {
                foreach (var neighbor in loc.neighbors) {
                    // Poda dura: si el vecino es cobertura y visible por el jugador → descartar
                    if (IsVisibleCover(neighbor)) continue;

                    float newCost = costSoFar[current] + CalculateTacticalCost(loc, neighbor, profile);

                    // Penalizador orientado al objetivo: si moverte a neighbor te aleja del goal y además es visible → penaliza fuerte
                    float dCur = Vector3.Distance(loc.position, goal.position);
                    float dNxt = Vector3.Distance(neighbor.position, goal.position);
                    if (dNxt > dCur && IsVisibleForPlayer(neighbor, playerPos)) {
                        newCost += 100f; // penalización fuerte; ajusta según mapa
                    }

                    UpdateFrontier(frontier, cameFrom, costSoFar, current, neighbor, newCost, goal.position);
                }

                if (loc.closestTriNode != null) {
                    foreach (var triNeighbor in loc.closestTriNode.neighbors) {
                        float newCost = costSoFar[current] + Vector3.Distance(loc.position, triNeighbor.Centroid());
                        UpdateFrontier(frontier, cameFrom, costSoFar, current, triNeighbor, newCost, goal.position);
                    }
                }
            }
            else if (current is TriNode tri) {
                foreach (var triNeighbor in tri.neighbors) {
                    float newCost = costSoFar[current] + Vector3.Distance(tri.Centroid(), triNeighbor.Centroid());
                    UpdateFrontier(frontier, cameFrom, costSoFar, current, triNeighbor, newCost, goal.position);
                }

                foreach (var tactical in tacticalLocations.Where(t => t.closestTriNode == tri)) {
                    // Poda dura para tácticos visibles de cobertura
                    if (IsVisibleCover(tactical)) continue;

                    float newCost = costSoFar[current] + Vector3.Distance(tri.Centroid(), tactical.position);

                    float dCur = Vector3.Distance(tri.Centroid(), goal.position);
                    float dNxt = Vector3.Distance(tactical.position, goal.position);
                    if (dNxt > dCur && IsVisibleForPlayer(tactical, playerPos)) {
                        newCost += 100f;
                    }

                    UpdateFrontier(frontier, cameFrom, costSoFar, current, tactical, newCost, goal.position);
                }
            }
        }

        List<Vector3> path = new List<Vector3>();
        while (current != start) {
            path.Add(GetPosition(current));
            if (!cameFrom.ContainsKey(current)) break;
            current = cameFrom[current];
        }
        path.Add(GetPosition(start));
        path.Reverse();
        return path;
    }

    private static bool IsVisibleCover(TacticalGraphBuilder.TacticalLocation node) {
        // Descartamos nodos de cobertura fija visibles 
        return node.qualities.TryGetValue("coberturaFija", out float c) && c > 0.5f
            && node.qualities.TryGetValue("visiblePorJugador", out float v) && v > 0.5f;
    }

    private static bool IsVisibleForPlayer(TacticalGraphBuilder.TacticalLocation node, Vector3 playerPos) {
        // Usa cualidad si existe; si no, asume visible = 0 (no visible)
        return node.qualities.TryGetValue("visiblePorJugador", out float v) && v > 0.5f;
    }




    private static void UpdateFrontier(PriorityQueue<object> frontier,
                                       Dictionary<object, object> cameFrom,
                                       Dictionary<object, float> costSoFar,
                                       object current, object neighbor,
                                       float newCost, Vector3 goalPos) 
    {
        if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor]) {
            costSoFar[neighbor] = newCost;
            float priority = newCost + Heuristic(GetPosition(neighbor), goalPos);
            frontier.Enqueue(neighbor, priority);
            cameFrom[neighbor] = current;
        }
    }

    private static Vector3 GetPosition(object node) {
        if (node is TacticalGraphBuilder.TacticalLocation tl) return tl.position;
        if (node is TriNode tri) return tri.Centroid();
        return Vector3.zero;
    }
}


public class PriorityQueue<T> {
    private List<(T item, float priority)> elements = new List<(T, float)>();
    public int Count => elements.Count;

    public void Enqueue(T item, float priority) {
        elements.Add((item, priority));
    }

    public T Dequeue() {
        int bestIndex = 0;
        for (int i = 0; i < elements.Count; i++) {
            if (elements[i].priority < elements[bestIndex].priority) {
                bestIndex = i;
            }
        }
        T bestItem = elements[bestIndex].item;
        elements.RemoveAt(bestIndex);
        return bestItem;
    }
}
