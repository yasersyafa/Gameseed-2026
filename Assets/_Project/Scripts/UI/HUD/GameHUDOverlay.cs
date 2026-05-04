using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// Runtime in-game HUD overlay. Programmatic Canvas + uGUI. Score popups,
/// countdown big-num pulse, power-up icon row per player. Auto-spawned by
/// HUDBootstrap.
/// </summary>
public class GameHUDOverlay : MonoBehaviour
{
    private Canvas _canvas;
    private RectTransform _rootRect;
    private Text   _countdownLabel;
    private Camera _cam;

    private readonly Dictionary<int, RectTransform> _powerUpRows = new();
    private readonly Dictionary<int, RectTransform> _offScreenArrows = new();
    private GameManager _gameManager;

    [Inject]
    public void Construct(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    private void Awake()
    {
        BuildCanvas();
        BuildCountdown();
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerEliminated += HandleElim;
        GameEvents.OnCountdownTick    += HandleCountdown;
        GameEvents.OnPickupCollected  += HandlePickupCollected;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerEliminated -= HandleElim;
        GameEvents.OnCountdownTick    -= HandleCountdown;
        GameEvents.OnPickupCollected  -= HandlePickupCollected;
    }

    private void Start()
    {
        _cam = Camera.main;
        // Fallback: if not injected (auto-spawned via HUDBootstrap), find once.
        if (_gameManager == null) _gameManager = FindFirstObjectByType<GameManager>();
    }

    private void BuildCanvas()
    {
        var canvasGo = new GameObject("HUDCanvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();
        _rootRect = _canvas.GetComponent<RectTransform>();
    }

    private void BuildCountdown()
    {
        var go = new GameObject("Countdown");
        go.transform.SetParent(_rootRect, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(400, 200);

        _countdownLabel = go.AddComponent<Text>();
        _countdownLabel.alignment = TextAnchor.MiddleCenter;
        _countdownLabel.fontSize  = 160;
        _countdownLabel.color     = new Color(1f, 1f, 1f, 0f);
        _countdownLabel.font      = FontHelper.Default();
        _countdownLabel.text      = "";
    }

    // ── Countdown ─────────────────────────────────────────────────────────────
    private void HandleCountdown(int value)
    {
        if (_countdownLabel == null) return;
        _countdownLabel.text  = value > 0 ? value.ToString() : "GO!";
        _countdownLabel.color = value > 0 ? Color.white : new Color(1f, 0.85f, 0.2f, 1f);

        var rt = _countdownLabel.rectTransform;
        rt.localScale = Vector3.one * 2.2f;
        DOTween.Kill(rt);
        rt.DOScale(0.85f, 0.45f).SetEase(Ease.OutBack).SetUpdate(true)
            .SetLink(rt.gameObject, LinkBehaviour.KillOnDestroy);
        _countdownLabel.DOFade(1f, 0.05f).SetUpdate(true)
            .SetLink(_countdownLabel.gameObject, LinkBehaviour.KillOnDestroy);
        _countdownLabel.DOFade(0f, 0.45f).SetDelay(0.55f).SetUpdate(true)
            .SetLink(_countdownLabel.gameObject, LinkBehaviour.KillOnDestroy);
    }

    // ── Score popup ───────────────────────────────────────────────────────────
    private void HandleElim(int idx, PlayerController controller)
    {
        if (controller == null || _cam == null) return;
        Vector3 worldPos = controller.transform.position + Vector3.up * 1.5f;
        Vector3 screenPos = _cam.WorldToScreenPoint(worldPos);
        if (screenPos.z < 0f) return;

        var go = new GameObject("ScorePopup");
        go.transform.SetParent(_rootRect, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 80);
        rt.position = screenPos;

        var label = go.AddComponent<Text>();
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize  = 64;
        label.color     = Color.white;
        label.font      = FontHelper.Default();
        label.text      = "ELIM!";

        rt.localScale = Vector3.one * 0.4f;
        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(rt.DOScale(1.2f, 0.18f).SetEase(Ease.OutBack))
           .Join(rt.DOAnchorPosY(rt.anchoredPosition.y + 80f, 0.6f).SetEase(Ease.OutQuad))
           .Append(label.DOFade(0f, 0.3f))
           .SetLink(go, LinkBehaviour.KillOnDestroy)
           .OnComplete(() => { if (go != null) Destroy(go); });
    }

    // ── Power-up HUD ──────────────────────────────────────────────────────────
    private void HandlePickupCollected(int playerIndex, int powerUpKey)
    {
        var row = GetOrCreatePowerUpRow(playerIndex);

        var icon = new GameObject($"PUIcon_{powerUpKey}");
        icon.transform.SetParent(row, false);
        var rt = icon.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60, 60);

        var img = icon.AddComponent<Image>();
        img.color = ColorForPowerUp(powerUpKey);

        var label = new GameObject("KeyLabel");
        label.transform.SetParent(rt, false);
        var lrt = label.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var txt = label.AddComponent<Text>();
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize  = 22;
        txt.color     = Color.black;
        txt.font      = FontHelper.Default();
        txt.text      = ((PowerUpSO.PowerUpKey)powerUpKey).ToString().Substring(0, 3);

        rt.localScale = Vector3.one * 0.4f;
        rt.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true)
            .SetLink(icon, LinkBehaviour.KillOnDestroy);

        Destroy(icon, 12f); // simple TTL fallback; replace with duration-from-SO later
    }

    private RectTransform GetOrCreatePowerUpRow(int playerIndex)
    {
        if (_powerUpRows.TryGetValue(playerIndex, out var existing) && existing != null) return existing;

        var rowGo = new GameObject($"PowerUpRow_{playerIndex}");
        rowGo.transform.SetParent(_rootRect, false);
        var rt = rowGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot     = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(20f, 20f + playerIndex * 75f);
        rt.sizeDelta = new Vector2(400, 60);

        var hl = rowGo.AddComponent<HorizontalLayoutGroup>();
        hl.spacing      = 8;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;

        _powerUpRows[playerIndex] = rt;
        return rt;
    }

    // ── Off-screen player arrows ──────────────────────────────────────────────
    private void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null || _rootRect == null || _gameManager == null) return;

        var players = _gameManager.GetAllPlayers();
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (p == null || p.IsEliminated) { HideArrow(i); continue; }

            Vector3 vp = _cam.WorldToViewportPoint(p.transform.position);
            bool behindCam = vp.z < 0f;
            bool onScreen  = !behindCam && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
            if (onScreen) { HideArrow(i); continue; }

            ShowArrow(i, p, vp, behindCam);
        }
    }

    private void HideArrow(int idx)
    {
        if (_offScreenArrows.TryGetValue(idx, out var rt) && rt != null)
            rt.gameObject.SetActive(false);
    }

    private void ShowArrow(int idx, PlayerController p, Vector3 vp, bool behindCam)
    {
        var rt = GetOrCreateArrow(idx, p);
        if (rt == null) return;

        // If behind camera, mirror coords
        if (behindCam) { vp.x = 1f - vp.x; vp.y = 1f - vp.y; }

        // Direction from screen center to off-screen target (in viewport space)
        Vector2 center = new(0.5f, 0.5f);
        Vector2 target = new(vp.x, vp.y);
        Vector2 dir    = (target - center);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();

        // Clamp to canvas edge with small margin
        const float margin = 0.06f;
        float halfW = 0.5f - margin;
        float halfH = 0.5f - margin;
        float scale = Mathf.Min(halfW / Mathf.Abs(dir.x + 1e-4f), halfH / Mathf.Abs(dir.y + 1e-4f));
        Vector2 edgeVp = center + dir * scale;

        // Convert viewport to screen pos for canvas overlay
        Vector2 screenPos = new(edgeVp.x * Screen.width, edgeVp.y * Screen.height);
        rt.position = screenPos;

        // Rotate arrow toward off-screen target
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        rt.rotation = Quaternion.Euler(0f, 0f, angle);

        if (!rt.gameObject.activeSelf) rt.gameObject.SetActive(true);
    }

    private RectTransform GetOrCreateArrow(int idx, PlayerController p)
    {
        if (_offScreenArrows.TryGetValue(idx, out var existing) && existing != null) return existing;

        var go = new GameObject($"OffScreenArrow_{idx}");
        go.transform.SetParent(_rootRect, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80f, 80f);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Color tint = (p.visual != null && p.visual.sharedMaterial != null)
            ? p.visual.sharedMaterial.color
            : Color.white;

        var txt = go.AddComponent<Text>();
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize  = 72;
        txt.fontStyle = FontStyle.Bold;
        txt.color     = tint;
        txt.font      = FontHelper.Default();
        txt.text      = "▲";
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow   = VerticalWrapMode.Overflow;

        _offScreenArrows[idx] = rt;
        return rt;
    }

    private Color ColorForPowerUp(int key)
    {
        return ((PowerUpSO.PowerUpKey)key) switch
        {
            PowerUpSO.PowerUpKey.Fire   => new Color(1f, 0.4f, 0.1f),
            PowerUpSO.PowerUpKey.Ice    => new Color(0.4f, 0.85f, 1f),
            PowerUpSO.PowerUpKey.Multi  => new Color(0.85f, 0.6f, 1f),
            PowerUpSO.PowerUpKey.Shield => new Color(1f, 0.95f, 0.4f),
            _ => Color.white,
        };
    }
}
