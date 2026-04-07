using UnityEngine;

/// <summary>
/// URPMaterialFixer.cs
///
/// Corrige automáticamente todos los materiales que se ponen ROSADOS/MAGENTA
/// en proyectos URP (Universal Render Pipeline).
///
/// El problema: los objetos que referencian el material por defecto de Unity
/// ("Standard" shader) se vuelven rosados porque ese shader no es compatible
/// con URP. Los botones ya se corrigen solos en GazeTimerButton(Mouse).cs;
/// este script corrige el piso y cualquier otro objeto estático.
///
/// Uso: agrega un GameObject vacío llamado "URPFixer" a la escena y
/// adjúntale este componente. Se ejecuta una sola vez en Start().
/// </summary>
public class URPMaterialFixer : MonoBehaviour
{
    [Tooltip("Color base para el piso y otros objetos sin material asignado.")]
    public Color floorColor = new Color(0.45f, 0.45f, 0.45f, 1f);   // gris neutro

    void Start()
    {
        // Orden de preferencia de shaders URP
        Shader target = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Standard");

        if (target == null)
        {
            Debug.LogWarning("[URPMaterialFixer] No se encontró ningún shader compatible.");
            return;
        }

        int fixedCount = 0;

        // Recorre TODOS los Renderer de la escena en el momento de Start()
        // (los botones ya pusieron sus materiales URP en Awake)
        foreach (Renderer r in FindObjectsOfType<Renderer>())
        {
            // ── Omitir botones y sus hijos (ellos manejan sus materiales) ────
            if (r.GetComponentInParent<GazeTimerButton>()      != null) continue;
            if (r.GetComponentInParent<GazeTimerButtonMouse>() != null) continue;

            // ── Revisar cada slot de material ────────────────────────────────
            // r.materials devuelve instancias (no modifica shared materials)
            Material[] mats = r.materials;
            bool changed = false;

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;

                string sName = mats[i].shader.name;

                // Si el shader NO es URP ni Skybox → está roto en URP → reemplazar
                if (!sName.Contains("Universal Render Pipeline") &&
                    !sName.Contains("Skybox"))
                {
                    var newMat  = new Material(target);
                    newMat.name = mats[i].name + "_URP_Fixed";
                    newMat.color = floorColor;
                    mats[i]     = newMat;
                    changed     = true;
                }
            }

            if (changed)
            {
                r.materials = mats;
                fixedCount++;
                Debug.Log($"[URPMaterialFixer] Material corregido en: {r.gameObject.name}");
            }
        }

        if (fixedCount == 0)
            Debug.Log("[URPMaterialFixer] Todos los materiales ya son compatibles con URP.");
        else
            Debug.Log($"[URPMaterialFixer] {fixedCount} material(es) corregido(s).");
    }
}
