using System.Collections;
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

    [Header("Dash Settings")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private bool isDashing;
    private bool canDash = true;

    private Collider2D lastDashHitEnemy;

    public bool IsDashing => isDashing;

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

        if (playercollider != null)
        {
            if (normalHeight <= 0f)
                normalHeight = playercollider.size.y;

            if (normalOffset == Vector2.zero)
                normalOffset = playercollider.offset;

            if (slideHeight <= 0f)
                slideHeight = Mathf.Max(1f, normalHeight * 0.5f);

            if (slideOffset == Vector2.zero)
            {
                float offsetShift = (normalHeight - slideHeight) * 0.5f;
                slideOffset = new Vector2(normalOffset.x, normalOffset.y - offsetShift);
            }

            playercollider.size = new Vector2(playercollider.size.x, normalHeight);
            playercollider.offset = normalOffset;
        }
    }

    void Update()
    {
        UpdateAnimations();

        if (isDashing) return;

        HandleSlideTimers();
        Flip();

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;

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

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

    public void OnSlide(InputValue value)
    {
        if (value.isPressed && canSlide && !isSliding && isGrounded && Mathf.Abs(moveInput.x) > 0.01f)
            StartSlide();
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed && canDash && !isSliding)
            StartCoroutine(PerformDash());
    }

    private IEnumerator PerformDash()
    {
        canDash = false;
        isDashing = true;
        lastDashHitEnemy = null;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDashing) return;
        if (collision.collider == lastDashHitEnemy) return;

        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy == null)
            enemy = collision.gameObject.GetComponentInParent<Enemy>();

        if (enemy == null) return;

        lastDashHitEnemy = collision.collider;
        enemy.TakeDashDamage();
    }

    private void HandleMovement()
    {
        float targetSpeed = isSliding ? facingDirection * slideSpeed : moveInput.x * speed;

        float platformX = 0f;
        if (isGrounded && groundCheck != null)
        {
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
        if (groundCheck == null) return;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            jumpsRemaining = maxJumps;
            canVariableJump = false;

            if (!isDashing)
                canDash = true;
        }
    }

    private void HandleJumpLogic()
    {
        if (jumpBufferTimer > 0f && isGrounded)
            ExecuteJump(jumpForce);
    }

    private void ExecuteJump(float force)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        jumpBufferTimer = 0f;
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

    private void StartSlide()
    {
        if (playercollider == null) return;

        isSliding = true;
        canSlide = false;
        slideTimer = slideDuration;
        playercollider.size = new Vector2(playercollider.size.x, slideHeight);
        playercollider.offset = slideOffset;
    }

    private void StopSlide()
    {
        if (playercollider == null) return;

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
        anim.SetBool("isSliding", isSliding);
        anim.SetBool("isDashing", isDashing);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    public void NotifyMushroomBounce()
    {
        Debug.Log("Player bounced on mushroom!");
    }

    public void DisableVariableJump()
    {
        canVariableJump = false;
    }

    private void Flip()
    {
        if (isSliding || isDashing) return;

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

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
