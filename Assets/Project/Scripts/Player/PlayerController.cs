using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public InputReader input;
    private Rigidbody2D rb;

    [Header("Movement")]
    public float moveSpeed;
    public float acceleration;
    public float decceleration;
    public float velPower;
    public float airControlMultiplier;

    [Header("Ground Check")]
    public Transform groundCheckTransform;
    public Vector2 groundCheckSize;
    public LayerMask groundLayer;

    [Header("Jump")]
    public float jumpForce;
    public float fallingGravityMultiplier;
    public float jumpCutGravityMultiplier;
    public float maxCoyoteTime;
    public float maxJumpBufferTime;

    [Header("Wall Interaction")]
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public Vector2 wallCheckSize;
    public float wallSlideSpeed;
    public float wallJumpForceX;
    public float wallJumpForceY;
    public float wallJumpInputLockTime;

    // State
    private Vector2 moveInput;
    public bool isGrounded;
    private bool isWallSliding;
    private int wallContactDirection;
    private bool jumpReleased;

    // Timers
    private CountdownTimer coyoteTimer;
    private CountdownTimer jumpBufferTimer;
    private CountdownTimer wallJumpInputLockTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        coyoteTimer = new CountdownTimer(maxCoyoteTime);
        jumpBufferTimer = new CountdownTimer(maxJumpBufferTime);
        wallJumpInputLockTimer = new CountdownTimer(wallJumpInputLockTime);
    }

    void OnEnable()
    {
        input.Move += OnMove;
        input.Jump += OnJump;
    }

    void OnDisable()
    {
        input.Move -= OnMove;
        input.Jump -= OnJump;
    }

    void Update()
    {
        UpdateGrounded();
        UpdateWallContact();
        TickTimers();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleJump();
        HandleGravity();
        HandleWallSlide();
    }

    private void UpdateGrounded()
    {
        // store previous grounded state
        bool wasGrounded = isGrounded;
        // test if current frame is grounded
        isGrounded = Physics2D.OverlapBox(groundCheckTransform.position, groundCheckSize, 0f, groundLayer);

        // start and stop coyote time depending on the grounded state
        if (wasGrounded && !isGrounded) coyoteTimer.Start();
        if (!wasGrounded && isGrounded) coyoteTimer.Stop();
    }

    private void UpdateWallContact()
    {
        bool touchingLeft = Physics2D.OverlapBox(wallCheckLeft.position, wallCheckSize, 0f, groundLayer);
        bool touchingRight = Physics2D.OverlapBox(wallCheckRight.position, wallCheckSize, 0f, groundLayer);

        // get the direction of wall contact -1 for left and 1 for right and 0 if not touching any walls
        wallContactDirection = touchingLeft ? -1 : touchingRight ? 1 : 0;

        // wall sliding only happens if not on the ground and player is actively pushing against the wall
        isWallSliding = !isGrounded                                 // is in the air
            && wallContactDirection != 0                            // touching a wall
            && moveInput.x == wallContactDirection;     // pushing against wall
    }

    private void TickTimers()
    {
        coyoteTimer.Tick(Time.deltaTime);
        jumpBufferTimer.Tick(Time.deltaTime);
        wallJumpInputLockTimer.Tick(Time.deltaTime);
    }

    private void OnMove(Vector2 dir) => moveInput = dir;

    private void OnJump(bool pressed)
    {
        if (pressed)
        {
            jumpBufferTimer.Start(); 
            jumpReleased = false;
        }
        else
        {
            jumpBufferTimer.Stop(); 
            jumpReleased = true;
        }
    }

    private void HandleMovement()
    {
        // movement code used from the following video: https://www.youtube.com/watch?v=KbtcEVCM7bw
        float targetSpeed = wallJumpInputLockTimer.IsRunning ? 0f : moveInput.x * moveSpeed;
        float speedDif = targetSpeed - rb.linearVelocityX;
        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : decceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDif) * accelRate, velPower) * Mathf.Sign(speedDif);

        // reduces lateral movement if player is in the air
        if (!isGrounded) movement *= airControlMultiplier;

        rb.AddForce(movement * Vector2.right);
    }

    private void HandleJump()
    {
        if (!jumpBufferTimer.IsRunning) return;

        if (isWallSliding)
        {
            // applies an impulse opposite of wall contact direction
            // rb.linearVelocity = Vector2.zero;
            rb.linearVelocityY = 0f;
            rb.AddForce(new Vector2(-wallContactDirection * wallJumpForceX, wallJumpForceY), ForceMode2D.Impulse);
            jumpReleased = false;
            jumpBufferTimer.Stop();
            wallJumpInputLockTimer.Start();
            return;
        }

        // jump on the ground
        if (isGrounded || coyoteTimer.IsRunning)
        {
            // applies an impulse up
            rb.linearVelocityY = 0f;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpReleased = false;
            jumpBufferTimer.Stop();
            coyoteTimer.Stop();
        }
    }

    private void HandleGravity()
    {
        if (isWallSliding) return;

        // increase gravity when falling (i.e. when y velocity is negative)
        if (rb.linearVelocityY < 0f)
        {
            rb.AddForce((fallingGravityMultiplier - 1f) * Physics2D.gravity.y * rb.mass * Vector2.up);
        }
        // increase gravity when jump is released early to cut gravity
        else if (rb.linearVelocityY > 0f && jumpReleased)
        {
            rb.AddForce((jumpCutGravityMultiplier - 1f) * Physics2D.gravity.y * rb.mass * Vector2.up);
        }
    }

    private void HandleWallSlide()
    {
        if (isWallSliding && rb.linearVelocityY < wallSlideSpeed)
            rb.linearVelocityY = wallSlideSpeed;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (groundCheckTransform != null)
            Gizmos.DrawWireCube(groundCheckTransform.position, groundCheckSize);

        Gizmos.color = Color.blue;
        if (wallCheckLeft != null) Gizmos.DrawWireCube(wallCheckLeft.position, wallCheckSize);
        if (wallCheckRight != null) Gizmos.DrawWireCube(wallCheckRight.position, wallCheckSize);
    }
}