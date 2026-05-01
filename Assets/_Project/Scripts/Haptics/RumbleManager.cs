using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gamepad rumble dispatcher. Subscribe ke combat events, route motor speeds
/// per-player Gamepad. Gated by SettingsManager.RumbleEnabled.
/// </summary>
public class RumbleManager : MonoBehaviour
{
    [Header("Profiles (low, high, duration)")]
    [SerializeField] private Vector3 hitProfile     = new(0.3f, 0.5f, 0.15f);
    [SerializeField] private Vector3 deathProfile   = new(0.8f, 1.0f, 0.30f);
    [SerializeField] private Vector3 parryProfile   = new(0.5f, 0.7f, 0.10f);
    [SerializeField] private Vector3 dashProfile    = new(0.2f, 0.3f, 0.08f);
    [SerializeField] private Vector3 throwProfile   = new(0.15f, 0.25f, 0.06f);

    private readonly Dictionary<int, Coroutine> _activeRoutines = new();

    private GameManager _gm;

    private void OnEnable()
    {
        GameEvents.OnPlayerHit         += HandleHit;
        GameEvents.OnPlayerEliminated  += HandleEliminated;
        GameEvents.OnBoomerangParried  += HandleParry;
        GameEvents.OnPlayerDashed      += HandleDash;
        GameEvents.OnBoomerangThrown   += HandleThrow;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerHit         -= HandleHit;
        GameEvents.OnPlayerEliminated  -= HandleEliminated;
        GameEvents.OnBoomerangParried  -= HandleParry;
        GameEvents.OnPlayerDashed      -= HandleDash;
        GameEvents.OnBoomerangThrown   -= HandleThrow;

        StopAllRumble();
    }

    private void HandleHit(int idx, PlayerController c)         => Rumble(idx, hitProfile);
    private void HandleEliminated(int idx, PlayerController c)  => Rumble(idx, deathProfile);
    private void HandleParry(int idx)                           => Rumble(idx, parryProfile);
    private void HandleDash(int idx)                            => Rumble(idx, dashProfile);
    private void HandleThrow(int idx)                           => Rumble(idx, throwProfile);

    public void Rumble(int playerIndex, Vector3 profile)
        => Rumble(playerIndex, profile.x, profile.y, profile.z);

    public void Rumble(int playerIndex, float low, float high, float duration)
    {
        if (SettingsManager.Instance != null && !SettingsManager.Instance.RumbleEnabled) return;

        var pad = GetGamepadForPlayer(playerIndex);
        if (pad == null) return;

        if (_activeRoutines.TryGetValue(playerIndex, out var existing))
            StopCoroutine(existing);

        _activeRoutines[playerIndex] = StartCoroutine(RumbleRoutine(playerIndex, pad, low, high, duration));
    }

    private IEnumerator RumbleRoutine(int idx, Gamepad pad, float low, float high, float duration)
    {
        pad.SetMotorSpeeds(low, high);
        yield return new WaitForSecondsRealtime(duration);
        pad.SetMotorSpeeds(0f, 0f);
        _activeRoutines.Remove(idx);
    }

    private Gamepad GetGamepadForPlayer(int playerIndex)
    {
        if (_gm == null) _gm = FindFirstObjectByType<GameManager>();
        if (_gm == null) return null;

        var players = _gm.GetAllPlayers();
        if (playerIndex < 0 || playerIndex >= players.Count) return null;

        var pi = players[playerIndex].GetComponent<PlayerInput>();
        if (pi == null) return null;

        foreach (var device in pi.devices)
            if (device is Gamepad g) return g;

        return null;
    }

    private void StopAllRumble()
    {
        foreach (var pad in Gamepad.all)
            pad.SetMotorSpeeds(0f, 0f);
        _activeRoutines.Clear();
    }
}
