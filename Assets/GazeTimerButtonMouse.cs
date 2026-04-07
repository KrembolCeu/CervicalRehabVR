using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GazeTimerButtonMouse.cs  — versión MOUSE para probar en el Editor
///
/// Idéntica a GazeTimerButton en lógica y feedback visual.
/// Solo cambia la detección: usa el cursor del ratón en lugar del HMD.
///
/// Para probar:
///   1. Abre "VR Prueba Mouse" o "NivelJuego Mouse".
///   2. Dale Play en el Editor.
///   3. Mueve el cursor encima de un cubo.
///   4. La barra de progreso se llena durante 3 segundos → confirma.
/// </summary>
public class GazeTimerButtonMouse : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Mouse / Gaze Settings")]
    [Tooltip("Segundos con cursor encima para confirmar")]
    public float gazeTimeRequired = 3f;

    [Header("Colores del cubo")]
    public Color normalColor    = new Color(0.20f, 0.50f, 1.00f, 1f);  // azul
    public Color gazeColor      = new Color(1.00f, 0.80f, 0.00f, 1f);  // amarillo
    public Color activatedColor = new Color(0.00f, 0.90f, 0.30f, 1f);  // verde

    [Header("Colores de la barra de progreso")]
    public Color barStartColor  = new Color(1.00f, 0.55f, 0.00f, 1f);  // naranja brillante
    public Color barEndColor    = new Color(0.00f, 1.00f, 0.30f, 1f);  // verde claro

    [Header("Acción al confirmar")]
    [Tooltip("Escena a cargar. Vacío = no carga escena.")]
    public string targetScene  = "";
    [Tooltip("Si es true, carga 'VR Prueba Mouse' (botón Regresar).")]
    public bool   isBackButton = false;
    [Tooltip("Dificultad: Facil | Medio | Dificil. Vacío = no guarda.")]
    public string difficulty   = "";

    // ── Private ───────────────────────────────────────────────────────────────

    private float    _gazeTimer;
    private bool     _confirmed;
    private bool     _wasOver;      // para debug: detectar cambio de estado

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

    const float BAR_W  =  0.88f;   // ancho (casi todo el cubo)
    const float BAR_H  =  0.18f;   // alto  — más gruesa para que se vea bien
    const float BAR_Y  = -0.35f;   // posición Y (zona baja del cubo, pero no en el borde)
    const float BAR_Z  = -0.60f;   // bien por delante de la cara frontal del cubo
    const float FILL_Z = -0.62f;   // fill un poco más al frente que el fondo

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

        bool over = IsMouseOver();

        // ── Debug: log solo cuando cambia el estado ──────────────────────────
        if (over != _wasOver)
        {
            _wasOver = over;
            if (over)
                Debug.Log($"[GazeButton] HOVER DETECTADO en '{gameObject.name}'");
            else
                Debug.Log($"[GazeButton] cursor salió de '{gameObject.name}'");
        }

        if (over)
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

    // ── Detección con mouse ───────────────────────────────────────────────────

    bool IsMouseOver()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out RaycastHit hit, 100f) && hit.transform == transform;
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

        string scene = isBackButton ? "VR Prueba Mouse" : targetScene;
        if (!string.IsNullOrEmpty(scene))
            StartCoroutine(LoadAfterDelay(scene, 0.5f));
        else
            Debug.Log($"[MouseButton] '{gameObject.name}' confirmado (sin escena asignada).");
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

        // Relleno
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

        ShowBar(false);
    }

    void UpdateBar(float t)
    {
        float fillW   = Mathf.Max(0.001f, t * BAR_W);
        float centerX = -BAR_W * 0.5f + fillW * 0.5f;

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
