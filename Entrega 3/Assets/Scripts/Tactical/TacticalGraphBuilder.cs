using UnityEngine;
using System.Collections.Generic;

public class TacticalGraphBuilder : MonoBehaviour {
    [Header("Configuración")]
    public Transform player; 
    public float connectionRadius = 5f; 
    public NavMeshGraphBuilder navMeshGraph; 
    public LayerMask obstacleMask; 

    [Header("Visualización del grafo táctico")]
    public bool showTacticalNodes = true;
    public bool showTacticalConnections = true;
    public bool showNavMeshLinks = true;

    public class TacticalLocation {
        public Vector3 position;
        public Dictionary<string, float> qualities = new Dictionary<string, float>();
        public List<TacticalLocation> neighbors = new List<TacticalLocation>();
        public TriNode closestTriNode;

        public TacticalLocation(Vector3 pos) {
            position = pos;
            qualities["curacion"] = 0f;
            qualities["coberturaFija"] = 0f;
            qualities["patrulla"] = 0f;
            qualities["alarma"] = 0f;
            qualities["potenciador"] = 0f;
            qualities["coberturaRelativa"] = 0f;
            qualities["visiblePorJugador"] = 0f;
        }
    }

    public List<TacticalLocation> tacticalLocations = new List<TacticalLocation>();

    void Start() {
        TacticalLocationMarker[] markers = FindObjectsOfType<TacticalLocationMarker>();

        foreach (var marker in markers) {
            TacticalLocation loc = new TacticalLocation(marker.transform.position);

            // Asignamos las cualidades
            loc.qualities["curacion"] = marker.curacion ? 1f : 0f;
            loc.qualities["coberturaFija"] = marker.coberturaFija ? 1f : 0f;
            loc.qualities["patrulla"] = marker.patrulla ? 1f : 0f;
            loc.qualities["alarma"] = marker.alarma ? 1f : 0f;
            loc.qualities["potenciador"] = marker.potenciador ? 1f : 0f;

            // Buscamos el triangulo mas cercano
            if (navMeshGraph != null && navMeshGraph.triangles != null) {
                TriNode closestTri = null;
                float minDist = float.MaxValue;
                foreach (var tri in navMeshGraph.triangles) {
                    Vector3 centroid = tri.Centroid();
                    float dist = Vector3.Distance(marker.transform.position, centroid);

                    if (dist < minDist && !Physics2D.Linecast(marker.transform.position, centroid, obstacleMask)) {
                        minDist = dist;
                        closestTri = tri;
                    }
                }
                loc.closestTriNode = closestTri;
            }

            tacticalLocations.Add(loc);

            // Referencia al marker
            marker.generatedLocation = loc;
            //Debug.Log("Asignado TacticalLocation a marker: " + marker.name);
        }

        // Conectamos los nodos tacticos entre si
        foreach (var loc in tacticalLocations) {
            foreach (var other in tacticalLocations) {
                if (loc == other) continue;
                if (Vector3.Distance(loc.position, other.position) <= connectionRadius) {
                    if (!Physics2D.Linecast(loc.position, other.position, obstacleMask)) {
                        loc.neighbors.Add(other);
                    }
                }
            }
        }
    }

    void Update() {
        if (player != null) {
            UpdateDynamicQualities(player);
        }
    }

    public void UpdateDynamicQualities(Transform player) {
        foreach (var loc in tacticalLocations) {
            Vector3 dir = (player.position - loc.position).normalized;
            float dist = Vector3.Distance(loc.position, player.position);

            // Cobertura relativa (obstaculo entre el loc y el jugador)
            RaycastHit2D hitRel = Physics2D.Raycast(loc.position, dir, dist, obstacleMask);
            loc.qualities["coberturaRelativa"] = (hitRel.collider != null) ? 1f : 0f;

            // Visibilidad directa desde el jugador hacia el nodo
            RaycastHit2D hitVis = Physics2D.Linecast(player.position, loc.position, obstacleMask);
            loc.qualities["visiblePorJugador"] = (hitVis.collider == null) ? 1f : 0f; // 1 si el jugador lo ve
        }
    }


    void OnDrawGizmos() {
        if (tacticalLocations == null) return;

        foreach (var loc in tacticalLocations) {
            if (showTacticalNodes) {
                if (loc.qualities["curacion"] > 0) Gizmos.color = Color.green;
                else if (loc.qualities["coberturaFija"] > 0) Gizmos.color = Color.blue;
                else if (loc.qualities["alarma"] > 0) Gizmos.color = Color.red;
                else if (loc.qualities["patrulla"] > 0) Gizmos.color = Color.yellow;
                else if (loc.qualities["potenciador"] > 0) Gizmos.color = Color.black;
                else Gizmos.color = Color.white;

                Gizmos.DrawSphere(loc.position, 0.3f);
            }

            if (showTacticalConnections) {
                Gizmos.color = Color.gray;
                foreach (var neighbor in loc.neighbors) {
                    Gizmos.DrawLine(loc.position, neighbor.position);
                }
            }

            if (showNavMeshLinks && loc.closestTriNode != null) {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(loc.position, loc.closestTriNode.Centroid());
            }
        }
    }
}
