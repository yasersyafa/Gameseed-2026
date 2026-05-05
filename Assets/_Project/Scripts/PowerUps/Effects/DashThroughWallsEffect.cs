using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Disables collision between this player and every Wall-tagged collider for
/// the effect duration. Uses per-pair <c>Physics.IgnoreCollision</c> instead
/// of <c>Physics.IgnoreLayerCollision</c> (latter is global — would also
/// affect other players' walls).
/// </summary>
public class DashThroughWallsEffect : IPowerUpEffect
{
    public PowerUpSO.PowerUpKey Key      => PowerUpSO.PowerUpKey.DashThroughWalls;
    public PowerUpSO.MutexGroup Mutex    => PowerUpSO.MutexGroup.None;
    public float                Duration { get; }

    private readonly List<Collider> _ignoredWalls = new();
    private Collider _playerCol;

    public DashThroughWallsEffect(float duration = 8f) { Duration = duration; }

    public void OnApply(PlayerController player)
    {
        _playerCol = player.GetComponent<Collider>();
        if (_playerCol == null) return;

        var walls = GameObject.FindGameObjectsWithTag("Wall");
        for (int i = 0; i < walls.Length; i++)
        {
            var col = walls[i].GetComponent<Collider>();
            if (col == null) continue;
            Physics.IgnoreCollision(_playerCol, col, true);
            _ignoredWalls.Add(col);
        }
    }

    public void OnRemove(PlayerController player)
    {
        if (_playerCol == null) { _ignoredWalls.Clear(); return; }
        for (int i = 0; i < _ignoredWalls.Count; i++)
        {
            if (_ignoredWalls[i] != null)
                Physics.IgnoreCollision(_playerCol, _ignoredWalls[i], false);
        }
        _ignoredWalls.Clear();
    }

    public void OnBeforeThrow(PlayerController player, Boomerang boomerang) { }
    public bool OnBeforeHit(PlayerController player, int killerIndex, Vector3 hitDir) => false;
    public bool OnBeforeDash(PlayerController player) => false;
    public void Tick(PlayerController player) { }
}
