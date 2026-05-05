using System;
using UnityEngine;

/// <summary>
/// Semua game event terpusat di sini.
/// Sistem lain subscribe/unsubscribe tanpa tahu satu sama lain.
/// </summary>
public static class GameEvents
{
    // ── Player lifecycle ──────────────────────────────────────────────────────

    /// <summary>Player kehabisan nyawa dan dieliminasi.</summary>
    public static event Action<int, PlayerController> OnPlayerEliminated;

    /// <summary>Player respawn (masih punya nyawa).</summary>
    public static event Action<int, PlayerController> OnPlayerRespawned;

    /// <summary>Player terkena boomerang (sebelum cek nyawa).</summary>
    public static event Action<int, PlayerController> OnPlayerHit;

    // ── Round lifecycle ───────────────────────────────────────────────────────

    /// <summary>Round dimulai setelah countdown selesai.</summary>
    public static event Action<int> OnRoundStarted;

    /// <summary>Round selesai — winnerIndex -1 berarti draw.</summary>
    public static event Action<int> OnRoundEnded;

    /// <summary>Game selesai, ada pemenang keseluruhan.</summary>
    public static event Action<int> OnGameOver;

    /// <summary>Score berubah.</summary>
    public static event Action<int[]> OnScoresUpdated;

    /// <summary>Countdown tick — 3, 2, 1, lalu 0 (GO).</summary>
    public static event Action<int> OnCountdownTick;

    // ── Combat / boomerang ────────────────────────────────────────────────────

    /// <summary>Player melempar boomerang.</summary>
    public static event Action<int> OnBoomerangThrown;

    /// <summary>Player menangkap boomerang kembali.</summary>
    public static event Action<int> OnBoomerangCaught;

    /// <summary>Player melakukan dash.</summary>
    public static event Action<int> OnPlayerDashed;

    /// <summary>Player mulai charging throw.</summary>
    public static event Action<int> OnChargeStarted;

    /// <summary>Player release charging throw — float = charge level 0..1.</summary>
    public static event Action<int, float> OnChargeReleased;

    /// <summary>Boomerang berhasil di-parry oleh melee.</summary>
    public static event Action<int> OnBoomerangParried;

    /// <summary>Boomerang ricochet ke wall.</summary>
    public static event Action OnBoomerangWallBounce;

    /// <summary>Iris transition reached fully-closed state (game-over only).</summary>
    public static event Action OnIrisClosed;

    /// <summary>Pickup spawned di arena.</summary>
    public static event Action OnPickupSpawned;

    /// <summary>Player ambil pickup — playerIndex, PowerUpSO.PowerUpKey int, world position.</summary>
    public static event Action<int, int, Vector3> OnPickupCollected;

    /// <summary>Power-up effect dilepas dari stack — playerIndex, PowerUpSO.PowerUpKey int.</summary>
    public static event Action<int, int> OnPowerUpRemoved;

    /// <summary>Shield effect absorbed an incoming hit — playerIndex, hit direction.</summary>
    public static event Action<int, Vector3> OnShieldAbsorbed;

    // ── Raise helpers (dipanggil sistem yang fire event) ─────────────────────

    public static void RaisePlayerEliminated(int index, PlayerController controller)
        => OnPlayerEliminated?.Invoke(index, controller);

    public static void RaisePlayerRespawned(int index, PlayerController controller)
        => OnPlayerRespawned?.Invoke(index, controller);

    public static void RaisePlayerHit(int index, PlayerController controller)
        => OnPlayerHit?.Invoke(index, controller);

    public static void RaiseRoundStarted(int round)
        => OnRoundStarted?.Invoke(round);

    public static void RaiseRoundEnded(int winnerIndex)
        => OnRoundEnded?.Invoke(winnerIndex);

    public static void RaiseGameOver(int winnerIndex)
        => OnGameOver?.Invoke(winnerIndex);

    public static void RaiseScoresUpdated(int[] scores)
        => OnScoresUpdated?.Invoke(scores);

    public static void RaiseCountdownTick(int value)
        => OnCountdownTick?.Invoke(value);

    public static void RaiseBoomerangThrown(int playerIndex)
        => OnBoomerangThrown?.Invoke(playerIndex);

    public static void RaiseBoomerangCaught(int playerIndex)
        => OnBoomerangCaught?.Invoke(playerIndex);

    public static void RaisePlayerDashed(int playerIndex)
        => OnPlayerDashed?.Invoke(playerIndex);

    public static void RaiseChargeStarted(int playerIndex)
        => OnChargeStarted?.Invoke(playerIndex);

    public static void RaiseChargeReleased(int playerIndex, float charge01)
        => OnChargeReleased?.Invoke(playerIndex, charge01);

    public static void RaiseBoomerangParried(int playerIndex)
        => OnBoomerangParried?.Invoke(playerIndex);

    public static void RaiseBoomerangWallBounce()
        => OnBoomerangWallBounce?.Invoke();

    public static void RaiseIrisClosed()
        => OnIrisClosed?.Invoke();

    public static void RaisePickupSpawned()
        => OnPickupSpawned?.Invoke();

    public static void RaisePickupCollected(int playerIndex, int powerUpKey, Vector3 worldPos)
        => OnPickupCollected?.Invoke(playerIndex, powerUpKey, worldPos);

    public static void RaisePowerUpRemoved(int playerIndex, int powerUpKey)
        => OnPowerUpRemoved?.Invoke(playerIndex, powerUpKey);

    public static void RaiseShieldAbsorbed(int playerIndex, Vector3 hitDir)
        => OnShieldAbsorbed?.Invoke(playerIndex, hitDir);

    /// <summary>
    /// Bersihkan semua subscriber — panggil saat scene reload
    /// agar tidak ada stale reference.
    /// </summary>
    public static void ClearAll()
    {
        OnPlayerEliminated  = null;
        OnPlayerRespawned   = null;
        OnPlayerHit         = null;
        OnRoundStarted      = null;
        OnRoundEnded        = null;
        OnGameOver          = null;
        OnScoresUpdated     = null;
        OnCountdownTick     = null;
        OnBoomerangThrown      = null;
        OnBoomerangCaught      = null;
        OnPlayerDashed         = null;
        OnChargeStarted        = null;
        OnChargeReleased       = null;
        OnBoomerangParried     = null;
        OnBoomerangWallBounce  = null;
        OnIrisClosed           = null;
        OnPickupSpawned        = null;
        OnPickupCollected      = null;
        OnPowerUpRemoved       = null;
        OnShieldAbsorbed       = null;
    }
}