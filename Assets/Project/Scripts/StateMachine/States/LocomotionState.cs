using UnityEngine;

public class LocomotionState : BaseState
{
    public LocomotionState(PlayerController player, Animator animator) : base(player, animator)
    {
    }

    public override void OnEnter()
    {
        Debug.Log("Locomotion State Entered");
        animator.CrossFade(LocomotionHash, crossFadeDuration);
    }

    public override void Update()
    {
        player.UpdateMovementAnimator();
        player.UpdateFacing();
        player.UpdateCoyoteTime();
    }

    public override void FixedUpdate()
    {
        player.HandleGroundMovement();
    }
}
