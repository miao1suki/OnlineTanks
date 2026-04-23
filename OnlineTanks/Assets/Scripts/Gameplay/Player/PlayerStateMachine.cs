using UnityEngine.Playables;

public class PlayerStateMachine
{
    private PlayerState currentState;

    public PlayerStateMachine(PlayerController player) { }

    public void ChangeState(PlayerState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void Update()
    {
        currentState?.Update();
    }
}