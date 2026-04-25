using UnityEngine;

public class PlayerAttackingState : IPlayerState
{
    public void Enter(PlayerController player)
    {
        player.Rb.linearVelocity = Vector3.zero;

        player.Melee.Swing(onComplete: () =>
        {
            // Kembali ke Idle setelah swing selesai
            player.ChangeState(new PlayerIdleState());
        });
    }

    public void Exit(PlayerController player) { }

    // Semua input diblock selama swing
    public void HandleMove(PlayerController player, Vector2 input)   { }
    public void HandleDash(PlayerController player)                   { }
    public void HandleThrow(PlayerController player)                  { }
    public void HandleAttack(PlayerController player)                 { }

    public void FixedUpdate(PlayerController player) { }
}