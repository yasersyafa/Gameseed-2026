using UnityEngine;
#region Player States
public class PlayerIdleState : IPlayerState
{
    public void Enter(PlayerController player)
    {
        player.Rb.linearVelocity = Vector3.zero;
    }

    public void Exit(PlayerController player) { }

    public void HandleMove(PlayerController player, Vector2 input)
    {
        player.MoveInput = input;
    }

    public void HandleDash(PlayerController player)
    {
        if (!player.CanDash) return;
        player.ChangeState(new PlayerDashingState());
    }

    public void HandleThrow(PlayerController player)
    {
        if (!player.HasBoomerang) return;
        player.ThrowBoomerang();
    }

    public void FixedUpdate(PlayerController player)
    {
        player.ApplyMovement();
        player.ApplyRotation();
    }

    public void HandleAttack(PlayerController player)
    {
        if (!player.HasBoomerang) return;
        player.ChangeState(new PlayerAttackingState());
    }
}
#endregion