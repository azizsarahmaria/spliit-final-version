using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private Vector2 moveInput;
    private int facingDirection = 1;

    [Header("Jump Settings")]
    public float jumpForce = 15f;
    public float doubleJumpForce = 12f;
    public float jumpCutMultiplier = 0.4f;
    public bool isGrounded;
    public float groundCheckRadius = 0.2f;

    [Header("Variable Jump (HOLD)")]
    public float jumpHoldForce = 20f;
    public float maxJumpHoldTime = 0.2f;
    private float jumpHoldTimer;

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
    public Vector2 slideOffset = new Vector2(0f, -0.88f);
    public float normalHeight = 2.97f;
    public Vector2 normalOffset = new Vector2(0f, -0.14f);

    private bool isSliding;
    private bool canSlide = true;
    private float slideTimer;
    private float slideCooldownTimer;
    private MovingPlatform currentPlatform;
    private readonly HashSet<Collider2D> groundedColliders = new HashSet<Collider2D>();

    [Header("Respawn Logic")]
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        playerInput = GetComponent<PlayerInput>();
        jumpAction = playerInput != null ? playerInput.actions["Jump"] : null;

        maxJumps = enableDoubleJump ? 2 : 1;
        jumpsRemaining = maxJumps;

        if (playercollider == null)
            playercollider = GetComponent<CapsuleCollider2D>();

        if (GameManager.instance != null && GameManager.instance.lastCheckpointPos == Vector2.zero)
            GameManager.instance.lastCheckpointPos = transform.position;
    }

    void Update()
    {
        if (isDead) return;

        UpdateAnimations();
        HandleSlideTimers();
        Flip();

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;

        PollJumpInput();
    }

    void FixedUpdate()
    {
        if (isDead) return;

        CheckGrounded();
        HandleJumpLogic();
        ApplyJumpHold();
        ApplyVariableGravity();
        HandleMovement();
    }

    // --- RESPAWN LOGIC ---
    public void Die()
    {
        if (isDead) return;

        isDead = true;
        rb.linearVelocity = Vector2.zero;

        if (GameManager.instance != null)
            GameManager.instance.PlayerDied();

        if (GameManager.instance == null || GameManager.instance.playerLives > 0)
            Invoke(nameof(RespawnAtCheckpoint), 1f);
    }

    public void Respawn(Vector2 position)
    {
        isDead = false;
        transform.position = position;
        rb.linearVelocity = Vector2.zero;
        jumpsRemaining = maxJumps;
    }

    private void RespawnAtCheckpoint()
    {
        Vector2 pos = (GameManager.instance != null && GameManager.instance.lastCheckpointPos != Vector2.zero)
            ? GameManager.instance.lastCheckpointPos
            : transform.position;

        Respawn(pos);
    }
    // --- END RESPAWN LOGIC ---

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Hazard"))
            Die();
    }

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

    public void OnSlide(InputValue value)
    {
        if (value.isPressed && canSlide && !isSliding && isGrounded && Mathf.Abs(moveInput.x) > 0.01f)
            StartSlide();
    }

    private void HandleMovement()
    {
        float platformVelX = currentPlatform != null ? currentPlatform.PlatformVelocity.x : 0f;
        float platformVelY = currentPlatform != null ? currentPlatform.PlatformVelocity.y : 0f;

        float targetSpeed = isSliding ? facingDirection * slideSpeed : moveInput.x * speed;

        rb.linearVelocity = new Vector2(targetSpeed + platformVelX, rb.linearVelocity.y + platformVelY);
    }

    private void PollJumpInput()
    {
        if (jumpAction == null) return;

        bool pressed = jumpAction.IsPressed();

        if (pressed && !isJumpHeld)
        {
            isJumpHeld = true;
            jumpBufferTimer = jumpBufferTime;

            if (!isGrounded && jumpsRemaining > 0)
                ExecuteJump(doubleJumpForce);
        }
        else if (!pressed && isJumpHeld)
        {
            isJumpHeld = false;

            if (rb.linearVelocity.y > 0f && canVariableJump)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
                canVariableJump = false;
            }
        }
    }

    private void CheckGrounded()
    {
        bool groundedByOverlap = false;

        if (groundCheck != null)
            groundedByOverlap = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        isGrounded = groundedByOverlap || groundedColliders.Count > 0;

        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            jumpsRemaining = maxJumps;
            canVariableJump = false;
        }
    }

    private void HandleJumpLogic()
    {
        if (jumpBufferTimer > 0f && isGrounded)
            ExecuteJump(jumpForce);
    }

    private void ExecuteJump(float force)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);

        jumpHoldTimer = maxJumpHoldTime;
        canVariableJump = true;

        jumpBufferTimer = 0f;
        jumpsRemaining--;
    }

    private void ApplyJumpHold()
    {
        if (isJumpHeld && canVariableJump && jumpHoldTimer > 0f)
        {
            rb.AddForce(Vector2.up * jumpHoldForce, ForceMode2D.Force);
            jumpHoldTimer -= Time.fixedDeltaTime;
        }
        else
        {
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
            if (slideTimer <= 0f)
                StopSlide();
        }
        else if (!canSlide)
        {
            slideCooldownTimer -= Time.deltaTime;
            if (slideCooldownTimer <= 0f)
                canSlide = true;
        }
    }

    private void UpdateAnimations()
    {
        if (anim == null || rb == null) return;

        anim.SetBool("isWalking", Mathf.Abs(moveInput.x) > 0.01f && isGrounded && !isSliding);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isJumping", !isGrounded && rb.linearVelocity.y > 0.1f);
        anim.SetBool("isFalling", !isGrounded && rb.linearVelocity.y < -0.1f);
        anim.SetBool("isSliding", isSliding);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    private void Flip()
    {
        if (isSliding) return;

        if (moveInput.x > 0.1f)
        {
            facingDirection = 1;
            spriteRenderer.flipX = false;
        }
        else if (moveInput.x < -0.1f)
        {
            facingDirection = -1;
            spriteRenderer.flipX = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        UpdateGroundedCollision(col);

        if (IsStandingOnCollision(col))
            currentPlatform = col.gameObject.GetComponent<MovingPlatform>();
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        UpdateGroundedCollision(col);

        if (currentPlatform == null && IsStandingOnCollision(col))
            currentPlatform = col.gameObject.GetComponent<MovingPlatform>();
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        groundedColliders.Remove(col.collider);

        if (currentPlatform != null && col.gameObject.GetComponent<MovingPlatform>() == currentPlatform)
            currentPlatform = null;
    }

    private void UpdateGroundedCollision(Collision2D col)
    {
        if (!IsGroundSurface(col.collider)) return;

        if (IsStandingOnCollision(col))
            groundedColliders.Add(col.collider);
        else
            groundedColliders.Remove(col.collider);
    }

    private bool IsStandingOnCollision(Collision2D col)
    {
        for (int i = 0; i < col.contactCount; i++)
        {
            if (col.GetContact(i).normal.y > 0.5f)
                return true;
        }
        return false;
    }

    private bool IsGroundSurface(Collider2D col)
    {
        bool layerMatches = groundLayer.value != 0 && (groundLayer.value & (1 << col.gameObject.layer)) != 0;
        return layerMatches || col.CompareTag("Ground") || col.GetComponent<MovingPlatform>() != null;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}