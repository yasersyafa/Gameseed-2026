/// <summary>
/// Semua ID SFX / music dipakai project. Tambahkan di sini, lalu daftarkan
/// clip-nya di AudioCueLibrary. AudioManager.Play(id) jalan otomatis.
/// </summary>
public enum AudioCueId
{
    None = 0,

    // ── Boomerang / Combat ─────────────────────────────────────────────
    BoomerangThrow,
    BoomerangCatch,
    BoomerangWallBounce,
    BoomerangWhoosh,
    BoomerangParry,
    PlayerHit,
    PlayerDeath,
    PlayerDash,
    ChargeLoop,
    ShieldBreak,

    // ── Round / Match ──────────────────────────────────────────────────
    CountdownTick,
    CountdownGo,
    RoundWin,
    MatchWin,

    // ── Power-up ───────────────────────────────────────────────────────
    PickupSpawn,
    PickupCollect,
    PowerUpActivate,

    // ── UI ─────────────────────────────────────────────────────────────
    UiHover,
    UiSelect,
    UiBack,

    // ── Music ──────────────────────────────────────────────────────────
    MusicMenu,
    MusicGame,
}
