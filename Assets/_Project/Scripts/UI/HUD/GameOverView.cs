using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using VContainer;

/// <summary>
/// Standalone game-over overlay. Lives on its own UIDocument with a higher
/// sortingOrder than the iris (300 vs 200) so the WINNER card + RESTART
/// button paint *on top* of the closed iris while the rest of the HUD
/// (sortingOrder 100) stays behind it.
///
/// Reveal flow:
///   1. OnGameOver fires → stash winner index.
///   2. Iris closes (~1.45 s) → IrisTransition raises OnIrisClosed.
///   3. Reveal panel + RESTART button.
///   4. RESTART click → hide panel, call RoundManager.Restart() (iris reopens
///      and a fresh round begins).
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class GameOverView : MonoBehaviour
{
    private static readonly Color[] PlayerTints =
    {
        new(1.00f, 0.74f, 0.66f),  // peach
        new(0.72f, 0.86f, 0.96f),  // sky
        new(0.74f, 0.93f, 0.84f),  // mint
        new(1.00f, 0.92f, 0.62f),  // lemon
    };
    private static readonly string[] PlayerTintClasses =
        { "peach", "sky", "mint", "lemon" };

    [Header("Design system refs (wire via Tools > Boomerang Fu > Setup/HUD)")]
    [SerializeField] private StyleSheet  hudStyleSheet;
    [SerializeField] private FontLibrary fontLibrary;

    private UIDocument    _doc;
    private VisualElement _panel;
    private VisualElement _content;
    private Label         _label;
    private Button        _restartButton;

    private int _pendingWinner = -1;

    private RoundManager _round;

    [Inject]
    public void Construct(RoundManager round)
    {
        _round = round;
    }

    private void Awake()
    {
        _doc = GetComponent<UIDocument>();
        if (_doc == null || _doc.rootVisualElement == null)
        {
            Debug.LogError("[GameOverView] UIDocument missing or unconfigured. Run Tools > Boomerang Fu > Setup/HUD.");
            enabled = false;
            return;
        }

        if (fontLibrary != null)
            FontHelper.Register(fontLibrary.display, fontLibrary.mono);

        var docRoot = _doc.rootVisualElement;
        docRoot.pickingMode = PickingMode.Position;
        if (hudStyleSheet != null && !docRoot.styleSheets.Contains(hudStyleSheet))
            docRoot.styleSheets.Add(hudStyleSheet);

        _panel         = docRoot.Q<VisualElement>("game-over-panel");
        _content       = docRoot.Q<VisualElement>("gameover-content");
        _label         = docRoot.Q<Label>("game-over-label");
        _restartButton = docRoot.Q<Button>("game-over-restart");

        ApplyFonts(docRoot);

        if (_restartButton != null) _restartButton.clicked += RestartMatch;
    }

    private void Start()
    {
        if (_round == null) _round = FindFirstObjectByType<RoundManager>();
    }

    private void OnEnable()
    {
        GameEvents.OnGameOver   += HandleGameOver;
        GameEvents.OnIrisClosed += HandleIrisClosed;
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver   -= HandleGameOver;
        GameEvents.OnIrisClosed -= HandleIrisClosed;
    }

    // ── Reveal ──────────────────────────────────────────────────────────────
    private void HandleGameOver(int winnerIndex) => _pendingWinner = winnerIndex;

    private void HandleIrisClosed()
    {
        if (_pendingWinner < 0 || _panel == null || _label == null) return;

        int winnerIndex = _pendingWinner;
        _pendingWinner = -1;

        _label.text = $"P{winnerIndex + 1} WINS!";
        _panel.style.display = DisplayStyle.Flex;

        if (_content != null)
        {
            _content.RemoveFromClassList("dustypink");
            _content.RemoveFromClassList("peach");
            _content.RemoveFromClassList("sky");
            _content.RemoveFromClassList("mint");
            _content.RemoveFromClassList("lemon");
            _content.AddToClassList(PlayerTintClasses[winnerIndex % PlayerTintClasses.Length]);
        }

        PunchScale(_panel, 1f, 0.55f, fromScale: 0.4f);
    }

    private void RestartMatch()
    {
        if (_panel != null) _panel.style.display = DisplayStyle.None;
        if (_round != null) _round.Restart();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────
    private void ApplyFonts(VisualElement root)
    {
        var display = FontHelper.Display();
        if (display == null) return;
        root.Query<Label>(className: "display-bold").ForEach(l =>
            l.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(display)));
        root.Query<Button>(className: "brutal-button").ForEach(b =>
            b.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(display)));
    }

    private static void PunchScale(VisualElement v, float to, float duration, float fromScale)
    {
        v.style.scale = new StyleScale(new Scale(new Vector3(fromScale, fromScale, 1f)));
        v.experimental.animation.Scale(to, Mathf.RoundToInt(duration * 1000));
    }
}
