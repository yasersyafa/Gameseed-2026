public interface IPlayerState
{
    void Enter(PlayerController player);
    void Exit(PlayerController player);

    void HandleMove(PlayerController player, UnityEngine.Vector2 input);
    void HandleDash(PlayerController player);
    void HandleThrow(PlayerController player);

    void FixedUpdate(PlayerController player);
}
