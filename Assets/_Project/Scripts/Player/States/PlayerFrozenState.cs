using UnityEngine;
#region Player States

public class PlayerFrozenState : IPlayerState
{
    public void Enter(PlayerController player)
    {
        player.Rb.linearVelocity = Vector3.zero;
    }

    public void Exit(PlayerController player) { }

    public void HandleMove(PlayerController player, Vector2 input) { }
    public void HandleDash(PlayerController player) { }
    public void HandleThrow(PlayerController player) { }
    public void HandleThrowReleased(PlayerController player) { }

    public void FixedUpdate(PlayerController player) { }
    public void HandleAttack(PlayerController player) { }
}
#endregion