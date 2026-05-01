using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fullscreen iris wipe — circle hole shrinks to winner position on round end,
/// then expands again on round start. Boomerang Fu style transition.
/// </summary>
public class IrisTransition : MonoBehaviour
{
    [Header("Timing (overridden by IrisConfigSO if present in Resources)")]
    [SerializeField] private float closeDuration = 0.7f;
    [SerializeField] private float openDuration  = 0.5f;
    [SerializeField] private float closeDelay    = 0.4f;
    [SerializeField] private float holdAtClosed  = 0.35f;

    [Header("Radii")]
    [SerializeField] private float fullRadius   = 1.6f;
    [SerializeField] private float closedRadius = 0.0f;

    [SerializeField] private float gameOverDurationMul = 1.5f;

    private Canvas        _canvas;
    private RawImage      _img;
    private Material      _mat;
    private RectTransform _rt;
    private GameManager   _gm;

    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int CenterId = Shader.PropertyToID("_Center");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");

    private void Awake()
    {
        TryLoadConfig();
        BuildCanvas();
        BuildOverlay();
        SetRadius(fullRadius);
    }

    private void TryLoadConfig()
    {
        var cfg = Resources.Load<IrisConfigSO>("IrisConfig");
        if (cfg == null) return;

        closeDelay          = cfg.closeDelay;
        closeDuration       = cfg.closeDuration;
        holdAtClosed        = cfg.holdAtClosed;
        openDuration        = cfg.openDuration;
        fullRadius          = cfg.fullRadius;
        closedRadius        = cfg.closedRadius;
        gameOverDurationMul = cfg.gameOverDurationMul;

#if UNITY_EDITOR
        Debug.Log($"[IrisTransition] Loaded IrisConfig: close={closeDuration}s open={openDuration}s hold={holdAtClosed}s delay={closeDelay}s");
#endif
    }

    private void OnEnable()
    {
        GameEvents.OnRoundEnded   += HandleRoundEnded;
        GameEvents.OnRoundStarted += HandleRoundStarted;
        GameEvents.OnGameOver     += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.OnRoundEnded   -= HandleRoundEnded;
        GameEvents.OnRoundStarted -= HandleRoundStarted;
        GameEvents.OnGameOver     -= HandleGameOver;
    }

    private void BuildCanvas()
    {
        var canvasGo = new GameObject("IrisCanvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 200; // above HUD
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
    }

    private void BuildOverlay()
    {
        var go = new GameObject("IrisOverlay");
        go.transform.SetParent(_canvas.transform, false);
        _rt = go.AddComponent<RectTransform>();
        _rt.anchorMin = Vector2.zero;
        _rt.anchorMax = Vector2.one;
        _rt.offsetMin = Vector2.zero;
        _rt.offsetMax = Vector2.zero;

        _img = go.AddComponent<RawImage>();
        _img.raycastTarget = false;

        var shader = Shader.Find("UI/IrisWipe");
        if (shader == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[IrisTransition] Shader 'UI/IrisWipe' not found. Skipping iris.");
#endif
            _img.enabled = false;
            return;
        }
        _mat = new Material(shader);
        _img.material = _mat;

        UpdateAspect();
    }

    private void UpdateAspect()
    {
        if (_mat == null) return;
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        _mat.SetFloat(AspectId, aspect);
    }

    private void SetRadius(float r)
    {
        if (_mat == null) return;
        _mat.SetFloat(RadiusId, r);
    }

    private void SetCenter(Vector2 uv)
    {
        if (_mat == null) return;
        _mat.SetVector(CenterId, new Vector4(uv.x, uv.y, 0f, 0f));
    }

    private Vector2 WorldToScreenUV(Vector3 worldPos)
    {
        var cam = Camera.main;
        if (cam == null) return new Vector2(0.5f, 0.5f);
        Vector3 vp = cam.WorldToViewportPoint(worldPos);
        if (vp.z < 0f) return new Vector2(0.5f, 0.5f);
        return new Vector2(Mathf.Clamp01(vp.x), Mathf.Clamp01(vp.y));
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void HandleRoundEnded(int winnerIndex)
    {
        Vector2 center = ResolveWinnerCenter(winnerIndex);
        CloseAndOpen(center);
    }

    private void HandleRoundStarted(int round)
    {
        // Safety: ensure iris is open at round start in case a wipe was interrupted
        OpenIris(openDuration);
    }

    private void HandleGameOver(int winnerIndex)
    {
        Vector2 center = ResolveWinnerCenter(winnerIndex);
        CloseIris(center, closeDuration * gameOverDurationMul);
        // Stay closed — match end.
    }

    private Vector2 ResolveWinnerCenter(int winnerIndex)
    {
        if (_gm == null) _gm = FindFirstObjectByType<GameManager>();
        if (_gm == null || winnerIndex < 0) return new Vector2(0.5f, 0.5f);

        var players = _gm.GetAllPlayers();
        if (winnerIndex >= players.Count) return new Vector2(0.5f, 0.5f);

        var winner = players[winnerIndex];
        if (winner == null) return new Vector2(0.5f, 0.5f);

        return WorldToScreenUV(winner.transform.position);
    }

    public void CloseIris(Vector2 centerUV, float duration)
    {
        if (_mat == null) return;
        UpdateAspect();
        SetCenter(centerUV);

        _mat.DOKill();
        DOVirtual.DelayedCall(closeDelay, () =>
        {
            if (_mat == null) return;
            DOVirtual.Float(_mat.GetFloat(RadiusId), closedRadius, duration, SetRadius)
                .SetEase(Ease.InQuad)
                .SetUpdate(true);
        }).SetUpdate(true);
    }

    public void CloseAndOpen(Vector2 centerUV)
    {
        if (_mat == null) return;
        UpdateAspect();
        SetCenter(centerUV);

        _mat.DOKill();
        var seq = DOTween.Sequence().SetUpdate(true);
        seq.AppendInterval(closeDelay)
           .Append(DOVirtual.Float(_mat.GetFloat(RadiusId), closedRadius, closeDuration, SetRadius)
                .SetEase(Ease.InQuad))
           .AppendInterval(holdAtClosed)
           .Append(DOVirtual.Float(closedRadius, fullRadius, openDuration, SetRadius)
                .SetEase(Ease.OutQuad));
    }

    public void OpenIris(float duration)
    {
        if (_mat == null) return;
        UpdateAspect();
        _mat.DOKill();
        DOVirtual.Float(_mat.GetFloat(RadiusId), fullRadius, duration, SetRadius)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }
}
