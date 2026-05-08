using UnityEngine;

public class DashState : BaseState
{
    public DashState(PlayerController player, Animator animator) : base(player, animator)
    {
    }
    public override void OnEnter()
    {
        Debug.Log("Dash State Entered");
        animator.CrossFade(DashHash, crossFadeDuration);
        player.HandleDash();
    }

    public override void Update()
    {
        player.UpdateDashAnimator();
    }
}
