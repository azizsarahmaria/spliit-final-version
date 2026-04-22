using UnityEngine;
using UnityEngine.InputSystem;

public class player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    public CapsuleCollider2D playercollider;

    [Header("Jump")]
    public float jumpForce = 5f;
    private bool canJump = true;
    private bool isGrounded = true;

    [Header("Double Jump")]
    public bool enableDoubleJump = true;
    public float doubleJumpForce = 4.5f; // Slightly less than first jump
    private int jumpsRemaining = 1;
    private int maxJumps = 2; // 1 = single jump, 2 = double jump

    [Header("Movement Variables")]
    public float speed = 5f;
    public int facingDirection = 1;
    private Vector2 MoveInput;

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

    private bool justJumped = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();

        if (playercollider != null)
        {
            playercollider.size = new Vector2(playercollider.size.x, normalHeight);
            playercollider.offset = normalOffset;
        }

        maxJumps = enableDoubleJump ? 2 : 1;
        jumpsRemaining = maxJumps;
    }

    void Update()
    {
        HandleSlideTimers();
        UpdateAnimations();
        Flip();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (justJumped)
        {
            justJumped = false;
            rb.linearVelocity = new Vector2(MoveInput.x * speed, rb.linearVelocity.y);
            return;
        }

        if (isSliding)
            rb.linearVelocity = new Vector2(facingDirection * slideSpeed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(MoveInput.x * speed, rb.linearVelocity.y);
    }

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

    public void OnJump(InputValue value)
    {
        if (!value.isPressed || jumpsRemaining <= 0) return;

        // If sliding, end the slide cleanly first.
        if (isSliding)
            StopSlide(triggerCooldown: true);

        // Use appropriate jump force (first jump vs double jump)
        float currentJumpForce = (jumpsRemaining == maxJumps) ? jumpForce : doubleJumpForce;

        // Apply jump velocity
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJumpForce);

        jumpsRemaining--;
        isGrounded = false;
        justJumped = true;
    }

    public void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }

    private void UpdateAnimations()
    {
        anim.SetBool("isWalking", MoveInput.x != 0 && !isSliding && isGrounded);
        anim.SetBool("isJumping", !isGrounded);
    }

    private void Flip()
    {
        if (!isSliding)
        {
            if (MoveInput.x > 0.1f)
            {
                facingDirection = 1;
                this.transform.GetChild(0).transform.GetComponent<SpriteRenderer>().flipX = false;
            }
            else if (MoveInput.x < -0.1f)
            {
                this.transform.GetChild(0).transform.GetComponent<SpriteRenderer>().flipX = true;
                facingDirection = -1;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (rb.linearVelocity.y <= 0.01f)
            {
                canJump = true;
                isGrounded = true;
                jumpsRemaining = maxJumps; // Reset jumps when landing
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = false;
    }
}