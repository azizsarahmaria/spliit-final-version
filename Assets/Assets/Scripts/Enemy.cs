using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float moveSpeed = 2f;
    public float patrolDistance = 4f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.5f;
    public float edgeCheckOffset = 0.4f;
    public LayerMask groundLayer;

    [Header("Health Settings")]
    public int maxHealth = 2;

    [Header("Attack Settings")]
    public float attackRange = 1.5f;       // ← increased, checks full radius around enemy
    public int attackDamage = 1;
    public float attackCooldown = 1.2f;
    public LayerMask playerLayer;

    [Header("Components")]
    public Animator anim;

    private Rigidbody2D rb;
    private Vector2 startPos;
    private int moveDirection = 1;
    private Vector3 originalScale;

    private int currentHealth;
    private bool isDead = false;
    private bool isHurt = false;
    private bool isAttacking = false;      // ← NEW: freeze patrol during attack

    private float attackTimer = 0f;
    private float flipCooldown = 0f;
    private const float FLIP_COOLDOWN_TIME = 0.4f;

    private const string ANIM_WALK = "isWalking";
    private const string ANIM_ATTACK = "attack";
    private const string ANIM_HURT = "hurt";
    private const string ANIM_DEATH = "death";

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;
        currentHealth = maxHealth;

        // ← NEW: delay startPos assignment by one frame so physics
        //   settles before we lock in the patrol origin
        StartCoroutine(InitStartPos());
    }

    System.Collections.IEnumerator InitStartPos()
    {
        yield return new WaitForFixedUpdate();
        startPos = transform.position;

        if (anim != null)
            anim.SetBool(ANIM_WALK, true);
    }

    void Update()
    {
        if (isDead || isHurt || isAttacking) return;

        attackTimer -= Time.deltaTime;

        CheckAttack();

        // Only patrol if player is not in attack range
        if (!isAttacking)
            Patrol();
    }

    // ── Patrol ────────────────────────────────────────────────
    void Patrol()
    {
        if (flipCooldown > 0f)
            flipCooldown -= Time.deltaTime;

        Vector2 edgeCheckPos = new Vector2(
            transform.position.x + (edgeCheckOffset * moveDirection),
            transform.position.y - 0.3f
        );

        bool groundAhead = Physics2D.Raycast(edgeCheckPos, Vector2.down, groundCheckDistance, groundLayer);
        bool reachedRight = transform.position.x >= startPos.x + patrolDistance;
        bool reachedLeft = transform.position.x <= startPos.x - patrolDistance;

        if (flipCooldown <= 0f)
        {
            bool shouldFlip = false;

            if (!groundAhead) shouldFlip = true;
            if (reachedRight && moveDirection == 1) shouldFlip = true;
            if (reachedLeft && moveDirection == -1) shouldFlip = true;

            if (shouldFlip)
            {
                moveDirection *= -1;
                flipCooldown = FLIP_COOLDOWN_TIME;
            }
        }

        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);

        float scaleX = Mathf.Abs(originalScale.x) * moveDirection;
        transform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);
    }

    // ── Attack ────────────────────────────────────────────────
    void CheckAttack()
    {
        if (attackTimer > 0f) return;

        // ← NEW: check full circle around enemy, not just in front
        Collider2D hit = Physics2D.OverlapCircle(
            transform.position,
            attackRange,
            playerLayer
        );

        if (hit != null)
        {
            attackTimer = attackCooldown;

            // ← NEW: face the player before attacking
            float dirToPlayer = hit.transform.position.x - transform.position.x;
            if (dirToPlayer != 0)
            {
                moveDirection = dirToPlayer > 0 ? 1 : -1;
                float scaleX = Mathf.Abs(originalScale.x) * moveDirection;
                transform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);
            }

            StartCoroutine(AttackRoutine(hit));
        }
    }

    // ← NEW: coroutine so patrol freezes for the duration of the attack
    System.Collections.IEnumerator AttackRoutine(Collider2D hit)
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;  // stop moving while attacking

        if (anim != null)
        {
            anim.SetBool(ANIM_WALK, false);
            anim.SetTrigger(ANIM_ATTACK);
        }

        // Wait for attack animation windup before applying damage
        yield return new WaitForSeconds(0.3f);

        // Apply damage (player may have moved away, so re-check)
        if (hit != null)
        {
            PlayerHealth player = hit.GetComponent<PlayerHealth>();
            if (player != null)
                player.TakeDamage(attackDamage, transform.position);
        }

        // Wait for rest of attack animation to finish
        yield return new WaitForSeconds(0.4f);

        isAttacking = false;

        if (anim != null)
            anim.SetBool(ANIM_WALK, true);
    }

    // ── Dash Damage ───────────────────────────────────────────
    public void TakeDashDamage()
    {
        if (isDead || isHurt) return;

        currentHealth--;

        if (currentHealth <= 0)
            Die();
        else
            StartCoroutine(HurtRoutine());
    }

    System.Collections.IEnumerator HurtRoutine()
    {
        isHurt = true;
        isAttacking = false;   // ← cancel any ongoing attack
        rb.linearVelocity = Vector2.zero;

        if (anim != null)
        {
            anim.SetBool(ANIM_WALK, false);
            anim.SetTrigger(ANIM_HURT);
        }

        yield return new WaitForSeconds(0.5f);

        isHurt = false;

        if (anim != null)
            anim.SetBool(ANIM_WALK, true);
    }

    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (anim != null)
        {
            anim.SetBool(ANIM_WALK, false);
            anim.SetTrigger(ANIM_DEATH);
        }

        Destroy(gameObject, 1.2f);
    }

    // ── Gizmos ────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // Edge check ray
        Gizmos.color = Color.red;
        Vector2 edgeCheckPos = new Vector2(
            transform.position.x + (edgeCheckOffset * moveDirection),
            transform.position.y
        );
        Gizmos.DrawLine(edgeCheckPos, edgeCheckPos + Vector2.down * groundCheckDistance);

        // Attack range — now a full circle
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}