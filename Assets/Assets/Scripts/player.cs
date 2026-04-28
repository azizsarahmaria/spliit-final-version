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

    [Header("Dash Settings")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private bool isDashing;
    private bool canDash = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        playerInput = GetComponent<PlayerInput>();
        jumpAction = playerInput.actions["Jump"];

        maxJumps = enableDoubleJump ? 2 : 1;
        jumpsRemaining = maxJumps;

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
        UpdateAnimations();

        if (isDashing) return;

        HandleSlideTimers();
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
        // Dash is now allowed on both ground and air
        if (value.isPressed && canDash && !isSliding)
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
        rb.gravityScale = 0f;

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

        float platformX = 0f;
        if (isGrounded)
        {
            // Raycast down to check if we're standing on a moving platform
            RaycastHit2D hit = Physics2D.Raycast(
                groundCheck.position,
                Vector2.down,
                groundCheckRadius + 0.2f,
                groundLayer
            );
            if (hit.collider != null)
            {
                MovingPlatform platform = hit.collider.GetComponent<MovingPlatform>();
                if (platform != null)
                    platformX = platform.PlatformVelocity.x;
            }
        }

        rb.linearVelocity = new Vector2(targetSpeed + platformX, rb.linearVelocity.y);
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
        else if (rb.linearVelocity.y > 0.1f && isJumpHeld && canVariableJump)
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
        anim.SetBool("isDashing", isDashing);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    public void NotifyMushroomBounce()
    {
        Debug.Log("Player bounced ousing UnityEngine;\r\nusing UnityEngine.InputSystem;\r\nusing System.Collections;\r\n\r\npublic class player : MonoBehaviour\r\n{\r\n    private Rigidbody2D rb;\r\n    private Animator anim;\r\n    private SpriteRenderer spriteRenderer;\r\n\r\n    [Header(\"Required References\")]\r\n    public CapsuleCollider2D playercollider;\r\n    public Transform groundCheck;\r\n    public LayerMask groundLayer;\r\n\r\n    private PlayerInput playerInput;\r\n    private InputAction jumpAction;\r\n\r\n    [Header(\"Movement Settings\")]\r\n    public float speed = 10f;\r\n    private Vector2 MoveInput;\r\n    private int facingDirection = 1;\r\n\r\n    [Header(\"Jump Settings\")]\r\n    public float jumpForce = 15f;\r\n    public float doubleJumpForce = 12f;\r\n    public float jumpCutMultiplier = 0.4f;\r\n    public bool isGrounded;\r\n    public float groundCheckRadius = 0.2f;\r\n\r\n    [Header(\"Gravity Settings\")]\r\n    public float normalGravity = 3f;\r\n    public float jumpGravity = 2.5f;\r\n    public float fallGravity = 5f;\r\n\r\n    [Header(\"Jump Helpers\")]\r\n    public float jumpBufferTime = 0.1f;\r\n    private float jumpBufferTimer;\r\n    public bool enableDoubleJump = true;\r\n    public int jumpsRemaining;\r\n    private int maxJumps = 2;\r\n    private bool isJumpHeld;\r\n    private bool canVariableJump;\r\n\r\n    [Header(\"Slide Settings\")]\r\n    public float slideSpeed = 15f;\r\n    public float slideDuration = 0.4f;\r\n    public float slideCooldown = 0.6f;\r\n    public float slideHeight = 1.5f;\r\n    public Vector2 slideOffset = new Vector2(0, -0.88f);\r\n    public float normalHeight = 2.97f;\r\n    public Vector2 normalOffset = new Vector2(0, -0.14f);\r\n\r\n    private bool isSliding;\r\n    private bool canSlide = true;\r\n    private float slideTimer;\r\n    private float slideCooldownTimer;\r\n\r\n    [Header(\"Dash Settings\")]\r\n    public float dashSpeed = 25f;\r\n    public float dashDuration = 0.2f;\r\n    public float dashCooldown = 1f;\r\n    private bool isDashing;\r\n    private bool canDash = true;\r\n\r\n    // ── Dash hit tracking ──────────────────────────────────── // ← NEW\r\n    // Prevents hitting the same enemy twice in one dash          // ← NEW\r\n    private Collider2D lastDashHitEnemy = null;                  // ← NEW\r\n\r\n    void Start()\r\n    {\r\n        rb = GetComponent<Rigidbody2D>();\r\n        anim = GetComponentInChildren<Animator>();\r\n        spriteRenderer = GetComponentInChildren<SpriteRenderer>();\r\n\r\n        playerInput = GetComponent<PlayerInput>();\r\n        jumpAction = playerInput.actions[\"Jump\"];\r\n\r\n        maxJumps = enableDoubleJump ? 2 : 1;\r\n        jumpsRemaining = maxJumps;\r\n\r\n        if (playercollider != null)\r\n        {\r\n            playercollider.size = new Vector2(playercollider.size.x, normalHeight);\r\n            playercollider.offset = normalOffset;\r\n        }\r\n        else\r\n        {\r\n            Debug.LogError(\"Producer Warning: Drag your CapsuleCollider2D into the Playercollider slot in the Inspector!\");\r\n        }\r\n    }\r\n\r\n    void Update()\r\n    {\r\n        UpdateAnimations();\r\n        if (isDashing) return;\r\n\r\n        HandleSlideTimers();\r\n        Flip();\r\n\r\n        if (jumpBufferTimer > 0) jumpBufferTimer -= Time.deltaTime;\r\n        PollJumpInput();\r\n    }\r\n\r\n    void FixedUpdate()\r\n    {\r\n        if (isDashing) return;\r\n\r\n        CheckGrounded();\r\n        HandleJumpLogic();\r\n        ApplyVariableGravity();\r\n        HandleMovement();\r\n    }\r\n\r\n    // --- INPUT SYSTEM EVENTS ---\r\n\r\n    public void OnMove(InputValue value) => MoveInput = value.Get<Vector2>();\r\n\r\n    public void OnSlide(InputValue value)\r\n    {\r\n        if (value.isPressed && canSlide && !isSliding && isGrounded && MoveInput.x != 0)\r\n            StartSlide();\r\n    }\r\n\r\n    public void OnDash(InputValue value)\r\n    {\r\n        if (value.isPressed && canDash && !isSliding)\r\n            StartCoroutine(PerformDash());\r\n    }\r\n\r\n    // --- DASH COROUTINE ---\r\n\r\n    private IEnumerator PerformDash()\r\n    {\r\n        canDash = false;\r\n        isDashing = true;\r\n        lastDashHitEnemy = null; // ← NEW: reset hit-tracking at the start of each dash\r\n\r\n        float originalGravity = rb.gravityScale;\r\n        rb.gravityScale = 0f;\r\n        rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0f);\r\n\r\n        yield return new WaitForSeconds(dashDuration);\r\n\r\n        rb.gravityScale = originalGravity;\r\n        isDashing = false;\r\n\r\n        yield return new WaitForSeconds(dashCooldown);\r\n        canDash = true;\r\n    }\r\n\r\n    // --- DASH COLLISION DETECTION ---                          // ← NEW (whole section)\r\n\r\n    /// <summary>\r\n    /// Fires when the player's collider physically touches something.\r\n    /// We only care about Enemy contacts during a dash.\r\n    /// </summary>\r\n    private void OnCollisionEnter2D(Collision2D collision)\r\n    {\r\n        if (!isDashing) return;                                  // not dashing → ignore\r\n        if (collision.collider == lastDashHitEnemy) return;      // already hit this enemy this dash\r\n\r\n        Enemy enemy = collision.gameObject.GetComponent<Enemy>();\r\n        if (enemy == null) return;\r\n\r\n        lastDashHitEnemy = collision.collider;                   // record so we don't double-hit\r\n        enemy.TakeDashDamage();\r\n    }\r\n\r\n    // --- MOVEMENT LOGIC ---\r\n\r\n    private void HandleMovement()\r\n    {\r\n        float targetSpeed = isSliding ? facingDirection * slideSpeed : MoveInput.x * speed;\r\n        rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);\r\n    }\r\n\r\n    private void PollJumpInput()\r\n    {\r\n        bool pressed = jumpAction.IsPressed();\r\n\r\n        if (pressed && !isJumpHeld)\r\n        {\r\n            isJumpHeld = true;\r\n            jumpBufferTimer = jumpBufferTime;\r\n\r\n            if (!isGrounded && jumpsRemaining > 0)\r\n                ExecuteJump(doubleJumpForce);\r\n        }\r\n        else if (!pressed && isJumpHeld)\r\n        {\r\n            isJumpHeld = false;\r\n\r\n            if (rb.linearVelocity.y > 0 && canVariableJump)\r\n            {\r\n                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);\r\n                canVariableJump = false;\r\n            }\r\n        }\r\n    }\r\n\r\n    private void CheckGrounded()\r\n    {\r\n        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);\r\n\r\n        if (isGrounded && rb.linearVelocity.y <= 0.1f)\r\n        {\r\n            jumpsRemaining = maxJumps;\r\n            canVariableJump = false;\r\n            if (!isDashing) canDash = true;\r\n        }\r\n    }\r\n\r\n    private void HandleJumpLogic()\r\n    {\r\n        if (jumpBufferTimer > 0 && isGrounded)\r\n            ExecuteJump(jumpForce);\r\n    }\r\n\r\n    private void ExecuteJump(float force)\r\n    {\r\n        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);\r\n        jumpBufferTimer = 0;\r\n        canVariableJump = true;\r\n        jumpsRemaining--;\r\n\r\n        if (!isJumpHeld)\r\n        {\r\n            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);\r\n            canVariableJump = false;\r\n        }\r\n    }\r\n\r\n    private void ApplyVariableGravity()\r\n    {\r\n        if (rb.linearVelocity.y < -0.1f)         rb.gravityScale = fallGravity;\r\n        else if (rb.linearVelocity.y > 0.1f && isJumpHeld) rb.gravityScale = jumpGravity;\r\n        else                                       rb.gravityScale = normalGravity;\r\n    }\r\n\r\n    // --- SLIDE LOGIC ---\r\n\r\n    private void StartSlide()\r\n    {\r\n        isSliding = true;\r\n        canSlide = false;\r\n        slideTimer = slideDuration;\r\n        playercollider.size = new Vector2(playercollider.size.x, slideHeight);\r\n        playercollider.offset = slideOffset;\r\n    }\r\n\r\n    private void StopSlide()\r\n    {\r\n        isSliding = false;\r\n        slideCooldownTimer = slideCooldown;\r\n        playercollider.size = new Vector2(playercollider.size.x, normalHeight);\r\n        playercollider.offset = normalOffset;\r\n    }\r\n\r\n    private void HandleSlideTimers()\r\n    {\r\n        if (isSliding)\r\n        {\r\n            slideTimer -= Time.deltaTime;\r\n            if (slideTimer <= 0) StopSlide();\r\n        }\r\n        else if (!canSlide)\r\n        {\r\n            slideCooldownTimer -= Time.deltaTime;\r\n            if (slideCooldownTimer <= 0) canSlide = true;\r\n        }\r\n    }\r\n\r\n    private void UpdateAnimations()\r\n    {\r\n        anim.SetBool(\"isWalking\",  MoveInput.x != 0 && isGrounded && !isSliding);\r\n        anim.SetBool(\"isGrounded\", isGrounded);\r\n        anim.SetBool(\"isSliding\",  isSliding);\r\n        anim.SetBool(\"isDashing\",  isDashing);\r\n        anim.SetFloat(\"yVelocity\", rb.linearVelocity.y);\r\n    }\r\n\r\n    public void NotifyMushroomBounce() => Debug.Log(\"Player bounced on mushroom!\");\r\n\r\n    private void Flip()\r\n    {\r\n        if (isSliding || isDashing) return;\r\n        if (MoveInput.x > 0.1f)       { facingDirection =  1; spriteRenderer.flipX = false; }\r\n        else if (MoveInput.x < -0.1f) { facingDirection = -1; spriteRenderer.flipX = true;  }\r\n    }\r\n\r\n    private void OnDrawGizmosSelected()\r\n    {\r\n        if (groundCheck) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);\r\n    }\r\n}n mushroom!");
    }

    public void DisableVariableJump()
    {
        canVariableJump = false;
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