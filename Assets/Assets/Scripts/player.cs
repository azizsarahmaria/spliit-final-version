using UnityEngine;
using UnityEngine.InputSystem;

public class player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer; // FIX 0: cached so Flip() doesn't use fragile GetChild(0)
    public CapsuleCollider2D playercollider;

    // ─────────────────────────────────────────────
    //  JUMP
    // ─────────────────────────────────────────────
    [Header("Jump")]
    public float jumpForce = 5f;
    private bool isGrounded = true;

    [Header("Variable Jump Height")]
    public float jumpCutMultiplier = 0.5f; // releasing early multiplies upward velocity by this

    [Header("Variable Gravity")]
    public float normalGravity = 1f;   // on the ground / at apex
    public float jumpGravity = 1f;   // while rising  (keep at 1 or slightly above)
    public float fallGravity = 2.5f; // while falling (makes the arc snappy)

    [Header("Jump Buffer")]
    public float jumpBufferTime = 0.15f; // seconds the jump input is remembered before landing
    private float jumpBufferTimer = 0f;
    private bool jumpReleased = false;   // flag: player let go of jump button

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
    public Transform groundCheck;           // empty child GameObject placed at the player's feet
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;           // set this to your Ground layer in the Inspector

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
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // FIX 0

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
    //  UPDATE  (timers + animations + visuals)
    // ─────────────────────────────────────────────
    void Update()
    {
        HandleSlideTimers();
        UpdateAnimations();
        Flip();
    }

    // ─────────────────────────────────────────────
    //  FIXED UPDATE  (all physics here)
    // ─────────────────────────────────────────────
    void FixedUpdate()
    {
        CheckGrounded();        // FIX 1: reliable overlap-circle ground detection
        ApplyVariableGravity(); // FIX 3: 3-state gravity
        HandleMovement();
        HandleJump();           // FIX 2: jump buffer + jump cut live here
    }

    // ─────────────────────────────────────────────
    //  FIX 1 — GROUND CHECK (OverlapCircle)
    // ─────────────────────────────────────────────
    // Replaces the old OnCollisionEnter/Exit pair which could desync.
    // Steps to set up in Unity:
    //   1. Right-click your Player in the Hierarchy → Create Empty → name it "GroundCheck"
    //   2. Move it to the bottom of your sprite (feet level)
    //   3. Drag it into the "Ground Check" field on this script in the Inspector
    //   4. Set "Ground Layer" to whatever layer your ground tiles are on
    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // reset jumps the moment we touch ground
        if (isGrounded && !wasGrounded)
        {
            jumpsRemaining = maxJumps;
            rb.gravityScale = normalGravity;
        }
    }

    // ─────────────────────────────────────────────
    //  FIX 2 — JUMP BUFFER + JUMP CUT
    // ─────────────────────────────────────────────
    // Jump buffer:  pressing jump up to 0.15s before landing still fires the jump.
    // Jump cut:     releasing the button early cuts upward velocity by jumpCutMultiplier,
    //               giving a short hop vs a full hold jump.
    private void HandleJump()
    {
        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.fixedDeltaTime;

        // consume a buffered jump the moment we are grounded
        if (jumpBufferTimer > 0f && isGrounded && jumpsRemaining == maxJumps)
        {
            ExecuteJump(jumpForce);
            return;
        }

        // jump cut: player released button while still rising
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
    //  FIX 3 — 3-STATE VARIABLE GRAVITY
    // ─────────────────────────────────────────────
    // Rising  → jumpGravity  (tune the rise arc)
    // Falling → fallGravity  (tune the fall arc, higher = snappier)
    // Ground  → normalGravity
    private void ApplyVariableGravity()
    {
        if (rb.linearVelocity.y < -0.1f)
            rb.gravityScale = fallGravity;
        else if (rb.linearVelocity.y > 0.1f)
            rb.gravityScale = jumpGravity;
        else
            rb.gravityScale = normalGravity;
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
            if (slideTimer <= 0f)
                StopSlide();
        }

        if (!canSlide)
        {
            slideCooldownTimer -= Time.deltaTime;
            if (slideCooldownTimer <= 0f)
                canSlide = true;
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
        anim.SetBool("isWalking", MoveInput.x != 0 && !isSliding && isGrounded);
        anim.SetBool("isJumping", !isGrounded);
    }

    // ─────────────────────────────────────────────
    //  FLIP  (FIX 0 — uses cached spriteRenderer)
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

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            jumpBufferTimer = jumpBufferTime; // always store buffer on press
            jumpReleased = false;

            // if airborne and double jump available, fire immediately
            if (!isGrounded && jumpsRemaining > 0)
                ExecuteJump(doubleJumpForce);
        }
        else
        {
            // released → trigger jump cut in next FixedUpdate
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