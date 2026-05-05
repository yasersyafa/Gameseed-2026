using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manage stack power-up aktif di 1 player. Max 3 slots (Boomerang Fu rule).
/// Fire/Ice mutex enforced. PlayerController call OnBeforeThrow + OnBeforeHit
/// di hook gameplay.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PowerUpController : MonoBehaviour
{
    public const int MaxStack = 3;

    private struct Active
    {
        public IPowerUpEffect effect;
        public float          expireAt;
    }

    private readonly List<Active> _active = new(MaxStack);
    private PlayerController _player;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    /// <summary>
    /// Top of the active stack — newest applied effect. <see cref="PowerUpSO.PowerUpKey.None"/>
    /// when nothing's active. Used by juice listeners (PowerUpAuraVfx) to pick
    /// the dominant tint after every stack mutation.
    /// </summary>
    public PowerUpSO.PowerUpKey TopKey()
        => _active.Count > 0 ? _active[_active.Count - 1].effect.Key : PowerUpSO.PowerUpKey.None;

    private void Update()
    {
        if (_active.Count == 0) return;

        // Per-frame tick (Telekinesis steers active boomerang, etc).
        for (int i = 0; i < _active.Count; i++)
            _active[i].effect.Tick(_player);

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (Time.time >= _active[i].expireAt)
            {
                var eff = _active[i].effect;
                _active.RemoveAt(i);
                eff.OnRemove(_player);
                GameEvents.RaisePowerUpRemoved(_player.PlayerIndex, (int)eff.Key);
#if UNITY_EDITOR
                Debug.Log($"[PowerUp {_player.PlayerIndex}] expired {eff.Key}");
#endif
            }
        }
    }

    public bool Apply(IPowerUpEffect effect)
    {
        if (effect == null) return false;

        // Mutex: drop existing of same group
        if (effect.Mutex != PowerUpSO.MutexGroup.None)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].effect.Mutex == effect.Mutex)
                {
                    var old = _active[i].effect;
                    _active.RemoveAt(i);
                    old.OnRemove(_player);
                    GameEvents.RaisePowerUpRemoved(_player.PlayerIndex, (int)old.Key);
                }
            }
        }

        // Stack overflow: drop oldest
        if (_active.Count >= MaxStack)
        {
            var oldest = _active[0].effect;
            _active.RemoveAt(0);
            oldest.OnRemove(_player);
            GameEvents.RaisePowerUpRemoved(_player.PlayerIndex, (int)oldest.Key);
        }

        _active.Add(new Active { effect = effect, expireAt = Time.time + effect.Duration });
        effect.OnApply(_player);

#if UNITY_EDITOR
        Debug.Log($"[PowerUp {_player.PlayerIndex}] apply {effect.Key} dur={effect.Duration}s stack={_active.Count}");
#endif
        GameEvents.RaisePickupCollected(
            _player.PlayerIndex, (int)effect.Key, _player.transform.position);
        return true;
    }

    public void OnBeforeThrow(Boomerang boomerang)
    {
        for (int i = 0; i < _active.Count; i++)
            _active[i].effect.OnBeforeThrow(_player, boomerang);
    }

    /// <summary>
    /// Return true if any effect consumed the dash input (Teleport). Consuming
    /// effects are removed from the stack same shape as <see cref="OnBeforeHit"/>.
    /// </summary>
    public bool OnBeforeDash()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i].effect.OnBeforeDash(_player))
            {
                var eff = _active[i].effect;
                _active.RemoveAt(i);
                eff.OnRemove(_player);
                GameEvents.RaisePowerUpRemoved(_player.PlayerIndex, (int)eff.Key);
                return true;
            }
        }
        return false;
    }

    /// <summary>Return true if any effect absorbed the hit.</summary>
    public bool OnBeforeHit(int killerIndex, Vector3 hitDir)
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i].effect.OnBeforeHit(_player, killerIndex, hitDir))
            {
                // Consume effect
                var eff = _active[i].effect;
                _active.RemoveAt(i);
                eff.OnRemove(_player);
                GameEvents.RaisePowerUpRemoved(_player.PlayerIndex, (int)eff.Key);
#if UNITY_EDITOR
                Debug.Log($"[PowerUp {_player.PlayerIndex}] absorbed by {eff.Key}");
#endif
                return true;
            }
        }
        return false;
    }

    public void ClearAll()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var eff = _active[i].effect;
            eff.OnRemove(_player);
            GameEvents.RaisePowerUpRemoved(_player.PlayerIndex, (int)eff.Key);
        }
        _active.Clear();
    }
}
