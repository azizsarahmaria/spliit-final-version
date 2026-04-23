using UnityEngine;
using UnityEngine.InputSystem;

public class angerscript : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    public CapsuleCollider2D playercollider;

    [Header("Jump")]
    public float jumpForce = 5f;
    private bool canJump = true;
    private bool isGrounded = true;

    [Header("Movement Variables")]
    public float speed = 5f;
    public int facingDirection = 1;
    private Vector2 MoveInput;

    // Tracks when a jump was just initiated, so HandleMovement doesn't
    // overwrite the jump velocity with walk velocity.
    private bool justJumped = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        UpdateAnimations();
        Flip();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        // If we just jumped, don't override vertical velocity this step.
        // Preserve horizontal movement so the player can still steer mid-air.
        if (justJumped)
        {
            justJumped = false;
            rb.linearVelocity = new Vector2(MoveInput.x * speed, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(MoveInput.x * speed, rb.linearVelocity.y);
    }

    public void OnJump(InputValue value)
    {
        if (!value.isPressed || !canJump) return;

        // Apply jump velocity IMMEDIATELY on input frame.
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        canJump = false;
        isGrounded = false;
        justJumped = true;
    }

    public void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }

    private void UpdateAnimations()
    {
        // Only need isJumping and isFalling for your 3 animations
        anim.SetBool("isJumping", rb.linearVelocity.y > 0.1f);
        anim.SetBool("isFalling", rb.linearVelocity.y < -0.1f);
    }

    private void Flip()
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            // Only mark grounded if we're actually falling/landing
            if (rb.linearVelocity.y <= 0.01f)
            {
                canJump = true;
                isGrounded = true;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = false;
    }
}