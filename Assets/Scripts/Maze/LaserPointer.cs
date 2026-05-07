using UnityEngine;
using UnityEngine.InputSystem.XR;

[RequireComponent(typeof(LineRenderer))]
public class LaserPointer : MonoBehaviour
{
    [Header("References")]
    public Transform mazePlane;
    public Transform laserDot;
    public Transform exitStar;
    public Transform guideRing;
    public Texture2D mazeTexture;

    [Header("Movement")]
    [Tooltip("UV units per degree per second of head tilt")]
    public float sensitivity = 0.008f;
    [Tooltip("Degrees of tilt ignored to prevent drift")]
    public float deadZone    = 3f;

    [Header("Entry / Exit UV")]
    public float startU     = 0.037f;
    public float startV     = 0.970f;
    public float exitU      = 0.965f;
    public float exitV      = 0.035f;
    public float exitRadius = 0.035f;

    [Header("Grab to Navigate")]
    [Tooltip("Seconds the gaze must rest on the dot to grab it")]
    public float grabDwellTime = 1.5f;
    [Tooltip("UV-space radius around the dot that counts as 'looking at it'")]
    public float grabRadius    = 0.08f;

    [Header("Dot Colors")]
    public Color dotNormalColor  = new Color(1f, 0f,   0f,   1f);
    public Color dotWallColor    = new Color(1f, 0.9f, 0.1f, 1f);
    public Color dotUngrabColor  = new Color(1f, 0.5f, 0f,   1f);
    public Color dotDwellColor   = new Color(0f, 1f,   0f,   1f);

    [Header("Wall Detection")]
    [Range(0f, 1f)]
    public float wallThreshold = 0.45f;

    public bool IsOnWall  { get; private set; }
    public bool IsAtExit  { get; private set; }
    public bool IsGrabbed { get; private set; }

    float      _u, _v;
    Quaternion _calibRot;
    bool       _calibrated;
    Material   _dotMat;
    float      _dwellTimer;

    void Awake()
    {
        var lr = GetComponent<LineRenderer>();
        lr.enabled = false;

        if (laserDot != null)
        {
            var dr = laserDot.GetComponent<Renderer>();
            if (dr != null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Standard");
                _dotMat       = new Material(sh);
                _dotMat.color = dotUngrabColor;
                dr.material   = _dotMat;
            }
            laserDot.localScale = Vector3.one * 0.06f;
        }

        if (exitStar != null)
        {
            var sr = exitStar.GetComponent<Renderer>();
            if (sr != null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Standard");
                var sm = new Material(sh);
                sm.color = new Color(1f, 0.85f, 0f, 1f);
                sr.material = sm;
            }
            exitStar.localScale = Vector3.one * 0.06f;
        }
    }

    void Start()
    {
        ResetToStart();
        CalibrateCenter();
    }

    public void CalibrateCenter()
    {
        _calibRot   = ReadHeadRot();
        _calibrated = true;
    }

    public void ResetToStart()
    {
        _u          = startU;
        _v          = startV;
        IsGrabbed   = false;
        IsAtExit    = false;
        IsOnWall    = false;
        _dwellTimer = 0f;
        PositionDot();
        PositionStar();
        if (guideRing != null) guideRing.gameObject.SetActive(true);
    }

    public void SetMaze(Texture2D tex)
    {
        mazeTexture = tex;
        ResetToStart();
        CalibrateCenter();
    }

    void Update()
    {
        if (!_calibrated) return;

        if (exitStar != null)
        {
            float pulse = 1f + 0.2f * Mathf.Sin(Time.time * 5f);
            exitStar.localScale = Vector3.one * 0.06f * pulse;
        }

        if (guideRing != null)
        {
            if (IsGrabbed)
            {
                if (guideRing.gameObject.activeSelf)
                    guideRing.gameObject.SetActive(false);
            }
            else
            {
                if (!guideRing.gameObject.activeSelf)
                    guideRing.gameObject.SetActive(true);

                float gazeU, gazeV;
                ComputeGazeUV(out gazeU, out gazeV);

                float dx = gazeU - _u;
                float dy = gazeV - _v;
                bool nearDot = (dx * dx + dy * dy) < (grabRadius * grabRadius);
                float ringPulse = nearDot
                    ? 1f + 0.12f * Mathf.Sin(Time.time * 8f)
                    : 1f + 0.35f * Mathf.Sin(Time.time * 3.5f);
                guideRing.localScale = Vector3.one * ringPulse;

                if (mazePlane != null)
                {
                    Vector3 localPos = new Vector3(gazeU - 0.5f, gazeV - 0.5f, -0.025f);
                    guideRing.position = mazePlane.TransformPoint(localPos);
                    guideRing.rotation = mazePlane.rotation;
                }
            }
        }

        if (!IsGrabbed)
        {
            UpdateUngrabbed();
            return;
        }

        UpdateGrabbed();
    }

    void UpdateUngrabbed()
    {
        float gazeU, gazeV;
        ComputeGazeUV(out gazeU, out gazeV);

        float dx = gazeU - _u;
        float dy = gazeV - _v;
        bool gazeOnDot = (dx * dx + dy * dy) < (grabRadius * grabRadius);

        if (gazeOnDot)
        {
            _dwellTimer += Time.deltaTime;

            float t = Mathf.Clamp01(_dwellTimer / grabDwellTime);
            if (_dotMat != null)
                _dotMat.color = Color.Lerp(dotUngrabColor, dotDwellColor, t);

            if (laserDot != null)
                laserDot.localScale = Vector3.one * Mathf.Lerp(0.06f, 0.09f, t);

            if (_dwellTimer >= grabDwellTime)
            {
                IsGrabbed = true;
                _dwellTimer = 0f;
                CalibrateCenter();
                if (laserDot != null) laserDot.localScale = Vector3.one * 0.06f;
            }
        }
        else
        {
            _dwellTimer = 0f;
            float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 4f);
            if (_dotMat != null)
                _dotMat.color = new Color(dotUngrabColor.r, dotUngrabColor.g * pulse,
                                          dotUngrabColor.b, 1f);
            if (laserDot != null)
                laserDot.localScale = Vector3.one * (0.055f + 0.01f * Mathf.Sin(Time.time * 4f));
        }

        PositionDot();
    }

    void UpdateGrabbed()
    {
        Quaternion delta = Quaternion.Inverse(_calibRot) * ReadHeadRot();
        Vector3 euler    = delta.eulerAngles;

        float yaw   = euler.y > 180f ? euler.y - 360f : euler.y;
        float pitch = euler.x > 180f ? euler.x - 360f : euler.x;

        float mx = Mathf.Abs(yaw)   > deadZone ? Mathf.Sign(yaw)   * (Mathf.Abs(yaw)   - deadZone) : 0f;
        float my = Mathf.Abs(pitch) > deadZone ? Mathf.Sign(pitch) * (Mathf.Abs(pitch) - deadZone) : 0f;

        float du =  mx * sensitivity * Time.deltaTime;
        float dv = -my * sensitivity * Time.deltaTime;

        const float STEP = 0.002f;
        int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(du), Mathf.Abs(dv)) / STEP));
        float su = du / steps;
        float sv = dv / steps;
        bool hitWall = false;

        for (int i = 0; i < steps; i++)
        {
            float nu = Mathf.Clamp01(_u + su);
            float nv = Mathf.Clamp01(_v + sv);

            bool blockU = SampleWall(nu, _v);
            bool blockV = SampleWall(_u, nv);

            if (!blockU) _u = nu;
            if (!blockV) _v = nv;

            if (blockU || blockV) hitWall = true;
            if (blockU && blockV) break;
        }

        IsOnWall = hitWall;

        if (_dotMat != null)
            _dotMat.color = IsOnWall ? dotWallColor : dotNormalColor;
        if (laserDot != null)
            laserDot.localScale = Vector3.one * 0.06f;

        PositionDot();

        float ex = _u - exitU;
        float ey = _v - exitV;
        IsAtExit = (ex * ex + ey * ey) < (exitRadius * exitRadius);
    }

    void ComputeGazeUV(out float u, out float v)
    {
        u = _u;
        v = _v;

        if (mazePlane == null) return;

        Quaternion headRot = ReadHeadRot();
        Vector3 gazeDir = headRot * Vector3.forward;

        Camera cam = Camera.main;
        Vector3 rayOrigin = cam != null ? cam.transform.position : Vector3.zero;

        Vector3 planeNormal = mazePlane.forward;
        Vector3 planePoint  = mazePlane.position;

        float denom = Vector3.Dot(planeNormal, gazeDir);
        if (Mathf.Abs(denom) < 0.0001f) return;

        float t = Vector3.Dot(planePoint - rayOrigin, planeNormal) / denom;
        if (t <= 0f) return;

        Vector3 hitWorld = rayOrigin + gazeDir * t;
        Vector3 hitLocal = mazePlane.InverseTransformPoint(hitWorld);

        u = Mathf.Clamp01(hitLocal.x + 0.5f);
        v = Mathf.Clamp01(hitLocal.y + 0.5f);
    }

    bool SampleWall(float u, float v)
    {
        if (mazeTexture == null || !mazeTexture.isReadable) return false;
        int px = Mathf.Clamp(Mathf.RoundToInt(u * (mazeTexture.width  - 1)), 0, mazeTexture.width  - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(v * (mazeTexture.height - 1)), 0, mazeTexture.height - 1);
        Color c = mazeTexture.GetPixel(px, py);
        return (c.r + c.g + c.b) / 3f < wallThreshold;
    }

    void PositionDot()
    {
        if (laserDot == null || mazePlane == null) return;
        Vector3 localPos = new Vector3(_u - 0.5f, _v - 0.5f, -0.015f);
        laserDot.position = mazePlane.TransformPoint(localPos);
    }

    void PositionStar()
    {
        if (exitStar == null || mazePlane == null) return;
        Vector3 localPos = new Vector3(exitU - 0.5f, exitV - 0.5f, -0.02f);
        exitStar.position = mazePlane.TransformPoint(localPos);
    }

    static Quaternion ReadHeadRot()
    {
        var hmd = UnityEngine.InputSystem.InputSystem.GetDevice<XRHMD>();
        if (hmd != null)
        {
            Quaternion r = hmd.centerEyeRotation.ReadValue();
            if (r != Quaternion.identity) return r;
        }
        Camera cam = Camera.main;
        return cam != null ? cam.transform.rotation : Quaternion.identity;
    }
}
