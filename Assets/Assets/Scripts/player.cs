using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // Required for the Dash Coroutine

public class player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    [Header("Required References")]
    public CapsuleCollider2D playercollider;
    public Transform groundCheck;
    public LayerMask groundLayer;

    private PlayerInput playerInput;
    private InputAction jumpAction;

    [Header("Movement Settings")]
    public float speed = 10f;
    private Vector2 MoveInput;
    private int facingDirection = 1;

    [Header("Jump Settings")]
    public float jumpForce = 15f;
    public float doubleJumpForce = 12f;
    public float jumpCutMultiplier = 0.4f;
    public bool isGrounded;
    public float groundCheckRadius = 0.2f;

    [Header("Gravity Settings")]
    public float normalGravity = 3f;
    public float jumpGravity = 2.5f;
    public float fallGravity = 5f;

    [Header("Jump Helpers")]
    public float jumpBufferTime = 0.1f;
    private float jumpBufferTimer;
    public bool enableDoubleJump = true;
    public int jumpsRemaining;
    private int maxJumps = 2;
    private bool isJumpHeld;
    private bool canVariableJump;

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
    private float slideTimer;
    private float slideCooldownTimer;

    [Header("Dash Settings (Air Only)")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private bool isDashing;
    private bool canDash = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // Using GetComponentInChildren in case the visuals are on a child object
        anim = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        playerInput = GetComponent<PlayerInput>();
        jumpAction = playerInput.actions["Jump"];

        maxJumps = enableDoubleJump ? 2 : 1;
        jumpsRemaining = maxJumps;

        // Initialize collider
        if (playercollider != null)
        {
            playercollider.size = new Vector2(playercollider.size.x, normalHeight);
            playercollider.offset = normalOffset;
        }
        else
        {
            Debug.LogError("Producer Warning: Drag your CapsuleCollider2D into the Playercollider slot in the Inspector!");
        }
    }

    void Update()
    {
        // If we are dashing, we skip normal movement logic
        if (isDashing) return;

        HandleSlideTimers();
        UpdateAnimations();
        Flip();

        if (jumpBufferTimer > 0) jumpBufferTimer -= Time.deltaTime;

        PollJumpInput();
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        CheckGrounded();
        HandleJumpLogic();
        ApplyVariableGravity();
        HandleMovement();
    }

    // --- INPUT SYSTEM EVENTS ---

    public void OnMove(InputValue value) => MoveInput = value.Get<Vector2>();

    public void OnSlide(InputValue value)
    {
        if (value.isPressed && canSlide && !isSliding && isGrounded && MoveInput.x != 0)
            StartSlide();
    }

    public void OnDash(InputValue value)
    {
        // Dash only allowed if: Button pressed + Not grounded + Cooldown ready + Not sliding
        if (value.isPressed && canDash && !isGrounded && !isSliding)
        {
            StartCoroutine(PerformDash());
        }
    }

    // --- DASH COROUTINE ---
    private IEnumerator PerformDash()
    {
        canDash = false;
        isDashing = true;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f; // Freeze gravity for a linear dash

        // Dash in the direction the player is currently facing
        rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // --- MOVEMENT LOGIC ---

    private void HandleMovement()
    {
        float targetSpeed = isSliding ? facingDirection * slideSpeed : MoveInput.x * speed;
        rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
    }

    private void PollJumpInput()
    {
        bool pressed = jumpAction.IsPressed();

        if (pressed && !isJumpHeld)
        {
            isJumpHeld = true;
            jumpBufferTimer = jumpBufferTime;

            if (!isGrounded && jumpsRemaining > 0)
            {
                ExecuteJump(doubleJumpForce);
            }
        }
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
            // Producer Note: This allows dash to reset immediately upon landing
            if (!isDashing) canDash = true;
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

    // --- SLIDE LOGIC ---

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
        anim.SetBool("isDashing", isDashing); // Link to Animator
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
    }
    public void NotifyMushroomBounce()
    {
        // Optional: play animation, sound, or state change
        Debug.Log("Player bounced on mushroom!");
    }
    private void Flip()
    {
        if (isSliding || isDashing) return;
        if (MoveInput.x > 0.1f) { facingDirection = 1; spriteRenderer.flipX = false; }
        else if (MoveInput.x < -0.1f) { facingDirection = -1; spriteRenderer.flipX = true; }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}