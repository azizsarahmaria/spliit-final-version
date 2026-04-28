using UnityEngine;

public class Spike : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float moveSpeed = 3f;
    public float patrolDistance = 4f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.5f;
    public float edgeCheckOffset = 0.4f;
    public LayerMask groundLayer;
    public LayerMask mushroomLayer;

    [Header("Wall Detection")]
    public float wallRayLength = 0.8f;      // ✅ controls yellow ray length
    public float wallRaySpread = 0.3f;      // ✅ controls gap between the 3 rays

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
        // Ground/edge check (downward)
        Vector2 edgeCheckPos = new Vector2(
            transform.position.x + (edgeCheckOffset * moveDirection),
            transform.position.y - 0.3f
        );
        bool groundAhead = Physics2D.Raycast(edgeCheckPos, Vector2.down, groundCheckDistance, groundLayer);

        // Wall check at three heights
        LayerMask wallMask = groundLayer | mushroomLayer;
        Vector2 dir = new Vector2(moveDirection, 0);

        bool wallLow = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y - wallRaySpread), dir, wallRayLength, wallMask);
        bool wallMid = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y), dir, wallRayLength, wallMask);
        bool wallHigh = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y + wallRaySpread), dir, wallRayLength, wallMask);

        bool wallAhead = wallLow || wallMid || wallHigh;

        bool reachedRight = transform.position.x >= startPos.x + patrolDistance;
        bool reachedLeft = transform.position.x <= startPos.x - patrolDistance;

        if (!groundAhead && moveDirection == 1) moveDirection = -1;
        else if (!groundAhead && moveDirection == -1) moveDirection = 1;

        if (wallAhead && moveDirection == 1) moveDirection = -1;
        else if (wallAhead && moveDirection == -1) moveDirection = 1;

        if (reachedRight) moveDirection = -1;
        else if (reachedLeft) moveDirection = 1;

        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);

        float scaleX = Mathf.Abs(originalScale.x) * moveDirection;
        transform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);
    }

    private void OnDrawGizmosSelected()
    {
        // Ground edge ray (red)
        Gizmos.color = Color.red;
        Vector2 edgeCheckPos = new Vector2(
            transform.position.x + (edgeCheckOffset * moveDirection),
            transform.position.y
        );
        Gizmos.DrawLine(edgeCheckPos, edgeCheckPos + Vector2.down * groundCheckDistance);

        // Three wall rays (yellow)
        Gizmos.color = Color.yellow;
        Vector3 dir = new Vector3(moveDirection * wallRayLength, 0);
        Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y - wallRaySpread), new Vector3(transform.position.x, transform.position.y - wallRaySpread) + dir);
        Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y), new Vector3(transform.position.x, transform.position.y) + dir);
        Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y + wallRaySpread), new Vector3(transform.position.x, transform.position.y + wallRaySpread) + dir);
    }
}