using UnityEngine;

public class TacticalLocationMarker : MonoBehaviour {
    [Header("Cualidades estáticas")]
    public bool curacion;
    public bool coberturaFija;
    public bool patrulla;
    public bool alarma;
    public bool potenciador;
    [HideInInspector]
    public TacticalGraphBuilder.TacticalLocation generatedLocation;
}
