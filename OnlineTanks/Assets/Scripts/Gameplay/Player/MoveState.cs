public class MoveState : PlayerState
{
    public MoveState(PlayerController player) : base(player) { }

    public override void Update()
    {
        player.ServerMove(player.Input.MoveInput, player.Input.LookInput);
    }
}