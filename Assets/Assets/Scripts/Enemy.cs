using UnityEngine;

public class Enemy : MonoBehaviour
{
    private const string PLAYER_TAG = "Player";

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
    public float attackRange = 1.5f;
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
    private int dashHitsTaken = 0;
    private bool isDead = false;
    private bool isHurt = false;
    private bool isAttacking = false;      // ← NEW: freeze patrol during attack

    private float attackTimer = 0f;
    private float flipCooldown = 0f;
    private const float FLIP_COOLDOWN_TIME = 0.4f;
    private Transform playerTarget;
    private Collider2D playerTargetCollider;

    private const string ANIM_WALK = "isWalking";
    private const string ANIM_ATTACK = "attack";
    private const string ANIM_HURT = "hurt";
    private const string ANIM_DEATH = "death";

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;
        currentHealth = maxHealth;
        CachePlayerTarget();

        // Set startPos immediately — no coroutine needed
        startPos = transform.position;

        if (anim != null)
            anim.SetBool(ANIM_WALK, true);
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

        Collider2D hit = FindPlayerInRange();

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

    Collider2D FindPlayerInRange()
    {
        CachePlayerTarget();
        if (playerTarget == null) return null;

        float distanceToPlayer = GetDistanceToPlayer();
        if (distanceToPlayer > attackRange) return null;

        return playerTargetCollider;
    }

    void CachePlayerTarget()
    {
        if (playerTarget != null && playerTargetCollider != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(PLAYER_TAG);
        if (playerObject == null) return;

        playerTarget = playerObject.transform;
        playerTargetCollider = playerObject.GetComponent<Collider2D>();

        if (playerTargetCollider == null)
            playerTargetCollider = playerObject.GetComponentInChildren<Collider2D>();
    }

    float GetDistanceToPlayer()
    {
        if (playerTarget == null) return float.MaxValue;

        if (playerTargetCollider != null)
        {
            Vector2 closestPoint = playerTargetCollider.ClosestPoint(transform.position);
            return Vector2.Distance(transform.position, closestPoint);
        }

        return Vector2.Distance(transform.position, playerTarget.position);
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
            if (player == null)
                player = hit.GetComponentInParent<PlayerHealth>();

            if (player != null)
                player.TakeDamage(attackDamage, transform.position);

            AngerHealth anger = hit.GetComponent<AngerHealth>();
            if (anger == null)
                anger = hit.GetComponentInParent<AngerHealth>();

            if (anger != null)
                anger.TakeDamage(attackDamage, transform.position);
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
        if (isDead) return;

        StopAllCoroutines();
        dashHitsTaken++;
        currentHealth = Mathf.Max(0, currentHealth - 1);

        if (dashHitsTaken >= 2 || currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(HurtRoutine());
        }
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
        StopAllCoroutines();
        isDead = true;
        isHurt = false;
        isAttacking = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (anim != null)
        {
            // Force the walking bool to false so it doesn't try to stay in patrol
            anim.SetBool(ANIM_WALK, false);

            // Clear these to make sure 'die' is the priority
            anim.ResetTrigger(ANIM_HURT);
            anim.ResetTrigger(ANIM_ATTACK);

            anim.SetTrigger(ANIM_DEATH);
        }

        Destroy(gameObject, 1.2f);
    }
    // ── Gizmos ────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // Use transform.position in editor, startPos at runtime
        Vector2 origin = Application.isPlaying ? startPos : (Vector2)transform.position;

        // Edge check ray
        Gizmos.color = Color.red;
        Vector2 edgeCheckPos = new Vector2(
            transform.position.x + (edgeCheckOffset * moveDirection),
            transform.position.y
        );
        Gizmos.DrawLine(edgeCheckPos, edgeCheckPos + Vector2.down * groundCheckDistance);

        // Attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Patrol bounds
        Gizmos.color = Color.green;
        Vector3 leftBound = new Vector3(origin.x - patrolDistance, transform.position.y, 0);
        Vector3 rightBound = new Vector3(origin.x + patrolDistance, transform.position.y, 0);
        Gizmos.DrawLine(leftBound, rightBound);
        Gizmos.DrawWireSphere(leftBound, 0.2f);
        Gizmos.DrawWireSphere(rightBound, 0.2f);
    }
}
