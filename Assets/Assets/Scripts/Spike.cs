using UnityEngine;

public class Spike : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float moveSpeed = 3f;
    public float patrolDistance = 4f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.5f;
    public float edgeCheckOffset = 0.4f; // how far ahead to check for edge
    public LayerMask groundLayer;

    [Header("Components")]
    public Animator anim;

    private Rigidbody2D rb;
    private Vector2 startPos;
    private int moveDirection = 1;
    private Vector3 originalScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
        originalScale = transform.localScale;

        if (anim != null)
            anim.SetBool("isWalking", true);
    }

    void Update()
    {
        Patrol();
    }

    void Patrol()
    {
        // ✅ Start the raycast from near the feet, not the center
        Vector2 edgeCheckPos = new Vector2(
            transform.position.x + (edgeCheckOffset * moveDirection),
            transform.position.y - 0.3f  // offset down toward feet
        );

        bool groundAhead = Physics2D.Raycast(edgeCheckPos, Vector2.down, groundCheckDistance, groundLayer);

        bool reachedRight = transform.position.x >= startPos.x + patrolDistance;
        bool reachedLeft = transform.position.x <= startPos.x - patrolDistance;

        // ✅ Only flip when SURE — separate conditions
        if (!groundAhead && moveDirection == 1) moveDirection = -1;
        else if (!groundAhead && moveDirection == -1) moveDirection = 1;

        if (reachedRight) moveDirection = -1;
        else if (reachedLeft) moveDirection = 1;

        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);

        float scaleX = Mathf.Abs(originalScale.x) * moveDirection;
        transform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);
    }

    // Draw gizmos to visualize edge detection in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 edgeCheckPos = new Vector2(
            transform.position.x + (edgeCheckOffset * moveDirection),
            transform.position.y
        );
        Gizmos.DrawLine(edgeCheckPos, edgeCheckPos + Vector2.down * groundCheckDistance);
    }
}