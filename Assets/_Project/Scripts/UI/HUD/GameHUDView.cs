using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using VContainer;

/// <summary>
/// HUD generator. Loads design system from UXML/USS under
/// Assets/_Project/UI/HUD/Resources, queries named elements, binds GameEvents.
/// No runtime VisualElement construction for layout — only data + dynamic
/// instances (player rows cloned from PlayerRowTemplate.uxml, ELIM popups,
/// off-screen arrows).
/// </summary>
public class GameHUDView : MonoBehaviour
{
    private const string UxmlPath          = "HUDView";
    private const string UssPath           = "HUDStyle";
    private const string PlayerRowUxmlPath = "PlayerRowTemplate";

    // Tint class names that match USS variants
    private static readonly string[] PlayerTintClasses =
        { "peach", "sky", "mint", "lemon" };

    private UIDocument        _doc;
    private VisualElement     _root;
    private VisualTreeAsset   _playerRowTemplate;

    // Persistent named elements from UXML
    private VisualElement _topLeft;
    private VisualElement _worldLayer;
    private Label         _roundLabel;
    private VisualElement _countdownZone;
    private VisualElement _countdownContent;
    private Label         _countdownLabel;
    private VisualElement _gameOverPanel;
    private VisualElement _gameOverContent;
    private Label         _gameOverLabel;
    private VisualElement _pauseModal;
    private Button        _pauseButton;
    private Button        _resumeButton;
    private Button        _exitButton;

    // Per-player state
    private readonly Dictionary<int, VisualElement> _playerRows    = new();
    private readonly Dictionary<int, VisualElement> _infoContents  = new();
    private readonly Dictionary<int, VisualElement> _dotRows       = new();
    private readonly Dictionary<int, VisualElement> _offScreenArrows = new();

    // Pause
    private float _preTimeScale = 1f;
    private bool  _paused;
    private Coroutine _countdownAnim;

    // Services
    private GameManager  _gameManager;
    private RoundManager _round;
    private LivesSystem  _lives;
    private Camera       _cam;

    [Inject]
    public void Construct(GameManager gm, RoundManager round, LivesSystem lives)
    {
        _gameManager = gm;
        _round       = round;
        _lives       = lives;
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        BuildDocument();
        LoadAndCloneTree();
        QueryElements();
        ApplyFonts();
        WirePauseButtons();
    }

    private void Start()
    {
        _cam = Camera.main;
        if (_gameManager == null) _gameManager = FindFirstObjectByType<GameManager>();
        if (_round       == null) _round       = FindFirstObjectByType<RoundManager>();
        if (_lives       == null) _lives       = FindFirstObjectByType<LivesSystem>();
    }

    private void OnEnable()
    {
        GameEvents.OnCountdownTick    += HandleCountdown;
        GameEvents.OnRoundStarted     += HandleRoundStarted;
        GameEvents.OnGameOver         += HandleGameOver;
        GameEvents.OnScoresUpdated    += HandleScoresUpdated;
        GameEvents.OnPlayerEliminated += HandleElim;
    }

    private void OnDisable()
    {
        GameEvents.OnCountdownTick    -= HandleCountdown;
        GameEvents.OnRoundStarted     -= HandleRoundStarted;
        GameEvents.OnGameOver         -= HandleGameOver;
        GameEvents.OnScoresUpdated    -= HandleScoresUpdated;
        GameEvents.OnPlayerEliminated -= HandleElim;
    }

    // ── Document setup ───────────────────────────────────────────────────────
    private void BuildDocument()
    {
        var ps = ScriptableObject.CreateInstance<PanelSettings>();
        ps.scaleMode           = PanelScaleMode.ScaleWithScreenSize;
        ps.referenceResolution = new Vector2Int(1920, 1080);
        ps.match               = 0.5f;
        ps.sortingOrder        = 100;

        _doc               = gameObject.AddComponent<UIDocument>();
        _doc.panelSettings = ps;
    }

    private void LoadAndCloneTree()
    {
        var view = Resources.Load<VisualTreeAsset>(UxmlPath);
        var uss  = Resources.Load<StyleSheet>(UssPath);
        _playerRowTemplate = Resources.Load<VisualTreeAsset>(PlayerRowUxmlPath);

        if (view == null)
        {
            Debug.LogError($"[GameHUDView] Missing UXML at Resources/{UxmlPath}");
            return;
        }

        var docRoot = _doc.rootVisualElement;
        docRoot.style.flexGrow = 1;
        // Root must be Position so pause button click reaches it. UI Toolkit
        // PerformPick early-outs on Ignore — entire subtree becomes unpickable.
        docRoot.pickingMode = PickingMode.Position;

        view.CloneTree(docRoot);
        if (uss != null) docRoot.styleSheets.Add(uss);

        _root = docRoot.Q<VisualElement>("hud-root") ?? docRoot;
    }

    private void QueryElements()
    {
        _topLeft          = _root.Q<VisualElement>("top-left");
        _worldLayer       = _root.Q<VisualElement>("world-layer");
        _roundLabel       = _root.Q<Label>("round-label");
        _countdownZone    = _root.Q<VisualElement>("center-countdown");
        _countdownContent = _root.Q<VisualElement>("countdown-content");
        _countdownLabel   = _root.Q<Label>("countdown-label");
        _gameOverPanel    = _root.Q<VisualElement>("game-over-panel");
        _gameOverContent  = _root.Q<VisualElement>("gameover-content");
        _gameOverLabel    = _root.Q<Label>("game-over-label");
        _pauseModal       = _root.Q<VisualElement>("pause-modal");
        _pauseButton      = _root.Q<Button>("pause-button");
        _resumeButton     = _root.Q<Button>("pause-resume");
        _exitButton       = _root.Q<Button>("pause-exit");
    }

    private void ApplyFonts()
    {
        var display = FontHelper.Display();
        var mono    = FontHelper.Mono();
        if (display == null && mono == null) return;

        // All display-bold labels → Display font; round-label gets Mono so
        // numbers feel tabular.
        if (display != null)
        {
            _root.Query<Label>(className: "display-bold").ForEach(l =>
                l.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(display)));
            _root.Query<Button>(className: "brutal-button").ForEach(b =>
                b.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(display)));
        }
        if (mono != null && _roundLabel != null)
            _roundLabel.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(mono));
    }

    private void WirePauseButtons()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_pauseButton  != null) _pauseButton.clicked  += Pause;
        if (_resumeButton != null) _resumeButton.clicked += Resume;
        if (_exitButton   != null) _exitButton.clicked   += ExitGame;
#else
        // In release the pause UI is hidden + non-interactive.
        if (_pauseButton != null) _pauseButton.style.display = DisplayStyle.None;
#endif
    }

    // ── Pause ────────────────────────────────────────────────────────────────
    private void Pause()
    {
        if (_paused) return;
        _paused = true;
        _preTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        if (_pauseModal != null) _pauseModal.style.display = DisplayStyle.Flex;
    }

    private void Resume()
    {
        if (!_paused) return;
        _paused = false;
        Time.timeScale = _preTimeScale > 0f ? _preTimeScale : 1f;
        AudioListener.pause = false;
        if (_pauseModal != null) _pauseModal.style.display = DisplayStyle.None;
    }

    private void ExitGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Player row instantiation ─────────────────────────────────────────────
    private VisualElement EnsurePlayerRow(int playerIndex)
    {
        if (_playerRows.TryGetValue(playerIndex, out var existing) && existing != null)
            return existing;

        if (_playerRowTemplate == null || _topLeft == null)
        {
            Debug.LogWarning("[GameHUDView] Missing PlayerRowTemplate or top-left zone");
            return null;
        }

        var instance = _playerRowTemplate.Instantiate();
        var row = instance.Q<VisualElement>("player-row") ?? instance;
        _topLeft.Add(row);

        string tintClass = PlayerTintClasses[playerIndex % PlayerTintClasses.Length];
        bool   even      = playerIndex % 2 == 0;

        var infoCard    = row.Q<VisualElement>("info-card");
        var infoContent = row.Q<VisualElement>("info-content");
        var tag         = row.Q<Label>("tag");
        var dots        = row.Q<VisualElement>("dots");

        if (infoContent != null)
        {
            infoContent.RemoveFromClassList("cream");
            infoContent.AddToClassList(tintClass);
        }
        if (infoCard != null)
            infoCard.AddToClassList(even ? "even" : "odd");
        if (tag != null)
        {
            tag.text = $"P{playerIndex + 1}";
            // Cloned-after-Awake labels miss the ApplyFonts query — assign here.
            var display = FontHelper.Display();
            if (display != null)
                tag.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(display));
        }

        _playerRows[playerIndex]   = row;
        _infoContents[playerIndex] = infoContent;
        _dotRows[playerIndex]      = dots;
        return row;
    }

    // ── Event handlers ───────────────────────────────────────────────────────
    private void HandleRoundStarted(int round)
    {
        if (_roundLabel != null) _roundLabel.text = $"ROUND {round}";

        // Materialise one row per joined player at the first round; idempotent
        // so subsequent rounds don't re-clone.
        if (_gameManager != null)
        {
            var players = _gameManager.GetAllPlayers();
            for (int i = 0; i < players.Count; i++)
                EnsurePlayerRow(i);
        }
    }

    private void HandleScoresUpdated(int[] scores)
    {
        for (int i = 0; i < scores.Length; i++)
        {
            EnsurePlayerRow(i);
            if (_dotRows.TryGetValue(i, out var row) && row != null)
                RebuildPointDots(row, scores[i]);
            if (_infoContents.TryGetValue(i, out var card) && card != null)
                PunchScale(card, 1f, 0.25f, fromScale: 1.25f);
        }
    }

    private void RebuildPointDots(VisualElement row, int count)
    {
        row.Clear();
        for (int i = 0; i < count; i++)
        {
            var dot = new VisualElement();
            dot.AddToClassList("point-dot");
            dot.pickingMode = PickingMode.Ignore;
            row.Add(dot);
        }
    }

    private void HandleCountdown(int value)
    {
        if (_countdownLabel == null || _countdownContent == null) return;
        if (_countdownAnim != null) StopCoroutine(_countdownAnim);
        _countdownAnim = StartCoroutine(CountdownRoutine(value));
    }

    private IEnumerator CountdownRoutine(int value)
    {
        _countdownLabel.text = value > 0 ? value.ToString() : "GO!";

        // Swap tint class on countdown-content
        _countdownContent.RemoveFromClassList("cream");
        _countdownContent.RemoveFromClassList("lemon");
        _countdownContent.AddToClassList(value == 0 ? "lemon" : "cream");

        _countdownZone.style.opacity = 1f;

        const float popDuration  = 0.45f;
        const float holdDuration = 0.50f;
        const float fadeDuration = 0.40f;
        const float fromScale    = 2.2f;
        const float toScale      = 1.0f;

        float t = 0f;
        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / popDuration);
            float eased = 1f - Mathf.Pow(1f - k, 3f);
            float s = Mathf.Lerp(fromScale, toScale, eased);
            _countdownZone.style.scale = new StyleScale(new Scale(new Vector3(s, s, 1f)));
            yield return null;
        }
        _countdownZone.style.scale = new StyleScale(new Scale(new Vector3(toScale, toScale, 1f)));

        float held = 0f;
        while (held < holdDuration)
        {
            held += Time.unscaledDeltaTime;
            yield return null;
        }

        float f = 0f;
        while (f < fadeDuration)
        {
            f += Time.unscaledDeltaTime;
            _countdownZone.style.opacity = Mathf.Lerp(1f, 0f, Mathf.Clamp01(f / fadeDuration));
            yield return null;
        }
        _countdownZone.style.opacity = 0f;
        _countdownAnim = null;
    }

    private void HandleElim(int idx, PlayerController controller)
    {
        if (controller == null || _cam == null) return;
        SpawnElimPopup(controller.transform.position + Vector3.up * 1.5f);
    }

    private void HandleGameOver(int winnerIndex)
    {
        if (_gameOverPanel == null || _gameOverLabel == null) return;
        _gameOverLabel.text = $"P{winnerIndex + 1} WINS!";
        _gameOverPanel.style.display = DisplayStyle.Flex;
        if (winnerIndex >= 0 && _gameOverContent != null)
        {
            _gameOverContent.RemoveFromClassList("dustypink");
            _gameOverContent.RemoveFromClassList("peach");
            _gameOverContent.RemoveFromClassList("sky");
            _gameOverContent.RemoveFromClassList("mint");
            _gameOverContent.RemoveFromClassList("lemon");
            _gameOverContent.AddToClassList(PlayerTintClasses[winnerIndex % PlayerTintClasses.Length]);
        }
        PunchScale(_gameOverPanel, 1f, 0.55f, fromScale: 0.4f);
    }

    // ── ELIM popup + off-screen arrow ────────────────────────────────────────
    private void SpawnElimPopup(Vector3 worldPos)
    {
        Vector2 ui = WorldToUi(worldPos);
        if (float.IsNaN(ui.x) || _worldLayer == null) return;

        var popup = new VisualElement();
        popup.AddToClassList("brutal-card");
        popup.AddToClassList("elim-popup");
        popup.style.left = ui.x - 100;
        popup.style.top  = ui.y - 40;
        popup.style.rotate = new StyleRotate(new Rotate(new Angle(UnityEngine.Random.Range(-6f, 6f))));

        var shadow = new VisualElement();
        shadow.AddToClassList("brutal-shadow");
        var content = new VisualElement();
        content.AddToClassList("brutal-content");
        content.AddToClassList("lemon");

        var lbl = new Label("ELIM!");
        lbl.AddToClassList("display-bold");
        lbl.AddToClassList("elim-text");
        var display = FontHelper.Display();
        if (display != null)
            lbl.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(display));
        content.Add(lbl);

        popup.Add(shadow);
        popup.Add(content);
        _worldLayer.Add(popup);

        PunchScale(popup, 1f, 0.18f, fromScale: 0.4f);

        float startTop = ui.y - 40;
        popup.schedule.Execute(() =>
        {
            popup.style.top = startTop - 80f;
            FadeOut(popup, 0.3f);
        }).StartingIn(180);

        popup.schedule.Execute(() =>
        {
            if (popup.parent != null) popup.parent.Remove(popup);
        }).StartingIn(900);
    }

    private void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null || _gameManager == null || _worldLayer == null) return;

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
        if (_offScreenArrows.TryGetValue(idx, out var v) && v != null)
            v.style.display = DisplayStyle.None;
    }

    private void ShowArrow(int idx, PlayerController p, Vector3 vp, bool behindCam)
    {
        var arrow = EnsureArrow(idx, p);
        if (arrow == null) return;

        if (behindCam) { vp.x = 1f - vp.x; vp.y = 1f - vp.y; }

        Vector2 center = new(0.5f, 0.5f);
        Vector2 target = new(vp.x, vp.y);
        Vector2 dir = (target - center);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();

        const float margin = 0.06f;
        float halfW = 0.5f - margin;
        float halfH = 0.5f - margin;
        float scale = Mathf.Min(halfW / Mathf.Abs(dir.x + 1e-4f), halfH / Mathf.Abs(dir.y + 1e-4f));
        Vector2 edgeVp = center + dir * scale;

        Vector2 ui = ViewportToUi(edgeVp);
        arrow.style.left = ui.x - 40;
        arrow.style.top  = ui.y - 40;

        float angle = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg + 90f;
        arrow.style.rotate = new StyleRotate(new Rotate(new Angle(angle)));

        if (arrow.style.display.value != DisplayStyle.Flex)
            arrow.style.display = DisplayStyle.Flex;
    }

    private VisualElement EnsureArrow(int idx, PlayerController p)
    {
        if (_offScreenArrows.TryGetValue(idx, out var existing) && existing != null) return existing;

        Color tint = (p.visual != null && p.visual.sharedMaterial != null)
            ? p.visual.sharedMaterial.color
            : Color.white;

        var arrow = new VisualElement();
        arrow.AddToClassList("brutal-card");
        arrow.AddToClassList("offscreen-arrow");

        var shadow = new VisualElement();
        shadow.AddToClassList("brutal-shadow");
        var content = new VisualElement();
        content.AddToClassList("brutal-content");
        content.style.backgroundColor = tint;

        var lbl = new Label("▲");
        lbl.AddToClassList("display-bold");
        lbl.style.fontSize = 56;
        var display = FontHelper.Display();
        if (display != null)
            lbl.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(display));
        content.Add(lbl);

        arrow.Add(shadow);
        arrow.Add(content);
        _worldLayer.Add(arrow);
        _offScreenArrows[idx] = arrow;
        return arrow;
    }

    // ── Coords ───────────────────────────────────────────────────────────────
    private Vector2 WorldToUi(Vector3 worldPos)
    {
        if (_cam == null) return new Vector2(float.NaN, float.NaN);
        Vector3 sp = _cam.WorldToScreenPoint(worldPos);
        if (sp.z < 0f) return new Vector2(float.NaN, float.NaN);
        return new Vector2(sp.x, Screen.height - sp.y);
    }

    private Vector2 ViewportToUi(Vector2 vp)
        => new(vp.x * Screen.width, (1f - vp.y) * Screen.height);

    // ── Animation helpers (non-countdown) ────────────────────────────────────
    private static void PunchScale(VisualElement v, float to, float duration, float fromScale)
    {
        v.style.scale = new StyleScale(new Scale(new Vector3(fromScale, fromScale, 1f)));
        v.experimental.animation.Scale(to, Mathf.RoundToInt(duration * 1000));
    }

    private static void FadeOut(VisualElement v, float duration)
    {
        v.experimental.animation.Start(
            new StyleValues { opacity = v.resolvedStyle.opacity },
            new StyleValues { opacity = 0f },
            Mathf.RoundToInt(duration * 1000));
    }

}
