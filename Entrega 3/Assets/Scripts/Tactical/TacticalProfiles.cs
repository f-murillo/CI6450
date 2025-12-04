using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TacticalProfiles : MonoBehaviour
{
    public class NPCProfile {
        public Dictionary<string, float> weights = new Dictionary<string, float>();
        public float distanciaSegura = 5f;
        public float umbralAlarmaCercana = 3f;
    }

    public static NPCProfile GuardiaProfile() {
        return new NPCProfile {
            weights = new Dictionary<string, float> {
                {"curacion", 0f},
                {"coberturaFija", -4f},
                {"alarma", -5f},
                {"coberturaRelativa", -4f},
                {"visiblePorJugador", +5f},
                {"potenciador", 0f} 
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
                {"coberturaRelativa", 0f},
                {"potenciador", 0f}
            }
        };
    }

    public static NPCProfile JefeProfile() {
        return new NPCProfile {
            weights = new Dictionary<string, float> {
                {"curacion", -4f},
                {"coberturaFija", -10f},
                {"alarma", 10f},
                {"coberturaRelativa", -5f},
                {"potenciador", -25f}
            }
        };
    }
}
