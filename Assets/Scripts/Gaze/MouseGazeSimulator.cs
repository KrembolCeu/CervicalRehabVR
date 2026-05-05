using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class MouseGazeSimulator : MonoBehaviour
{
    [Header("Settings")]
    public float maxDistance = 100f;
    public LayerMask gazeLayer = ~0;
    public float selectTime = 2f;

    [Header("Debug")]
    public bool showRayInEditor = true;

    private IGazeable _current;
    private float _gazeTimer;
    private Camera _cam;

    void Start()
    {


        if (UnityEngine.XR.XRSettings.enabled)
        {
            enabled = false;
            return;
        }

        _cam = GetComponent<UnityEngine.Camera>();
        if (_cam == null) _cam = Camera.main;

        var gs = GetComponent<GazeSelector>();
        if (gs != null) gs.enabled = false;


        if (DifficultyConfig.Instance != null && DifficultyConfig.Instance.Current != null)
            selectTime = DifficultyConfig.Instance.Current.gazeSelectTime;
    }

    void Update()
    {
        if (_cam == null) return;
        if (Mouse.current == null) return;

        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (showRayInEditor)
            Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.cyan);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, gazeLayer))
        {
            var gazeable = hit.collider.GetComponentInParent<IGazeable>();
            if (gazeable != null)
            {
                if (gazeable != _current)
                {
                    ExitCurrent();
                    _current = gazeable;
                    _gazeTimer = 0f;
                    _current.OnGazeEnter();
                }

                _gazeTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(_gazeTimer / selectTime);
                _current.OnGazeStay(progress);

                if (_gazeTimer >= selectTime)
                {
                    _current.OnGazeSelect();
                    _gazeTimer = 0f;
                }
                return;
            }
        }

        ExitCurrent();
    }

    void ExitCurrent()
    {
        if (_current == null) return;
        _current.OnGazeExit();
        _current = null;
        _gazeTimer = 0f;
    }
}
