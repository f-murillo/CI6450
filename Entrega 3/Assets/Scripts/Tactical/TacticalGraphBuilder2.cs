using UnityEngine;
using System.Collections.Generic;

public class TacticalGraphBuilder2 : MonoBehaviour {
    [Header("Configuración")]
    public Transform player; 
    public float connectionRadius = 5f; 
    public NavMeshGraphBuilder navMeshGraph; 
    public LayerMask obstacleMask; 

    [Header("Cobertura dinámica")]
    public float visionAngle = 60f;       
    public int rayCount = 10;             
    public float visionDistance = 10f;    
    public float dynamicNodeThreshold = 0.5f; 
    public float offsetBehindObstacle = 1f;  

    [Header("Visualización del grafo táctico")]
    public bool showTacticalNodes = true;
    public bool showTacticalConnections = true;
    public bool showNavMeshLinks = true;

    public class TacticalLocation2 {
        public Vector3 position;
        public Dictionary<string, float> qualities = new Dictionary<string, float>();
        public List<TacticalLocation2> neighbors = new List<TacticalLocation2>();
        public TriNode closestTriNode;

        public TacticalLocation2(Vector3 pos) {
            position = pos;
            qualities["curacion"] = 0f;
            qualities["coberturaFija"] = 0f;
            qualities["patrulla"] = 0f;
            qualities["alarma"] = 0f;
            qualities["potenciador"] = 0f;
            qualities["coberturaRelativa"] = 0f;
            qualities["coberturaDinamica"] = 0f;
            qualities["visiblePorJugador"] = 0f;
        }
    }

    public List<TacticalLocation2> tacticalLocations = new List<TacticalLocation2>();

    void Start() {
        TacticalLocationMarker[] markers = FindObjectsOfType<TacticalLocationMarker>();

        foreach (var marker in markers) {
            TacticalLocation2 loc = new TacticalLocation2(marker.transform.position);

            // Asignamos las cualidades estaticas
            loc.qualities["curacion"] = marker.curacion ? 1f : 0f;
            loc.qualities["coberturaFija"] = marker.coberturaFija ? 1f : 0f;
            loc.qualities["patrulla"] = marker.patrulla ? 1f : 0f;
            loc.qualities["alarma"] = marker.alarma ? 1f : 0f;
            loc.qualities["potenciador"] = marker.potenciador ? 1f : 0f;

            // Buscamos el triangulo mas cercano
            loc.closestTriNode = FindClosestTri(marker.transform.position);

            tacticalLocations.Add(loc);
            //marker.generatedLocation = loc;
        }

        ConnectLocations();
    }

    void Update() {
        if (player != null) {
            UpdateDynamicQualities(player);
            GenerateDynamicCoverNodes(player);
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
            loc.qualities["visiblePorJugador"] = (hitVis.collider == null) ? 1f : 0f;
        }
    }

    // Nodos dinamicos detras de un obstaculo
    public void GenerateDynamicCoverNodes(Transform player) {
        tacticalLocations.RemoveAll(loc => loc.qualities["coberturaDinamica"] > 0f);

        Vector3 forward = player.up; 
        for (int i = 0; i < rayCount; i++) {
            float angle = -visionAngle/2f + (visionAngle/(rayCount-1)) * i;
            Vector3 dir = Quaternion.Euler(0,0,angle) * forward;

            RaycastHit2D hit = Physics2D.Raycast(player.position, dir, visionDistance, obstacleMask);
            if (hit.collider != null) {
                // Punto detras del obstaculo
                Vector3 coverPos = (Vector3)hit.point - (Vector3)hit.normal * offsetBehindObstacle;

                if (Physics2D.Linecast(player.position, coverPos, obstacleMask).collider == null) continue;

                // Evitar duplicados
                if (tacticalLocations.Exists(loc => Vector3.Distance(loc.position, coverPos) < dynamicNodeThreshold)) continue;

                // Evitar que quede dentro de otro obstaculo
                if (Physics2D.OverlapPoint(coverPos, obstacleMask) != null) continue;

                TacticalLocation2 dynLoc = new TacticalLocation2(coverPos);
                dynLoc.qualities["coberturaDinamica"] = 1f;

                // Buscar triangulo mas cercano
                dynLoc.closestTriNode = FindClosestTri(coverPos);

                tacticalLocations.Add(dynLoc);

                // Debug visual
                //Debug.DrawLine(player.position, coverPos, Color.magenta);
            }
        }

        ConnectLocations();
    }

    private TriNode FindClosestTri(Vector3 pos) {
        if (navMeshGraph == null || navMeshGraph.triangles == null) return null;
        TriNode closestTri = null;
        float minDist = float.MaxValue;
        foreach (var tri in navMeshGraph.triangles) {
            Vector3 centroid = tri.Centroid();
            float dist = Vector3.Distance(pos, centroid);
            if (dist < minDist && !Physics2D.Linecast(pos, centroid, obstacleMask)) {
                minDist = dist;
                closestTri = tri;
            }
        }
        return closestTri;
    }

    private void ConnectLocations() {
        foreach (var loc in tacticalLocations) {
            loc.neighbors.Clear();
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

    void OnDrawGizmos() {
        if (tacticalLocations == null) return;

        foreach (var loc in tacticalLocations) {
            if (showTacticalNodes) {
                if (loc.qualities["curacion"] > 0) Gizmos.color = Color.green;
                else if (loc.qualities["coberturaFija"] > 0) Gizmos.color = Color.blue;
                else if (loc.qualities["alarma"] > 0) Gizmos.color = Color.red;
                else if (loc.qualities["patrulla"] > 0) Gizmos.color = Color.yellow;
                else if (loc.qualities["potenciador"] > 0) Gizmos.color = Color.black;
                else if (loc.qualities["coberturaDinamica"] > 0) Gizmos.color = Color.magenta;
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
