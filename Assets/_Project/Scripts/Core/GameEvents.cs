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
    }
}