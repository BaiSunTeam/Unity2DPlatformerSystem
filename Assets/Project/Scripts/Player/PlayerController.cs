using UnityEngine;
using ImprovedTimers;
using KBCore.Refs;

[RequireComponent(typeof(Rigidbody2D), typeof(PlatformCollisionChecker), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Self] Rigidbody2D rb;
    [SerializeField, Self] PlatformCollisionChecker collisionChecker;
    [SerializeField, Self] Animator animator;
    [SerializeField] InputReader input;

    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 7f;
    [SerializeField] float acceleration = 10f;
    [SerializeField] float decceleration = 10f;
    [SerializeField] float velPower = 0.9f;

    [Header("Jump Settings")]
    [SerializeField] float maxJumpBufferTime = 0.125f;
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float gravityMultiplier = 2f;
    [SerializeField] float airControlMultiplier = 0.275f;
    [SerializeField] float maxRiseSpeed = 12f;
    [SerializeField] float maxFallSpeed = 20f;
    [SerializeField] float jumpAnimationPowerCurve = 0.4f;

    [Header("Wall Settings")]
    [SerializeField] float wallSlideSpeed = 2f;

    [Header("Dash Settings")]
    [SerializeField] float dashForce = 10f;
    [SerializeField] float dashDuration = 0.25f;
    [SerializeField] float dashCooldown = 2f;

    // variables
    private Vector2 movement;
    private bool jumpReleased;
    private float lastMovementX = 1;

    // timers
    private CountdownTimer jumpBufferTimer;
    private CountdownTimer coyoteTimer;
    private CountdownTimer dashTimer;
    private CountdownTimer dashCooldownTimer;

    // State Machine
    private StateMachine stateMachine;
    private static readonly int BlendSpeed = Animator.StringToHash("Speed");

    void Awake()
    {
        rb.freezeRotation = true;
        SetUpTimers();
        SetUpStateMachine();
    }

    void SetUpTimers()
    {
        jumpBufferTimer = new CountdownTimer(maxJumpBufferTime);
        coyoteTimer = new CountdownTimer(maxJumpBufferTime);
        dashTimer = new CountdownTimer(dashDuration);
        dashCooldownTimer = new CountdownTimer(dashCooldown);
    
        dashTimer.OnTimerStop += () => {
            rb.gravityScale = 1f; 
            dashCooldownTimer.Start();
        };
    }

    void SetUpStateMachine()
    {
        stateMachine = new StateMachine();

        var locomotionState = new LocomotionState(this, animator);
        var jumpState = new JumpState(this, animator);
        var wallSlideState = new WallSlideState(this, animator);
        var dashState = new DashState(this, animator);

        At(locomotionState, jumpState, new FuncPredicate(() => jumpBufferTimer.IsRunning || !collisionChecker.IsGrounded));
        At(jumpState, wallSlideState, new FuncPredicate(() => collisionChecker.IsTouchingWall && movement.x != 0));
        At(wallSlideState, jumpState, new FuncPredicate(() => !collisionChecker.IsTouchingWall));
        At(locomotionState, dashState, new FuncPredicate(() => collisionChecker.IsGrounded && dashTimer.IsRunning));
        At(jumpState, dashState, new FuncPredicate(() => dashTimer.IsRunning));
        At(dashState, jumpState, new FuncPredicate(() => !dashTimer.IsRunning && !collisionChecker.IsGrounded));
        Any(locomotionState, new FuncPredicate(() => collisionChecker.IsGrounded && !jumpBufferTimer.IsRunning && !dashTimer.IsRunning));
        stateMachine.SetState(locomotionState);
    }

    void Update()
    {
        stateMachine.Update();
    }

    void FixedUpdate()
    {
        stateMachine.FixedUpdate();
    }

    public void UpdateFacing()
    {
        Vector3 spriteDirection = Vector3.one;
        spriteDirection.x = lastMovementX;
        transform.localScale = spriteDirection;
    }

    public void UpdateMovementAnimator() => animator.SetFloat(BlendSpeed, Mathf.Abs(movement.x));
    public void UpdateDashAnimator() => animator.SetFloat(BlendSpeed, 1f - dashTimer.Progress);

    public void UpdateJumpAnimator()
    {
        float t = Mathf.InverseLerp(maxRiseSpeed, -maxFallSpeed, rb.linearVelocityY);
        animator.SetFloat(BlendSpeed, ApplyPowerCurve(t, jumpAnimationPowerCurve));
    }

    public void UpdateCoyoteTime()
    {
        if (collisionChecker.WasGrounded && !collisionChecker.IsGrounded) coyoteTimer.Start();
        if (!collisionChecker.WasGrounded && collisionChecker.IsGrounded) coyoteTimer.Stop();
    }
    
    public void HandleGroundMovement() => rb.AddForce(GetLateralMovementForce());

    public void HandleAirMovement() => rb.AddForce(GetLateralMovementForce() * airControlMultiplier);

    public void HandleJump()
    {
        if (!jumpBufferTimer.IsRunning) return;

        if (collisionChecker.IsGrounded || coyoteTimer.IsRunning)
        {
            rb.linearVelocityY = 0f;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpBufferTimer.Stop();
            coyoteTimer.Stop();
        }
    }

    public void HandleWallJump()
    {
        if (!jumpBufferTimer.IsRunning) return;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(-collisionChecker.WallContactDirection * jumpForce, jumpForce), ForceMode2D.Impulse);
        jumpBufferTimer.Stop();
    }

    public void HandleWallSlide() => rb.linearVelocityY = -wallSlideSpeed;

    public void HandleGravity()
    {
        // increase gravity when falling (i.e. when y velocity is negative)
        if (rb.linearVelocityY < 0f)
        {
            rb.AddForce((gravityMultiplier - 1f) * Physics2D.gravity.y * rb.mass * Vector2.up);
        }
        // increase gravity when jump is released early to cut the jump
        else if (rb.linearVelocityY > 0f && jumpReleased)
        {
            rb.AddForce((gravityMultiplier - 1f) * Physics2D.gravity.y * rb.mass * Vector2.up);
        }

        // clamps y velocity
        rb.linearVelocityY = Mathf.Clamp(rb.linearVelocityY, -maxFallSpeed, maxRiseSpeed);
    }
    
    public void HandleDash()
    {
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.AddForce(new Vector2(lastMovementX, 0) * dashForce, ForceMode2D.Impulse);
    }

    // helpers
    Vector2 GetLateralMovementForce()
    {
        // movement code used from the following video: https://www.youtube.com/watch?v=KbtcEVCM7bw
        float targetSpeed = movement.x * moveSpeed;
        float speedDif = targetSpeed - rb.linearVelocityX;
        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : decceleration;
        float speedFactor = Mathf.Pow(Mathf.Abs(speedDif) * accelRate, velPower) * Mathf.Sign(speedDif);

        return speedFactor * Vector2.right;
    }

    private float ApplyPowerCurve(float t, float power)
    {
        float centered = t * 2f - 1f;
        return Mathf.Sign(centered) * Mathf.Pow(Mathf.Abs(centered), power) * 0.5f + 0.5f;
    }

    private void At(IState from, IState to, IPredicate condition) => stateMachine.AddTransition(from, to, condition);
    private void Any(IState to, IPredicate condition) => stateMachine.AddAnyTransition(to, condition);

    private void OnMove(Vector2 inputDir) 
    {
        movement = inputDir;
        lastMovementX = inputDir.x + lastMovementX * (1 - Mathf.Abs(inputDir.x));
    }

    private void OnJump(bool performed)
    {
        if (performed && !jumpBufferTimer.IsRunning) jumpBufferTimer.Start();
        else jumpBufferTimer.Stop();

        jumpReleased = !performed;
    }

    void OnDash()
    {
        if (!dashTimer.IsRunning && !dashCooldownTimer.IsRunning)
        {
            dashTimer.Start();
        } 
    }

    void OnEnable()
    {
        input.Move += OnMove;
        input.Jump += OnJump;
        input.Dash += OnDash;
    }

    void OnDisable()
    {
        input.Move -= OnMove;
        input.Jump -= OnJump;
        input.Dash -= OnDash;
    }
}
