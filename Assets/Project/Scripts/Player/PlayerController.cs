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
    private bool isTouchingWallLeft;
    private bool isTouchingWallRight;
    private int wallDirection;
    private bool jumpQueued;
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
        UpdateTimers();

        if ((isGrounded || coyoteTimer.IsRunning) && jumpBufferTimer.IsRunning)
            jumpQueued = true;
    }

    private void UpdateTimers()
    {
        coyoteTimer.Tick(Time.deltaTime);
        jumpBufferTimer.Tick(Time.deltaTime);
        wallJumpInputLockTimer.Tick(Time.deltaTime);
    }

    private void UpdateGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapBox(groundCheckTransform.position, groundCheckSize, 0f, groundLayer);

        if (wasGrounded && !isGrounded) coyoteTimer.Start();
        if (!wasGrounded && isGrounded)
        {
            coyoteTimer.Stop();
        }
    }

    private void UpdateWallContact()
    {
        isTouchingWallLeft = Physics2D.OverlapBox(wallCheckLeft.position, wallCheckSize, 0f, groundLayer);
        isTouchingWallRight = Physics2D.OverlapBox(wallCheckRight.position, wallCheckSize, 0f, groundLayer);

        bool pushingIntoWall = (isTouchingWallLeft && moveInput.x < -0.01f)
                            || (isTouchingWallRight && moveInput.x > 0.01f);

        isWallSliding = !isGrounded && pushingIntoWall;

        if (isWallSliding)
            wallDirection = isTouchingWallLeft ? -1 : 1;
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

    void FixedUpdate()
    {
        HandleMovement();
        HandleJump();
        HandleGravity();
        HandleWallSlide();
    }

    private void HandleMovement()
    {
        float targetSpeed = wallJumpInputLockTimer.IsRunning ? 0f : moveInput.x * moveSpeed;
        float speedDif = targetSpeed - rb.linearVelocityX;
        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : decceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDif) * accelRate, velPower) * Mathf.Sign(speedDif);

        if (!isGrounded)
            movement *= airControlMultiplier;

        rb.AddForce(movement * Vector2.right);
    }

    private void HandleJump()
    {
        if (jumpBufferTimer.IsRunning && isWallSliding)
        {
            rb.linearVelocityY = 0f;
            rb.AddForce(new Vector2(-wallDirection * wallJumpForceX, wallJumpForceY), ForceMode2D.Impulse);
            jumpReleased = false;
            jumpBufferTimer.Stop();
            wallJumpInputLockTimer.Start();
            return;
        }

        if (jumpQueued)
        {
            rb.linearVelocityY = 0f;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpQueued = false;
            jumpReleased = false;
            jumpBufferTimer.Stop();
            coyoteTimer.Stop();
        }
    }

    private void HandleGravity()
    {
        if (isWallSliding) return;

        if (rb.linearVelocityY < 0f)
        {
            rb.AddForce((fallingGravityMultiplier - 1f) * Physics2D.gravity.y * rb.mass * Vector2.up);
        }
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
        if (wallCheckLeft != null)
            Gizmos.DrawWireCube(wallCheckLeft.position, wallCheckSize);
        if (wallCheckRight != null)
            Gizmos.DrawWireCube(wallCheckRight.position, wallCheckSize);
    }
}