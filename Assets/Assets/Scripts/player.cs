using UnityEngine;
using UnityEngine.InputSystem;

public class player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    public CapsuleCollider2D playercollider;

    // NEW: direct reference to the jump action for reliable polling
    private PlayerInput playerInput;
    private InputAction jumpAction;

    [Header("Jump Settings")]
    public float jumpForce = 15f;
    public float doubleJumpForce = 12f;
    public float jumpCutMultiplier = 0.4f;
    public bool isGrounded;

    [Header("Gravity Settings")]
    public float normalGravity = 3f;
    public float jumpGravity = 2.5f;
    public float fallGravity = 5f;

    [Header("Buffer & Coyote")]
    public float jumpBufferTime = 0.1f;
    private float jumpBufferTimer;

    [Header("Double Jump")]
    public bool enableDoubleJump = true;
    private int jumpsRemaining;
    private int maxJumps = 2;

    [Header("Movement")]
    public float speed = 10f;
    private Vector2 MoveInput;
    private int facingDirection = 1;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Slide Settings")]
    public float slideSpeed = 15f;
    public float slideDuration = 0.4f;
    public float slideCooldown = 0.6f;
    public float slideHeight = 1.5f;
    public Vector2 slideOffset = new Vector2(0, -0.88f);
    public float normalHeight = 2.97f;
    public Vector2 normalOffset = new Vector2(0, -0.14f);

    private bool isSliding;
    private bool canSlide = true;
    private bool isJumpHeld;
    private bool canVariableJump;
    private float slideTimer;
    private float slideCooldownTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // NEW: grab the Jump action so we can poll it
        playerInput = GetComponent<PlayerInput>();
        jumpAction = playerInput.actions["Jump"];

        maxJumps = enableDoubleJump ? 2 : 1;
        jumpsRemaining = maxJumps;

        if (playercollider != null)
        {
            playercollider.size = new Vector2(playercollider.size.x, normalHeight);
            playercollider.offset = normalOffset;
        }
    }

    void Update()
    {
        HandleSlideTimers();
        UpdateAnimations();
        Flip();

        if (jumpBufferTimer > 0) jumpBufferTimer -= Time.deltaTime;

        // NEW: poll jump input for reliable press + release detection
        PollJumpInput();
    }

    void FixedUpdate()
    {
        CheckGrounded();
        HandleJumpLogic();
        ApplyVariableGravity();
        HandleMovement();
    }

    // NEW: replaces OnJump entirely
    private void PollJumpInput()
    {
        bool pressed = jumpAction.IsPressed();

        // Rising edge: just pressed this frame
        if (pressed && !isJumpHeld)
        {
            isJumpHeld = true;
            jumpBufferTimer = jumpBufferTime;

            // Air double-jump
            if (!isGrounded && jumpsRemaining > 0)
            {
                ExecuteJump(doubleJumpForce);
            }
        }
        // Falling edge: just released this frame
        else if (!pressed && isJumpHeld)
        {
            isJumpHeld = false;

            if (rb.linearVelocity.y > 0 && canVariableJump)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
                canVariableJump = false;
            }
        }
    }

    private void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            jumpsRemaining = maxJumps;
            canVariableJump = false;
        }
    }

    private void HandleJumpLogic()
    {
        if (jumpBufferTimer > 0 && isGrounded)
        {
            ExecuteJump(jumpForce);
        }
    }

    private void ExecuteJump(float force)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        jumpBufferTimer = 0;
        canVariableJump = true;
        jumpsRemaining--;

        // NEW: if the player already let go before this ran (quick tap),
        // cut the jump immediately. This fixes the timing race.
        if (!isJumpHeld)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            canVariableJump = false;
        }
    }

    private void ApplyVariableGravity()
    {
        if (rb.linearVelocity.y < -0.1f)
            rb.gravityScale = fallGravity;
        else if (rb.linearVelocity.y > 0.1f && isJumpHeld)
            rb.gravityScale = jumpGravity;
        else
            rb.gravityScale = normalGravity;
    }

    private void HandleMovement()
    {
        float targetSpeed = isSliding ? facingDirection * slideSpeed : MoveInput.x * speed;
        rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
    }

    // OnJump is GONE — replaced by PollJumpInput above

    public void OnMove(InputValue value) => MoveInput = value.Get<Vector2>();

    public void OnSlide(InputValue value)
    {
        if (value.isPressed && canSlide && !isSliding && isGrounded && MoveInput.x != 0)
            StartSlide();
    }

    private void StartSlide()
    {
        isSliding = true;
        canSlide = false;
        slideTimer = slideDuration;
        playercollider.size = new Vector2(playercollider.size.x, slideHeight);
        playercollider.offset = slideOffset;
    }

    private void StopSlide()
    {
        isSliding = false;
        slideCooldownTimer = slideCooldown;
        playercollider.size = new Vector2(playercollider.size.x, normalHeight);
        playercollider.offset = normalOffset;
    }

    private void HandleSlideTimers()
    {
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0) StopSlide();
        }
        else if (!canSlide)
        {
            slideCooldownTimer -= Time.deltaTime;
            if (slideCooldownTimer <= 0) canSlide = true;
        }
    }

    private void UpdateAnimations()
    {
        anim.SetBool("isWalking", MoveInput.x != 0 && isGrounded && !isSliding);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isSliding", isSliding);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    private void Flip()
    {
        if (isSliding) return;
        if (MoveInput.x > 0.1f) { facingDirection = 1; spriteRenderer.flipX = false; }
        else if (MoveInput.x < -0.1f) { facingDirection = -1; spriteRenderer.flipX = true; }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}