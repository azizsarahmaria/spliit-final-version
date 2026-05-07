using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Anger : MonoBehaviour
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
    public float normalHeight = 2.97f;
    public Vector2 normalOffset = new Vector2(-1.876f, -0.412f);

    private bool isSliding;
    private bool canSlide = true;
    private float slideTimer;
    private float slideCooldownTimer;
    private MovingPlatform currentPlatform;

    [Header("Dash Settings")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private bool isDashing;
    private bool canDash = true;
    private Collider2D lastDashHitEnemy;

    [SerializeField] private GameObject dashFireEffect;
    [SerializeField] private Animator fireEffectAnimator;

    [Header("Respawn Logic")]
    private bool isDead = false;



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

        normalHeight = playercollider.size.y;
        normalOffset = playercollider.offset;

        // Make sure flipX is OFF on the fire effect so it doesn't fight the localScale flip.
        if (dashFireEffect != null)
        {
            SpriteRenderer fireSR = dashFireEffect.GetComponent<SpriteRenderer>();
            if (fireSR != null) fireSR.flipX = false;
        }

        // After scene reload, lastCheckpointPos is already set — move there
        // On first load, it's zero — store our starting position as default
        if (GameManager.instance != null)
        {
            if (GameManager.instance.lastCheckpointPos != Vector2.zero)
                Respawn(GameManager.instance.lastCheckpointPos);
            else
                GameManager.instance.lastCheckpointPos = transform.position;
        }
    }

    void Update()
    {
        if (isDead) return;

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
        if (isDashing || isDead) return;

        CheckGrounded();
        HandleJumpLogic();
        ApplyJumpHold();
        ApplyVariableGravity();
        HandleMovement();
    }

    // Called by GameManager after scene reload
    public void Respawn(Vector2 position)
    {
        isDead = false;
        transform.position = position;
        rb.linearVelocity = Vector2.zero;
        jumpsRemaining = maxJumps;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        CheckDashHit(col);
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

        // Derive direction from live moveInput, fall back to facingDirection if not holding a direction.
        int dashDir = moveInput.x < -0.1f ? -1 : (moveInput.x > 0.1f ? 1 : facingDirection);

        if (dashFireEffect != null)
        {
            dashFireEffect.SetActive(true);

            // Flip ONLY via localScale. Do NOT also set flipX — two flips cancel each other.
            Vector3 fireScale = dashFireEffect.transform.localScale;
            fireScale.x = Mathf.Abs(fireScale.x) * dashDir;
            dashFireEffect.transform.localScale = fireScale;

            // Position fire on the correct side of the player
            Vector3 firePos = dashFireEffect.transform.localPosition;
            firePos.x = Mathf.Abs(firePos.x) * (dashDir == 1 ? -1 : 1);
            dashFireEffect.transform.localPosition = firePos;

            fireEffectAnimator.SetTrigger("PlayFire");
        }

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        isDashing = false;

        if (dashFireEffect != null)
            dashFireEffect.SetActive(false);

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
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
        if (groundCheck == null) return;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            jumpsRemaining = maxJumps;
            canVariableJump = false;
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
        ResizeColliderKeepingBottom(slideHeight);
    }

    private void StopSlide()
    {
        if (!CanStandUp()) return;
        ResizeColliderKeepingBottom(normalHeight);
        isSliding = false;
        slideCooldownTimer = slideCooldown;
    }

    private void ResizeColliderKeepingBottom(float newHeight)
    {
        Physics2D.SyncTransforms();
        float bottomBefore = playercollider.bounds.min.y;

        float heightDiff = normalHeight - newHeight;
        playercollider.size = new Vector2(playercollider.size.x, newHeight);
        playercollider.offset = new Vector2(normalOffset.x, normalOffset.y - heightDiff * 0.5f);

        Physics2D.SyncTransforms();
        float bottomAfter = playercollider.bounds.min.y;
        float drift = bottomBefore - bottomAfter;

        if (Mathf.Abs(drift) > 0.0001f)
        {
            rb.position = new Vector2(rb.position.x, rb.position.y + drift);
            Physics2D.SyncTransforms();
        }
    }

    private bool CanStandUp()
    {
        Bounds b = playercollider.bounds;
        float heightDiff = (normalHeight - slideHeight) * playercollider.transform.lossyScale.y;
        Vector2 boxCenter = new Vector2(b.center.x, b.max.y + heightDiff * 0.5f);
        Vector2 boxSize = new Vector2(b.size.x * 0.85f, heightDiff * 0.95f);
        return !Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundLayer);
    }

    private void HandleSlideTimers()
    {
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f) StopSlide();
        }
        else if (!canSlide)
        {
            slideCooldownTimer -= Time.deltaTime;
            if (slideCooldownTimer <= 0f) canSlide = true;
        }
    }

    private void UpdateAnimations()
    {
        if (anim == null || rb == null) return;

        bool isFalling = !isGrounded && rb.linearVelocity.y < -0.1f && !isSliding && !isDashing;

        anim.SetBool("isWalking", Mathf.Abs(moveInput.x) > 0.01f && isGrounded && !isSliding);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isSliding", isSliding);
        anim.SetBool("isDashing", isDashing);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        anim.SetBool("isFalling", isFalling);
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

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.GetComponent<MovingPlatform>() != null && col.contacts[0].normal.y > 0.5f)
            currentPlatform = col.gameObject.GetComponent<MovingPlatform>();

        CheckDashHit(col.collider);
    }

    private void CheckDashHit(Collider2D col)
    {
        if (isDashing && col.CompareTag("Enemy") && col != lastDashHitEnemy)
        {
            lastDashHitEnemy = col;
            EnemyHealth eHealth = col.GetComponent<EnemyHealth>();
            if (eHealth != null) eHealth.HandleDashHit();
        }
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.GetComponent<MovingPlatform>() != null)
            currentPlatform = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}