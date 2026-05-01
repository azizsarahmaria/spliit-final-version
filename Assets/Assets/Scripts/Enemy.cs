using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float patrolDistance = 5f;
    public float moveSpeed = 2f;
    private Vector3 startPosition;
    private int direction = 1;

    [Header("Detection Settings")]
    public LayerMask groundLayer;
    public float wallCheckDistance = 0.5f;
    public float groundCheckDistance = 1.0f;
    public Vector2 groundCheckOffset = new Vector2(0.5f, 0f);

    [Header("Combat Settings")]
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
    private float lastAttackTime;
    private bool isAttacking = false;

    [Header("Attack Timing")]
    [Tooltip("How long to wait after the attack animation starts before dealing damage")]
    public float hitDelay = 0.5f;
    public float attackHitboxRadius = 0.8f;

    [Header("References")]
    public LayerMask playerLayer;
    private Rigidbody2D rb;
    private Animator anim;
    private EnemyHealth healthScript;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        healthScript = GetComponent<EnemyHealth>();
        startPosition = transform.position;

        if (rb != null)
        {
            rb.mass = 100f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    void Update()
    {
        if (healthScript != null && healthScript.IsDead) return;

        CheckForPlayer();

        if (!isAttacking)
        {
            Patrol();
        }
    }

    private void Patrol()
    {
        // 1. Calculate ray origins
        Vector2 rayDir = direction == 1 ? Vector2.right : Vector2.left;
        Vector2 wallOrigin = (Vector2)transform.position;
        Vector2 groundOrigin = (Vector2)transform.position + new Vector2(groundCheckOffset.x * direction, groundCheckOffset.y);

        // 2. Check for Walls
        RaycastHit2D wallHit = Physics2D.Raycast(wallOrigin, rayDir, wallCheckDistance, groundLayer);

        // 3. Check for Ledges (Ground Check)
        RaycastHit2D groundHit = Physics2D.Raycast(groundOrigin, Vector2.down, groundCheckDistance, groundLayer);

        // Turn around if we hit a wall OR if there is no ground ahead
        float currentDist = transform.position.x - startPosition.x;
        bool outOfPatrolRange = (direction > 0 && currentDist >= patrolDistance) || (direction < 0 && currentDist <= -patrolDistance);

        if (wallHit.collider != null || groundHit.collider == null || outOfPatrolRange)
        {
            direction *= -1;
            Flip();
        }

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        anim.SetBool("isWalking", true);
    }

    private void CheckForPlayer()
    {
        if (isAttacking || Time.time < lastAttackTime + attackCooldown) return;

        Vector2 rayDirection = direction == 1 ? Vector2.right : Vector2.left;
        Vector2 rayOrigin = (Vector2)transform.position + (rayDirection * 0.5f);

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, attackRange, playerLayer);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            // Update direction to face player if they are behind us but within range
            float playerDir = hit.collider.transform.position.x - transform.position.x;
            int newDir = playerDir > 0 ? 1 : -1;

            if (newDir != direction)
            {
                direction = newDir;
                Flip();
            }

            StartCoroutine(AttackRoutine());
        }
    }

    private System.Collections.IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        rb.linearVelocity = Vector2.zero;
        anim.SetBool("isWalking", false);
        anim.SetTrigger("attack");

        yield return new WaitForSeconds(hitDelay);

        Vector2 hitZone = (Vector2)transform.position + new Vector2(direction * attackRange, 0);
        Collider2D playerHit = Physics2D.OverlapCircle(hitZone, attackHitboxRadius, playerLayer);

        if (playerHit != null && playerHit.CompareTag("Player"))
        {
            AngerHealth playerHealth = playerHit.GetComponent<AngerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1, transform.position);
            }
        }

        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
    }

    private void Flip()
    {
        Vector3 newScale = transform.localScale;
        newScale.x = Mathf.Abs(newScale.x) * direction;
        transform.localScale = newScale;
    }

    private void OnDrawGizmos()
    {
        // Patrol range
        Gizmos.color = Color.yellow;
        Vector3 center = Application.isPlaying ? startPosition : transform.position;
        Gizmos.DrawLine(center + Vector3.left * patrolDistance, center + Vector3.right * patrolDistance);

        // Wall check visualization
        Gizmos.color = Color.cyan;
        Vector2 rayDir = direction == 1 ? Vector2.right : Vector2.left;
        Gizmos.DrawRay(transform.position, rayDir * wallCheckDistance);

        // Ground check visualization
        Gizmos.color = Color.green;
        Vector2 groundOrigin = (Vector2)transform.position + new Vector2(groundCheckOffset.x * direction, groundCheckOffset.y);
        Gizmos.DrawRay(groundOrigin, Vector2.down * groundCheckDistance);

        // Attack hitbox
        Gizmos.color = new Color(1, 0, 0, 0.4f);
        Vector2 hitZone = (Vector2)transform.position + new Vector2(direction * attackRange, 0);
        Gizmos.DrawSphere(hitZone, attackHitboxRadius);
    }
}