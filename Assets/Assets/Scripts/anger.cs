using UnityEngine;
using UnityEngine.InputSystem;

public class anger : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody2D rb;
    private Animator anim;
    private CapsuleCollider2D col;

    [Header("Movement Variables")]
    public float speed = 5f;
    public int facingDirection = 1;
    private Vector2 MoveInput;

    [Header("Slide Variables")]
    public float slideSpeed = 8f;
    public float slideDuration = 0.4f;
    private bool isSliding = false;
    private float slideTimer = 0f;

    [Header("Collider Settings")]
    // --- Fill these in the Inspector after running once to capture defaults ---
    public Vector2 standingSize = new Vector2(0.5f, 1.8f);
    public Vector2 standingOffset = new Vector2(0f, 0f);
    public Vector2 slideSize = new Vector2(0.5f, 0.9f);   // Half the height
    public Vector2 slideOffset = new Vector2(0f, -0.45f); // Shift down so feet stay grounded

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        col = GetComponent<CapsuleCollider2D>();

        // Snapshot the standing collider values at runtime
        standingSize = col.size;
        standingOffset = col.offset;
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void Update()
    {
        HandleSlideTimer();
        Flip();
        UpdateAnimations();
    }

    private void HandleMovement()
    {
        if (isSliding)
            rb.linearVelocity = new Vector2(facingDirection * slideSpeed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(MoveInput.x * speed, rb.linearVelocity.y);
    }

    private void HandleSlideTimer()
    {
        if (!isSliding) return;

        slideTimer -= Time.deltaTime;
        if (slideTimer <= 0f)
            StopSlide();
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;

        // Shrink collider downward so the bottom stays on the ground
        col.size = slideSize;
        col.offset = slideOffset;
    }

    private void StopSlide()
    {
        isSliding = false;

        // Restore original collider
        col.size = standingSize;
        col.offset = standingOffset;
    }

    void Flip()
    {
        if (MoveInput.x > 0.1f)
            facingDirection = 1;
        else if (MoveInput.x < -0.1f)
            facingDirection = -1;
        transform.localScale = new Vector3(facingDirection, 1, 1);
    }

    void UpdateAnimations()
    {
        anim.SetBool("isWalking", MoveInput.x != 0 && !isSliding);
        anim.SetBool("isSliding", isSliding);
    }

    public void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }

    public void OnSlide(InputValue value) // Bind a key (e.g. Left Ctrl) in Input Actions
    {
        if (value.isPressed && !isSliding && MoveInput.x != 0)
            StartSlide();
    }
}