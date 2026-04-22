using UnityEngine;
using UnityEngine.InputSystem;

public class player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    public CapsuleCollider2D playercollider;

    // ─────────────────────────────────────────────
    //  JUMP
    // ─────────────────────────────────────────────
    [Header("Jump")]
    public float jumpForce = 5f;
    public bool isGrounded = true;

    [Header("Variable Jump Height")]
    public float jumpCutMultiplier = 0.5f;

    [Header("Variable Gravity")]
    public float normalGravity = 1f;
    public float jumpGravity = 1f;
    public float fallGravity = 2.5f;

    [Header("Jump Buffer")]
    public float jumpBufferTime = 0.15f;
    private float jumpBufferTimer = 0f;
    private bool jumpReleased = false;

    // ─────────────────────────────────────────────
    //  DOUBLE JUMP
    // ─────────────────────────────────────────────
    [Header("Double Jump")]
    public bool enableDoubleJump = true;
    public float doubleJumpForce = 4.5f;
    private int jumpsRemaining = 1;
    private int maxJumps = 2;

    // ─────────────────────────────────────────────
    //  MOVEMENT
    // ─────────────────────────────────────────────
    [Header("Movement Variables")]
    public float speed = 5f;
    public int facingDirection = 1;
    private Vector2 MoveInput;

    // ─────────────────────────────────────────────
    //  GROUND CHECK
    // ─────────────────────────────────────────────
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    // ─────────────────────────────────────────────
    //  SLIDE
    // ─────────────────────────────────────────────
    [Header("Slide")]
    public float slideSpeed = 10f;
    public float slideDuration = 0.4f;
    public float slideCooldown = 0.8f;

    [Tooltip("The height of the collider while sliding")]
    public float slideHeight;
    [Tooltip("The Y offset to keep the collider on the ground while sliding")]
    public Vector2 slideOffset;

    [Tooltip("Your character's standing height")]
    public float normalHeight;
    [Tooltip("Your character's standing Y offset")]
    public Vector2 normalOffset;

    private bool isSliding = false;
    private bool canSlide = true;
    private float slideTimer = 0f;
    private float slideCooldownTimer = 0f;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (playercollider != null)
        {
            playercollider.size = new Vector2(playercollider.size.x, normalHeight);
            playercollider.offset = normalOffset;
        }

        maxJumps = enableDoubleJump ? 2 : 1;
        jumpsRemaining = maxJumps;
        rb.gravityScale = normalGravity;
    }

    // ─────────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────────
    void Update()
    {
        HandleSlideTimers();
        UpdateAnimations();
        Flip();
    }

    // ─────────────────────────────────────────────
    //  FIXED UPDATE
    // ─────────────────────────────────────────────
    void FixedUpdate()
    {
        CheckGrounded();
        ApplyVariableGravity();
        HandleMovement();
        HandleJump(); // ← BUG FIX: this was missing before, so jump cut never ran
    }

    // ─────────────────────────────────────────────
    //  GROUND CHECK
    // ─────────────────────────────────────────────
    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            jumpsRemaining = maxJumps;
            rb.gravityScale = normalGravity;
        }
    }

    // ─────────────────────────────────────────────
    //  HANDLE JUMP (called every FixedUpdate)
    // ─────────────────────────────────────────────
    private void HandleJump()
    {
        // Count down buffer
        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.fixedDeltaTime;

        // Fire buffered ground jump the moment we land
        if (jumpBufferTimer > 0f && isGrounded && jumpsRemaining == maxJumps)
        {
            ExecuteJump(jumpForce);
            return;
        }

        // Jump cut: player released Space while still rising → short hop
        if (jumpReleased)
        {
            if (rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            jumpReleased = false;
        }
    }

    private void ExecuteJump(float force)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        jumpBufferTimer = 0f;
        jumpReleased = false;
        isGrounded = false;
        jumpsRemaining--;
    }

    // ─────────────────────────────────────────────
    //  VARIABLE GRAVITY
    // ─────────────────────────────────────────────
    private void ApplyVariableGravity()
    {
        if (rb.linearVelocity.y < -0.1f) rb.gravityScale = fallGravity;
        else if (rb.linearVelocity.y > 0.1f) rb.gravityScale = jumpGravity;
        else rb.gravityScale = normalGravity;
    }

    // ─────────────────────────────────────────────
    //  MOVEMENT
    // ─────────────────────────────────────────────
    private void HandleMovement()
    {
        if (isSliding)
            rb.linearVelocity = new Vector2(facingDirection * slideSpeed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(MoveInput.x * speed, rb.linearVelocity.y);
    }

    // ─────────────────────────────────────────────
    //  SLIDE
    // ─────────────────────────────────────────────
    private void HandleSlideTimers()
    {
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f) StopSlide();
        }

        if (!canSlide)
        {
            slideCooldownTimer -= Time.deltaTime;
            if (slideCooldownTimer <= 0f) canSlide = true;
        }
    }

    private void StopSlide(bool triggerCooldown = true)
    {
        isSliding = false;
        anim.SetBool("isSliding", false);

        playercollider.size = new Vector2(playercollider.size.x, normalHeight);
        playercollider.offset = normalOffset;

        if (triggerCooldown)
        {
            canSlide = false;
            slideCooldownTimer = slideCooldown;
        }
    }

    // ─────────────────────────────────────────────
    //  ANIMATIONS
    // ─────────────────────────────────────────────
    private void UpdateAnimations()
    {
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        anim.SetBool("isWalking", MoveInput.x != 0 && !isSliding && isGrounded);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isSliding", isSliding);
        anim.SetBool("isJumping", rb.linearVelocity.y > 0.5f);
        anim.SetBool("isFalling", rb.linearVelocity.y < -0.5f);
    }

    // ─────────────────────────────────────────────
    //  FLIP
    // ─────────────────────────────────────────────
    private void Flip()
    {
        if (isSliding) return;

        if (MoveInput.x > 0.1f)
        {
            facingDirection = 1;
            spriteRenderer.flipX = false;
        }
        else if (MoveInput.x < -0.1f)
        {
            facingDirection = -1;
            spriteRenderer.flipX = true;
        }
    }

    // ─────────────────────────────────────────────
    //  INPUT CALLBACKS
    // ─────────────────────────────────────────────
    public void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }

    // BUG FIX: OnJump was defined twice before — now there is only ONE
    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            jumpBufferTimer = jumpBufferTime;
            jumpReleased = false;

            // Double jump fires immediately while airborne
            if (!isGrounded && jumpsRemaining > 0)
                ExecuteJump(doubleJumpForce);
        }
        else
        {
            // Released → trigger jump cut next FixedUpdate = short hop
            jumpReleased = true;
        }
    }

    public void OnSlide(InputValue value)
    {
        if (value.isPressed && canSlide && !isSliding && MoveInput.x != 0 && isGrounded)
        {
            isSliding = true;
            slideTimer = slideDuration;
            anim.SetBool("isSliding", true);

            playercollider.size = new Vector2(playercollider.size.x, slideHeight);
            playercollider.offset = slideOffset;
        }
    }

    // ─────────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}