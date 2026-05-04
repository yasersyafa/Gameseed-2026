using DG.Tweening;
using UnityEngine;
#region Player States

/// <summary>
/// Tahan tombol throw → akumulasi charge → release fires boomerang dengan
/// force/distance scaled. Movement diperlambat saat charging untuk feel
/// tradeoff (mirror Boomerang Fu).
/// </summary>
public class PlayerChargingState : IPlayerState
{
    private const float ChargeMoveMultiplier = 0.55f;

    private float    _startTime;
    private Tween    _pulseTween;

    public void Enter(PlayerController player)
    {
        _startTime = Time.time;
        player.IsCharging = true;
#if UNITY_EDITOR
        Debug.Log($"[Player {player.PlayerIndex}] Charge START");
#endif
        GameEvents.RaiseChargeStarted(player.PlayerIndex);

        // Visual pulse saat charging
        if (player.visual != null)
        {
            var t = player.visual.transform;
            _pulseTween?.Kill();
            _pulseTween = t.DOScale(t.localScale * 1.08f, 0.25f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(t.gameObject, LinkBehaviour.KillOnDestroy);
        }
    }

    public void Exit(PlayerController player)
    {
        player.IsCharging = false;
        _pulseTween?.Kill();

        // Reset scale ke baseline
        if (player.visual != null)
            player.visual.transform.localScale = Vector3.one;
    }

    public void HandleMove(PlayerController player, Vector2 input) { }

    public void HandleDash(PlayerController player)
    {
        if (!player.CanDash) return;
        player.ChangeState(new PlayerDashingState());
    }

    public void HandleThrow(PlayerController player) { }

    public void HandleThrowReleased(PlayerController player)
    {
        float charge01 = Mathf.Clamp01((Time.time - _startTime) / player.ChargeMaxTime);
#if UNITY_EDITOR
        Debug.Log($"[Player {player.PlayerIndex}] Charge RELEASE charge={charge01:F2}");
#endif
        player.ThrowBoomerangCharged(charge01);
        GameEvents.RaiseChargeReleased(player.PlayerIndex, charge01);
        player.ChangeState(new PlayerIdleState());
    }

    public void HandleAttack(PlayerController player)
    {
        // Cancel charge → langsung melee
        player.ChangeState(new PlayerAttackingState());
    }

    public void FixedUpdate(PlayerController player)
    {
        // Movement diperlambat saat charging
        Vector3 dir = new Vector3(player.MoveInput.x, 0f, player.MoveInput.y).normalized;
        player.Rb.linearVelocity = dir * (player.MoveSpeed * ChargeMoveMultiplier);
        player.ApplyRotation();
    }
}
#endregion
