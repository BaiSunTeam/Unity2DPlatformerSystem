using UnityEngine;

public class WallSlideState : BaseState
{
    public WallSlideState(PlayerController player, Animator animator) : base(player, animator)
    {
    }

    public override void OnEnter()
    {
        Debug.Log("Wall Slide State Entered");
        animator.CrossFade(WallSlideHash, crossFadeDuration);
    }

    public override void FixedUpdate()
    {
        player.HandleWallSlide();
        player.HandleWallJump();
    }
}
