using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float patrolDistance = 5f;
    public float moveSpeed = 2f;
    private Vector3 startPosition;
    private int direction = 1;

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
    private EnemyHealth healthScript; // Link to your health script

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
        // If the health script says we are dead, stop moving entirely.
        if (healthScript != null && healthScript.IsDead) return;

        CheckForPlayer();

        if (!isAttacking)
        {
            Patrol();
        }
    }

    private void Patrol()
    {
        float currentDist = transform.position.x - startPosition.x;

        if (direction > 0 && currentDist >= patrolDistance)
        {
            direction = -1;
            Flip();
        }
        else if (direction < 0 && currentDist <= -patrolDistance)
        {
            direction = 1;
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

        // ⭐ FIX: Offset the hitZone based on the direction the enemy is facing
        Vector2 hitZone = (Vector2)transform.position + new Vector2(direction * attackRange, 0);

        // Check for the player in that forward-facing circle
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
        Gizmos.color = Color.yellow;
        Vector3 center = Application.isPlaying ? startPosition : transform.position;
        Gizmos.DrawLine(center + Vector3.left * patrolDistance, center + Vector3.right * patrolDistance);

        Gizmos.color = Color.red;
        Vector2 rayDir = direction == 1 ? Vector2.right : Vector2.left;
        Vector2 rayOrigin = (Vector2)transform.position + (rayDir * 0.5f);
        Gizmos.DrawRay(rayOrigin, rayDir * attackRange);

        // ⭐ FIX: Update Gizmo to match the new hitZone logic
        Gizmos.color = new Color(1, 0, 0, 0.4f);
        Vector2 hitZone = (Vector2)transform.position + new Vector2(direction * attackRange, 0);
        Gizmos.DrawSphere(hitZone, attackHitboxRadius);
    }
}