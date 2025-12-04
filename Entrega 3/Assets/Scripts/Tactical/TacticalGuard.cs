using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum TacticalGuardState { Patrol, GoToAlarm, TakeCover }

public class TacticalGuard : MonoBehaviour {
    [Header("Referencias")]
    public TacticalGraphBuilder tacticalGraph;
    public NavMeshGraphBuilder navMeshGraph;
    public Transform jugador;
    public TacticalLocationMarker alarmaMarker;
    public LayerMask obstacleMask;

    private TacticalGraphBuilder.TacticalLocation alarma;
    private TacticalProfiles.NPCProfile profile;

    private List<Vector3> currentPath;
    private int pathIndex;
    private float stuckTimer;
    private NPCMove mover;
    private TacticalGuardState currentState = TacticalGuardState.Patrol;
    private bool alerted = false;

    void Start() {
        mover = GetComponent<NPCMove>();
        profile = TacticalProfiles.GuardiaProfile();
        alarma = tacticalGraph.tacticalLocations
            .FirstOrDefault(loc => Vector3.Distance(loc.position, alarmaMarker.transform.position) < 0.1f);

        if (alarma == null) Debug.LogError("No se encontró TacticalLocation para la alarma.");
        else Debug.Log("Guardia en estado PATROL (inicio)");
    }

    void Update() {
        switch (currentState) {
            case TacticalGuardState.Patrol:
                break;

            case TacticalGuardState.GoToAlarm:
                DoNavFullPathTowards(alarma.position);
                FollowPath(alarma.position);

                if (alarma != null && Vector3.Distance(transform.position, alarma.position) < 1.0f) {
                    Debug.Log("Guardia alcanzó la alarma, alerta reseteada");
                    alerted = false;
                    currentState = TacticalGuardState.Patrol;
                    currentPath = null; pathIndex = 0;
                }
                break;

            case TacticalGuardState.TakeCover:
                var cover = PickFeasibleCover(transform.position, jugador.position, obstacleMask);
                if (cover != null) {
                    DoNavFullPathTowards(cover.position);
                    FollowPath(cover.position);
                } else {
                    var safeTri = PickNearestSafeTri(transform.position);
                    if (safeTri != null) {
                        DoNavFullPathTowards(safeTri.Centroid());
                        FollowPath(safeTri.Centroid());
                    }
                }
                break;
        }

        EvaluateTransitions();
    }

    // Para calcular el camino hacia un objetivo
    void DoNavFullPathTowards(Vector3 goalPos) {
        if (currentPath != null && pathIndex < currentPath.Count) return; // si ya hay un camino

        var startTri = navMeshGraph.FindClosestTriNode(transform.position);
        var goalTri = navMeshGraph.FindClosestTriNode(goalPos);

        var newPath = NavTacticalPathfinder.FindNavFullPath(
            startTri, goalTri, tacticalGraph, profile,
            jugador.position, obstacleMask,
            visiblePenalty: 8f
        );

        if (newPath == null || newPath.Count == 0) {
            Debug.LogWarning("Guardia: no se encontró camino hacia " + goalPos);
            return; // para evitar bucles infinitos
        }

        currentPath = newPath;
        pathIndex = 0;
        stuckTimer = 0f;
    }

    void FollowPath(Vector3 goalPos) {
        // Si hay linea de vision directa al objetivo
        if (!Physics2D.Linecast(transform.position, goalPos, obstacleMask)) {
            mover.MoveTowards(goalPos);
            return;
        }

        if (currentPath == null || pathIndex >= currentPath.Count) {
            DoNavFullPathTowards(goalPos);
            return;
        }

        // Buscar un nodo mas cercano al objetivo
        for (int i = currentPath.Count - 1; i > pathIndex; i--) {
            Vector3 candidate = currentPath[i];
            if (!Physics2D.Linecast(transform.position, candidate, obstacleMask)) {
                float distCandidateToGoal = Vector3.Distance(candidate, goalPos);
                float distCurrentToGoal = Vector3.Distance(currentPath[pathIndex], goalPos);

                if (distCandidateToGoal < distCurrentToGoal) {
                    pathIndex = i;
                    break;
                }
            }
        }

        Vector3 target = currentPath[pathIndex];
        float before = Vector3.Distance(transform.position, target);

        RaycastHit2D hit = Physics2D.Linecast(transform.position, target, obstacleMask);
        if (hit.collider != null) {
            Vector3 avoidance = (transform.position - (Vector3)hit.point).normalized * 0.5f;
            target += avoidance;
        }

        mover.MoveTowards(target);
        float after = Vector3.Distance(transform.position, target);

        if (after < 0.5f) {
            pathIndex++;
            if (pathIndex >= currentPath.Count) {
                currentPath = null;
            }
        }

        // En caso de que se atasque
        if (before - after < 0.01f) {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 1.0f) {
                Debug.LogWarning("Guardia atascado, recalculando camino completo");
                DoNavFullPathTowards(goalPos);
            }
        } else {
            stuckTimer = 0f;
        }
    }

    void EvaluateTransitions() {
        bool seesPlayer = !Physics2D.Linecast(transform.position, jugador.position, obstacleMask);
        float distToPlayer = Vector3.Distance(transform.position, jugador.position);
        bool playerNearAlarm = alarma != null &&
            Vector3.Distance(jugador.position, alarma.position) < profile.umbralAlarmaCercana;

        if (seesPlayer) {
            alerted = true;
        }

        if (currentState == TacticalGuardState.Patrol && alerted) {
            Debug.Log("TRANSICIÓN: PATROL → GO TO ALARM (alertado)");
            currentState = TacticalGuardState.GoToAlarm;
            currentPath = null; pathIndex = 0;
        }

        if (currentState == TacticalGuardState.GoToAlarm && distToPlayer < profile.distanciaSegura) {
            Debug.Log("TRANSICIÓN: GO TO ALARM → TAKE COVER (jugador cerca)");
            currentState = TacticalGuardState.TakeCover;
            currentPath = null; pathIndex = 0;
        }
        else if (currentState == TacticalGuardState.TakeCover && distToPlayer >= profile.distanciaSegura) {
            Debug.Log("TRANSICIÓN: TAKE COVER → GO TO ALARM (jugador lejos, sigue alerta)");
            currentState = TacticalGuardState.GoToAlarm;
            currentPath = null; pathIndex = 0;
        }
    }

    private TacticalGraphBuilder.TacticalLocation PickFeasibleCover(Vector3 npcPos, Vector3 playerPos, LayerMask obstacleMask) {
        TacticalGraphBuilder.TacticalLocation best = null;
        float bestScore = float.NegativeInfinity;

        foreach (var tl in tacticalGraph.tacticalLocations) {
            if (!tl.qualities.TryGetValue("coberturaFija", out float c) || c <= 0.5f) continue;
            if (tl.qualities.TryGetValue("visiblePorJugador", out float v) && v > 0.5f) continue;

            float dNpc = Vector3.Distance(npcPos, tl.position);
            float dPlayer = Vector3.Distance(playerPos, tl.position);

            bool crossesLos = !Physics2D.Linecast(playerPos, tl.position, obstacleMask) &&
                              DistancePointToSegment2D(playerPos, npcPos, tl.position) < 1.5f;
            if (crossesLos) continue;

            float score = -dNpc + dPlayer * 0.5f;
            if (score > bestScore) { bestScore = score; best = tl; }
        }
        return best;
    }

    TriNode PickNearestSafeTri(Vector3 pos) {
        TriNode best = null;
        float bestDist = float.MaxValue;
        foreach (var tri in navMeshGraph.triangles) {
            var c = tri.Centroid();
            if (!Physics2D.Linecast(jugador.position, c, obstacleMask)) continue;
            float d = Vector3.Distance(pos, c);
            if (d < bestDist) { bestDist = d; best = tri; }
        }
        return best;
    }

    private float DistancePointToSegment2D(Vector3 p, Vector3 a, Vector3 b) {
        Vector2 pa = (Vector2)(p - a);
        Vector2 ba = (Vector2)(b - a);
        float t = Mathf.Clamp(Vector2.Dot(pa, ba) / ba.sqrMagnitude, 0f, 1f);
        Vector2 proj = (Vector2)a + t * ba;
        return Vector2.Distance((Vector2)p, proj);
    }

    void OnDrawGizmos() {
        if (currentPath == null) return;
        Gizmos.color = Color.magenta;
        for (int i = 0; i < currentPath.Count - 1; i++) {
            Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
        }
    }
}
