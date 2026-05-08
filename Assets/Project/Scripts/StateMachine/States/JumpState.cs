using UnityEngine;

public class JumpState : BaseState
{
    public JumpState(PlayerController player, Animator animator) : base(player, animator)
    {
    }

    public override void OnEnter()
    {
        Debug.Log("Jump State Entered");
        animator.CrossFade(JumpHash, crossFadeDuration);
    }

    public override void Update()
    {
        player.UpdateJumpAnimator();
        player.UpdateFacing();
        player.UpdateCoyoteTime();
    }

    public override void FixedUpdate()
    {
        player.HandleAirMovement();
        player.HandleJump();
        player.HandleGravity();
    }
}
