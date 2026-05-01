using UnityEngine;

/// <summary>
/// Minimal settings overlay. Toggle via Esc. OnGUI for now — user can replace
/// with UI Toolkit panel later. Reads/writes SettingsManager which persists
/// via PlayerPrefs.
/// </summary>
public class SettingsScreen : MonoBehaviour
{
    private bool _open = false;

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            _open = !_open;
        }
    }

    private void OnGUI()
    {
        if (!_open) return;
        if (SettingsManager.Instance == null) return;
        var s = SettingsManager.Instance;

        const int W = 360;
        const int H = 380;
        var rect = new Rect((Screen.width - W) * 0.5f, (Screen.height - H) * 0.5f, W, H);

        GUI.Box(rect, "Settings");
        GUILayout.BeginArea(new Rect(rect.x + 16, rect.y + 28, W - 32, H - 44));

        GUILayout.Label($"Master Volume: {s.MasterVolume:F2}");
        s.SetMaster(GUILayout.HorizontalSlider(s.MasterVolume, 0f, 1f));

        GUILayout.Label($"Music Volume: {s.MusicVolume:F2}");
        s.SetMusic(GUILayout.HorizontalSlider(s.MusicVolume, 0f, 1f));

        GUILayout.Label($"SFX Volume: {s.SfxVolume:F2}");
        s.SetSfx(GUILayout.HorizontalSlider(s.SfxVolume, 0f, 1f));

        GUILayout.Label($"UI Volume: {s.UiVolume:F2}");
        s.SetUi(GUILayout.HorizontalSlider(s.UiVolume, 0f, 1f));

        GUILayout.Space(10);

        bool shake  = GUILayout.Toggle(s.ShakeEnabled,  " Screen Shake");
        if (shake != s.ShakeEnabled) s.SetShake(shake);

        bool slowmo = GUILayout.Toggle(s.SlowMoEnabled, " Slow Motion");
        if (slowmo != s.SlowMoEnabled) s.SetSlowMo(slowmo);

        bool rumble = GUILayout.Toggle(s.RumbleEnabled, " Gamepad Rumble");
        if (rumble != s.RumbleEnabled) s.SetRumble(rumble);

        GUILayout.Space(20);
        if (GUILayout.Button("Close (Esc)")) _open = false;

        GUILayout.EndArea();
    }
}
