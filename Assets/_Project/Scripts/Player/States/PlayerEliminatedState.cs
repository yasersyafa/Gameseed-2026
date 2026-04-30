using UnityEngine;
#region Player States

public class PlayerEliminatedState : IPlayerState
{
    public void Enter(PlayerController player)
    {
        player.Rb.linearVelocity = Vector3.zero;
        player.HasBoomerang      = false;
        player.CanDash           = false;
        player.MoveInput         = Vector2.zero;
    }

    public void Exit(PlayerController player)
    {
        // Reset resource saat keluar dari eliminated (spawn baru)
        player.HasBoomerang = true;
        player.CanDash      = true;
    }

    public void HandleMove(PlayerController player, Vector2 input) { }
    public void HandleDash(PlayerController player) { }
    public void HandleThrow(PlayerController player) { }
    public void HandleThrowReleased(PlayerController player) { }

    public void FixedUpdate(PlayerController player) { }

    public void HandleAttack(PlayerController player) { }
}
#endregion