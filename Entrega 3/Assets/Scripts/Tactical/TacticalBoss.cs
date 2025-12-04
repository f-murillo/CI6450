using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum TacticalBossState { Patrol, TakeCover }

[RequireComponent(typeof(NPCMove))]
public class TacticalBoss : MonoBehaviour {
    [Header("Referencias")]
    public TacticalGraphBuilder tacticalGraph;
    public NavMeshGraphBuilder navMeshGraph;
    public Transform jugador;
    public LayerMask obstacleMask;

    private TacticalProfiles.NPCProfile profile;
    private NPCMove mover;

    private List<Vector3> currentPath;
    private int pathIndex;
    private float stuckTimer;
    private TacticalBossState currentState = TacticalBossState.Patrol;

    void Start() {
        mover = GetComponent<NPCMove>();
        profile = TacticalProfiles.JefeProfile(); 
        Debug.Log("Jefe en estado PATROL (inicio)");
    }

    void Update() {
        switch (currentState) {
            case TacticalBossState.Patrol:
                break;

            case TacticalBossState.TakeCover:
                float distToPlayer = Vector3.Distance(transform.position, jugador.position);

                // Si el jugador se acerca demasiado, se recalcula otra cobertura
                if (distToPlayer < profile.distanciaSegura) {
                    var newCover = PickBestCoverAvoidingPlayer(transform.position, jugador.position, obstacleMask);
                    if (newCover != null) {
                        DoNavFullPathTowards(newCover.position);
                        FollowPath(newCover.position);
                    }
                } else {
                    var cover = PickBestCoverAvoidingPlayer(transform.position, jugador.position, obstacleMask);
                    if (cover != null) {
                        DoNavFullPathTowards(cover.position);
                        FollowPath(cover.position);
                    }
                }
                break;
        }

        EvaluateTransitions();
    }

    // Para calcular el camino
    void DoNavFullPathTowards(Vector3 goalPos) {
        if (currentPath != null && pathIndex < currentPath.Count) return;

        var startTri = navMeshGraph.FindClosestTriNode(transform.position);
        var goalTri = navMeshGraph.FindClosestTriNode(goalPos);

        currentPath = NavTacticalPathfinder.FindNavFullPath(
            startTri, goalTri, tacticalGraph, profile,
            jugador.position, obstacleMask,
            visiblePenalty: 10f
        );

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

        // Se busca un nodo mas cercano al objetivo
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

        mover.MoveTowards(target);
        float after = Vector3.Distance(transform.position, target);

        if (after < 0.6f) {
            pathIndex++;
            if (pathIndex >= currentPath.Count) currentPath = null;
        }

        // En caso de que se atasque
        if (before - after < 0.01f) {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 1.0f) {
                Debug.LogWarning("Jefe atascado, recalculando camino completo");
                DoNavFullPathTowards(goalPos);
                stuckTimer = 0f;
            }
        } else {
            stuckTimer = 0f;
        }
    }

    void EvaluateTransitions() {
        bool seesPlayer = !Physics2D.Linecast(transform.position, jugador.position, obstacleMask);
        float distToPlayer = Vector3.Distance(transform.position, jugador.position);

        if (seesPlayer && distToPlayer < profile.distanciaSegura * 2f) {
            if (currentState != TacticalBossState.TakeCover) {
                Debug.Log("TRANSICIÓN: PATROL → TAKE COVER (jugador detectado)");
                currentState = TacticalBossState.TakeCover;
                currentPath = null; pathIndex = 0;
            }
        }
        else if (!seesPlayer && currentState == TacticalBossState.TakeCover) {
            Debug.Log("TRANSICIÓN: TAKE COVER → PATROL (jugador perdido)");
            currentState = TacticalBossState.Patrol;
            currentPath = null; pathIndex = 0;
        }
    }

    // Para elegir la cobertura (fija o dinamica)
    private TacticalGraphBuilder.TacticalLocation PickBestCoverAvoidingPlayer(Vector3 npcPos, Vector3 playerPos, LayerMask obstacleMask) {
        TacticalGraphBuilder.TacticalLocation best = null;
        float bestScore = float.NegativeInfinity;

        foreach (var tl in tacticalGraph.tacticalLocations) {
            float score = 0f;

            if (tl.qualities.TryGetValue("coberturaFija", out float c)) score += c * 2f;
            if (tl.qualities.TryGetValue("coberturaDinamica", out float d)) score += d * 3f; 
            if (tl.qualities.TryGetValue("visiblePorJugador", out float v)) score -= v * 3f;

            float distNpc = Vector3.Distance(npcPos, tl.position);
            float distPlayer = Vector3.Distance(playerPos, tl.position);

            // Evitamos coberturas que necesiten pasar por donde esta el jugador
            bool crossesPlayer = !Physics2D.Linecast(npcPos, tl.position, obstacleMask) &&
                                 DistancePointToSegment2D(playerPos, npcPos, tl.position) < 1.0f;
            if (crossesPlayer) continue;

            score -= distNpc * 0.5f;   // cercanas al NPC
            score += distPlayer * 0.5f; // lejanas al jugador

            if (score > bestScore) {
                bestScore = score;
                best = tl;
            }
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
        Gizmos.color = Color.red;
        for (int i = 0; i < currentPath.Count - 1; i++) {
            Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
        }
    }
}
