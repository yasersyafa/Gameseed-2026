using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manage status effect aktif di 1 player. Apply, tick, expire.
/// Pasang di PlayerController GameObject.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class StatusEffectController : MonoBehaviour
{
    private struct ActiveEffect
    {
        public StatusEffectType type;
        public float            expireAt;
        public int              sourcePlayerIndex;
    }

    private readonly Dictionary<StatusEffectType, ActiveEffect> _active = new();
    private readonly List<StatusEffectType> _scratch = new(8);
    private PlayerController _player;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    public void Apply(StatusEffectType type, float duration, int sourcePlayerIndex = -1)
    {
        if (type == StatusEffectType.None) return;

        _active[type] = new ActiveEffect
        {
            type              = type,
            expireAt          = Time.time + duration,
            sourcePlayerIndex = sourcePlayerIndex,
        };

#if UNITY_EDITOR
        Debug.Log($"[Status] Apply {type} on player {_player?.PlayerIndex} dur={duration}s src={sourcePlayerIndex}");
#endif

        OnEffectStart(type, sourcePlayerIndex);
    }

    public bool Has(StatusEffectType type) => _active.ContainsKey(type);

    public void Clear(StatusEffectType type)
    {
        if (_active.Remove(type)) OnEffectEnd(type);
    }

    public void ClearAll()
    {
        if (_active.Count == 0) return;
        _scratch.Clear();
        foreach (var k in _active.Keys) _scratch.Add(k);
        for (int i = 0; i < _scratch.Count; i++) Clear(_scratch[i]);
        _scratch.Clear();
    }

    private void Update()
    {
        if (_active.Count == 0) return;

        _scratch.Clear();
        foreach (var kv in _active)
        {
            if (Time.time >= kv.Value.expireAt) _scratch.Add(kv.Key);
        }
        if (_scratch.Count == 0) return;

        for (int i = 0; i < _scratch.Count; i++)
        {
            var t   = _scratch[i];
            var src = _active[t].sourcePlayerIndex;
            _active.Remove(t);
#if UNITY_EDITOR
            Debug.Log($"[Status] Expire {t} on player {_player?.PlayerIndex}");
#endif
            OnEffectExpire(t, src);
        }
        _scratch.Clear();
    }

    // ── Hooks ─────────────────────────────────────────────────────────────────

    private void OnEffectStart(StatusEffectType type, int sourceIndex)
    {
        switch (type)
        {
            case StatusEffectType.Frozen:
                _player.SetFreeze(true);
                break;
            case StatusEffectType.Stunned:
                _player.SetFreeze(true);
                break;
            case StatusEffectType.Burning:
                // Boomerang Fu: fire = lethal seketika
                _player.OnHitByBoomerang(sourceIndex, Vector3.zero);
                break;
        }
    }

    private void OnEffectExpire(StatusEffectType type, int sourceIndex)
    {
        switch (type)
        {
            case StatusEffectType.Frozen:
            case StatusEffectType.Stunned:
                if (!_player.IsEliminated) _player.SetFreeze(false);
                break;
        }
    }

    private void OnEffectEnd(StatusEffectType type)
    {
        OnEffectExpire(type, -1);
    }
}
