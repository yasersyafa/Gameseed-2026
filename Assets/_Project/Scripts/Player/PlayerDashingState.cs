using System.Collections;
using UnityEngine;
#region Player States

public class PlayerDashingState : IPlayerState
{
    public void Enter(PlayerController player)
    {
        player.CanDash = false;
        Vector3 dir = player.MoveDirection != Vector3.zero
            ? player.MoveDirection
            : player.LastMoveDirection;

        player.Rb.linearVelocity = dir * player.DashForce;
        player.StartCoroutine(DashRoutine(player));
    }

    public void Exit(PlayerController player)
    {
        player.Rb.linearVelocity = Vector3.zero;
    }

    public void HandleMove(PlayerController player, Vector2 input) { }
    public void HandleDash(PlayerController player) { }
    public void HandleThrow(PlayerController player) { }

    public void FixedUpdate(PlayerController player) { }

    private IEnumerator DashRoutine(PlayerController player)
    {
        yield return YieldCollection.WaitForSeconds(player.DashDuration);

        // Kembali ke Idle dulu, baru mulai cooldown
        player.ChangeState(new PlayerIdleState());

        yield return YieldCollection.WaitForSeconds(player.DashCooldown);
        player.CanDash = true;
    }
}
#endregion