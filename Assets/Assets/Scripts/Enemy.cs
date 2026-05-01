using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float patrolDistance = 5f;
    public float moveSpeed = 2f;
    [Tooltip("Seconds before the enemy is allowed to flip direction again")]
    public float flipCooldown = 0.25f;

    private Vector3 startPosition;
    private int direction = 1;
    private float lastFlipTime = -999f;

    [Header("Detection Settings")]
    public LayerMask groundLayer;
    public float wallCheckDistance = 0.6f; // Slightly increased for reliability
    public float groundCheckDistance = 1.0f;
    public Vector2 groundCheckOffset = new Vector2(0.5f, 0f);

    [Tooltip("How many horizontal rays to cast across the enemy height")]
    public int wallRayCount = 4;

    [Header("Combat Settings")]
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
    public float hitDelay = 0.5f;
    public float attackHitboxRadius = 0.8f;
    public LayerMask playerLayer;

    private float lastAttackTime;
    private bool isAttacking = false;

    [Header("References")]
    private Rigidbody2D rb;
    private Animator anim;
    private EnemyHealth healthScript;
    private Collider2D col;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        healthScript = GetComponent<EnemyHealth>();
        col = GetComponent<Collider2D>();
        startPosition = transform.position;

        // Ensure Rigidbody is set up for top-down or platformer 2D
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Helps with sticking
    }

    void Update()
    {
        if (healthScript != null && healthScript.IsDead)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        CheckForPlayer();
    }

    void FixedUpdate()
    {
        if (healthScript != null && healthScript.IsDead) return;

        if (!isAttacking)
        {
            HandlePatrol();
        }
    }

    private void HandlePatrol()
    {
        Vector2 rayDir = direction == 1 ? Vector2.right : Vector2.left;

        // 1. Check for Walls/Ground
        bool wallDetected = WallDetected(rayDir);

        Vector2 groundOrigin = (Vector2)transform.position + new Vector2(groundCheckOffset.x * direction, groundCheckOffset.y);
        RaycastHit2D groundHit = Physics2D.Raycast(groundOrigin, Vector2.down, groundCheckDistance, groundLayer);

        // 2. Check Patrol Range
        float currentDist = transform.position.x - startPosition.x;
        bool outOfRange = (direction > 0 && currentDist >= patrolDistance) || (direction < 0 && currentDist <= -patrolDistance);

        // 3. Flip Logic
        bool cooldownReady = Time.time >= lastFlipTime + flipCooldown;

        if (cooldownReady && (wallDetected || groundHit.collider == null || outOfRange))
        {
            FlipDirection();
        }
        else
        {
            // Apply Movement
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
            anim.SetBool("isWalking", true);
        }
    }

    private bool WallDetected(Vector2 rayDir)
    {
        Bounds bounds = col.bounds;
        float bottom = bounds.min.y + 0.1f; // Higher offset to avoid floor
        float top = bounds.max.y - 0.1f;    // Lower offset to avoid ceiling
        float height = top - bottom;

        for (int i = 0; i < wallRayCount; i++)
        {
            float t = (float)i / (wallRayCount - 1);
            float y = bottom + t * height;

            // Start the ray slightly BEHIND the center so it doesn't start inside the wall
            Vector2 origin = new Vector2(bounds.center.x - (rayDir.x * 0.1f), y);

            if (Physics2D.Raycast(origin, rayDir, wallCheckDistance, groundLayer))
            {
                return true;
            }
        }
        return false;
    }

    private void FlipDirection()
    {
        direction *= -1;
        lastFlipTime = Time.time;

        // Zero out velocity to prevent "sliding" into the wall while turning
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        Vector3 newScale = transform.localScale;
        newScale.x = Mathf.Abs(newScale.x) * direction;
        transform.localScale = newScale;
    }

    private void CheckForPlayer()
    {
        // Don't check if already attacking or on cooldown
        if (isAttacking || Time.time < lastAttackTime + attackCooldown) return;

        Vector2 rayDirection = direction == 1 ? Vector2.right : Vector2.left;

        // Start the ray slightly in front of the enemy's center
        Vector2 rayOrigin = (Vector2)transform.position + (rayDirection * 0.2f);

        // Using BoxCast instead of Raycast makes detection much more reliable
        // It creates a "rectangle" of detection rather than a pixel-perfect line
        RaycastHit2D hit = Physics2D.BoxCast(rayOrigin, new Vector2(0.5f, 1f), 0f, rayDirection, attackRange, playerLayer);

        if (hit.collider != null)
        {
            // Debug line to see the detection in the scene view
            Debug.DrawRay(rayOrigin, rayDirection * attackRange, Color.red);

            if (hit.collider.CompareTag("Player"))
            {
                StartCoroutine(AttackRoutine());
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        anim.SetBool("isWalking", false);
        anim.SetTrigger("attack");

        yield return new WaitForSeconds(hitDelay);

        // Damage Logic
        Vector2 hitZone = (Vector2)transform.position + new Vector2(direction * attackRange, 0);
        Collider2D playerHit = Physics2D.OverlapCircle(hitZone, attackHitboxRadius, playerLayer);

        if (playerHit != null)
        {
            AngerHealth playerHealth = playerHit.GetComponent<AngerHealth>();
            if (playerHealth != null) playerHealth.TakeDamage(1, transform.position);
        }

        yield return new WaitForSeconds(0.5f); // Recovery time
        lastAttackTime = Time.time;
        isAttacking = false;
    }

    private void OnDrawGizmos()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (col == null) return;

        // Patrol range
        Gizmos.color = Color.yellow;
        Vector3 center = Application.isPlaying ? startPosition : transform.position;
        Gizmos.DrawLine(center + Vector3.left * patrolDistance, center + Vector3.right * patrolDistance);

        // Wall detection rays
        Gizmos.color = Color.cyan;
        Vector2 rayDir = direction == 1 ? Vector2.right : Vector2.left;
        Bounds b = col.bounds;
        for (int i = 0; i < wallRayCount; i++)
        {
            float t = (float)i / (wallRayCount - 1);
            float y = (b.min.y + 0.1f) + t * (b.size.y - 0.2f);
            Vector2 origin = new Vector2(b.center.x - (rayDir.x * 0.1f), y);
            Gizmos.DrawRay(origin, rayDir * wallCheckDistance);
        }

        // Ground check
        Gizmos.color = Color.green;
        Vector2 groundOrigin = (Vector2)transform.position + new Vector2(groundCheckOffset.x * direction, groundCheckOffset.y);
        Gizmos.DrawRay(groundOrigin, Vector2.down * groundCheckDistance);

        // Attack hitbox
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Vector2 hitPos = (Vector2)transform.position + new Vector2(direction * attackRange, 0);
        Gizmos.DrawSphere(hitPos, attackHitboxRadius);
    }
}