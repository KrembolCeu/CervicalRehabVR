using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GazeTimerButton.cs  — versión VR (ray desde HMD / Camera.main forward)
///
/// Feedback visual al mirar el botón:
///   1. Cubo cambia de color (normal → amarillo) con pulso de escala.
///   2. Barra de progreso 3D en la cara frontal del cubo se llena de
///      izquierda a derecha durante <gazeTimeRequired> segundos.
///      La barra interpola de cian → verde al completarse.
///   3. Al confirmar: cubo verde → espera 0.5 s → carga escena.
/// </summary>
public class GazeTimerButton : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Gaze Settings")]
    [Tooltip("Segundos mirando para confirmar la acción")]
    public float gazeTimeRequired = 3f;

    [Header("Colores del cubo")]
    public Color normalColor    = new Color(0.20f, 0.50f, 1.00f, 1f);  // azul
    public Color gazeColor      = new Color(1.00f, 0.80f, 0.00f, 1f);  // amarillo
    public Color activatedColor = new Color(0.00f, 0.90f, 0.30f, 1f);  // verde

    [Header("Colores de la barra de progreso")]
    public Color barStartColor  = new Color(0.00f, 0.85f, 1.00f, 1f);  // cian
    public Color barEndColor    = new Color(0.00f, 1.00f, 0.40f, 1f);  // verde claro

    [Header("Acción al confirmar")]
    [Tooltip("Escena a cargar. Vacío = no carga escena.")]
    public string targetScene  = "";
    [Tooltip("Si es true, carga 'VR Prueba' (botón Regresar).")]
    public bool   isBackButton = false;
    [Tooltip("Dificultad: Facil | Medio | Dificil. Vacío = no guarda.")]
    public string difficulty   = "";

    // ── Private ───────────────────────────────────────────────────────────────

    private float    _gazeTimer;
    private bool     _confirmed;

    // Cubo
    private Renderer _rend;
    private Material _cubeMat;
    private Vector3  _baseScale;

    // Shader compartido (se busca UNA sola vez en Awake)
    private Shader   _urpShader;

    // Barra de progreso
    private GameObject _barBg;
    private GameObject _barFill;
    private Material   _fillMat;

    // Dimensiones de la barra (en espacio LOCAL del cubo, escala 1)
    const float BAR_W    =  0.86f;   // ancho total de la barra
    const float BAR_H    =  0.07f;   // alto de la barra
    const float BAR_Y    = -0.42f;   // posición Y (parte inferior del cubo)
    const float BAR_Z    = -0.52f;   // justo frente a la cara frontal (-Z)
    const float FILL_Z   = -0.53f;   // fill ligeramente por delante del bg

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        _rend      = GetComponent<Renderer>();
        _baseScale = transform.localScale;

        // Buscar shader URP una sola vez; reutilizarlo en BuildProgressBar()
        _urpShader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color")
                  ?? Shader.Find("Standard");

        // Crear material propio con shader URP para evitar el color rosa
        _cubeMat = new Material(_urpShader);
        _rend.material = _cubeMat;

        SetCubeColor(normalColor);
        BuildProgressBar();
    }

    void Update()
    {
        if (_confirmed) return;

        bool gazed = IsBeingGazed();

        if (gazed)
        {
            _gazeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_gazeTimer / gazeTimeRequired);

            // Cubo
            SetCubeColor(Color.Lerp(normalColor, gazeColor, t));
            transform.localScale = _baseScale * (1f + 0.10f * Mathf.Sin(t * Mathf.PI));

            // Barra
            ShowBar(true);
            UpdateBar(t);

            if (_gazeTimer >= gazeTimeRequired)
                Confirm();
        }
        else
        {
            _gazeTimer = 0f;
            SetCubeColor(normalColor);
            transform.localScale = _baseScale;
            ShowBar(false);
        }
    }

    // ── Detección (VR: forward del HMD) ──────────────────────────────────────

    bool IsBeingGazed()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        return Physics.Raycast(ray, out RaycastHit hit, 50f) && hit.transform == transform;
    }

    // ── Confirmación ──────────────────────────────────────────────────────────

    void Confirm()
    {
        _confirmed = true;
        SetCubeColor(activatedColor);
        transform.localScale = _baseScale;

        // Barra completa en verde
        UpdateBar(1f);
        _fillMat.color = barEndColor;

        if (!string.IsNullOrEmpty(difficulty))
            DifficultyManager.SetDifficulty(difficulty);

        string scene = isBackButton ? "VR Prueba" : targetScene;
        if (!string.IsNullOrEmpty(scene))
            StartCoroutine(LoadAfterDelay(scene, 0.5f));
        else
            Debug.Log($"[GazeTimerButton] '{gameObject.name}' confirmado (sin escena asignada).");
    }

    IEnumerator LoadAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    // ── Barra de progreso ─────────────────────────────────────────────────────

    void BuildProgressBar()
    {
        // Reutilizar el shader ya encontrado en Awake()

        // Fondo oscuro
        _barBg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _barBg.name = "ProgressBar_BG";
        _barBg.transform.SetParent(transform);
        _barBg.transform.localPosition = new Vector3(0f, BAR_Y, BAR_Z);
        _barBg.transform.localScale    = new Vector3(BAR_W, BAR_H, 0.01f);

        // Desactivar el collider de inmediato (evita interferir con raycasts)
        var bgCol = _barBg.GetComponent<BoxCollider>();
        if (bgCol != null) bgCol.enabled = false;

        var bgMat = new Material(_urpShader);
        bgMat.color = new Color(0.08f, 0.08f, 0.08f, 1f);
        _barBg.GetComponent<Renderer>().material = bgMat;

        // Relleno (fill) — empieza con ancho casi cero
        _barFill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _barFill.name = "ProgressBar_Fill";
        _barFill.transform.SetParent(transform);
        _barFill.transform.localPosition = new Vector3(-BAR_W * 0.5f, BAR_Y, FILL_Z);
        _barFill.transform.localScale    = new Vector3(0.001f, BAR_H * 0.85f, 0.01f);

        // Desactivar el collider de inmediato
        var fillCol = _barFill.GetComponent<BoxCollider>();
        if (fillCol != null) fillCol.enabled = false;

        _fillMat = new Material(_urpShader);
        _fillMat.color = barStartColor;
        _barFill.GetComponent<Renderer>().material = _fillMat;

        // Oculta al inicio
        ShowBar(false);
    }

    void UpdateBar(float t)
    {
        float fillW   = Mathf.Max(0.001f, t * BAR_W);
        float centerX = -BAR_W * 0.5f + fillW * 0.5f;  // ancla al lado izquierdo

        _barFill.transform.localPosition = new Vector3(centerX, BAR_Y, FILL_Z);
        _barFill.transform.localScale    = new Vector3(fillW, BAR_H * 0.85f, 0.01f);

        _fillMat.color = Color.Lerp(barStartColor, barEndColor, t);
    }

    void ShowBar(bool show)
    {
        if (_barBg   != null) _barBg.SetActive(show);
        if (_barFill != null) _barFill.SetActive(show);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void SetCubeColor(Color c) => _cubeMat.color = c;

    public void ResetButton()
    {
        _confirmed = false;
        _gazeTimer = 0f;
        SetCubeColor(normalColor);
        transform.localScale = _baseScale;
        ShowBar(false);
    }
}
