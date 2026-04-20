using Unity.VisualScripting;
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

    private Vector2 moveInput;

    [Header("Ground Check")]
    public float groundCheckRadius;
    public Transform groundCheckTransform;
    public LayerMask groundLayer;
    public bool isGrounded;

    [Header("Jump")]
    public float jumpForce;
    public float fallingGravityMultiplier;
    public float airControlMultiplier;
    public float jumpCutGravityMultiplier;
    private bool jumpQueued;
    private bool jumpReleased;
    public float maxCoyoteTime;
    private CountdownTimer coyoteTimer;
    public float maxJumpBufferTime;
    private CountdownTimer jumpBufferTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        coyoteTimer = new CountdownTimer(maxCoyoteTime);
        jumpBufferTimer = new CountdownTimer(maxJumpBufferTime);
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
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(
            groundCheckTransform.position,
            groundCheckRadius,
            groundLayer
        );

        // Start coyote timer the frame we leave the ground
        if (wasGrounded && !isGrounded)
            coyoteTimer.Start();

        // Stop coyote timer the frame we land
        if (!wasGrounded && isGrounded)
            coyoteTimer.Stop();

        coyoteTimer.Tick(Time.deltaTime);
        jumpBufferTimer.Tick(Time.deltaTime);

        // Check jump condition every frame
        if ((isGrounded || coyoteTimer.IsRunning) && jumpBufferTimer.IsRunning)
        {
            jumpQueued = true;
        }

        Debug.Log(jumpBufferTimer.IsRunning);
    }

    // INPUT HANDLERS
    private void OnMove(Vector2 dir)
    {
        moveInput = dir;
    }

    private void OnJump(bool pressed)
    {
        if (pressed)
        {
            jumpBufferTimer.Start(); // always buffer, regardless of grounded state
            jumpReleased = false;
        }
        else
        {
            jumpBufferTimer.Stop();  // released early, cancel buffer
            jumpReleased = true;
        }
    }

    // MOVEMENT
    private void HandleMovement()
    {
        float targetSpeed = moveInput.x * moveSpeed;
        float speedDif = targetSpeed - rb.linearVelocityX;

        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f)
            ? acceleration
            : decceleration;

        float movement = Mathf.Pow(Mathf.Abs(speedDif) * accelRate, velPower) * Mathf.Sign(speedDif);

        // Reduce force when airborne
        if (!isGrounded)
            movement *= airControlMultiplier;

        rb.AddForce(movement * Vector2.right);
    }

    // JUMP
    private void HandleJump()
    {
        if (jumpQueued)
        {
            rb.linearVelocityY = 0;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpQueued = false;
            jumpReleased = false;
            jumpBufferTimer.Stop();  // consume the buffer
            coyoteTimer.Stop();      // consume coyote time so you can't double-jump off it
        }
    }

    private void HandleGravity()
    {
        if (rb.linearVelocityY < 0)
        {
            // Falling — apply extra gravity
            rb.AddForce(Vector2.up * Physics2D.gravity.y * (fallingGravityMultiplier - 1) * rb.mass);
        }
        else if (rb.linearVelocityY > 0 && jumpReleased)
        {
            // Still rising but jump was released early — cut the jump
            rb.AddForce(Vector2.up * Physics2D.gravity.y * (jumpCutGravityMultiplier - 1) * rb.mass);
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleJump();
        HandleGravity();
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheckTransform == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheckTransform.position, groundCheckRadius);
    }
}