using System.Collections;
using UnityEngine;

/// <summary>
/// Flying dive-attack enemy.
/// Animations required (exact names in Animator):
///   idle | Fly | AttackStart | AttackLoop | AttackEnd | Hit | Die
///
/// Setup checklist:
///   1. Add this script + Rigidbody2D + Collider2D to the enemy GameObject.
///   2. Set Rigidbody2D → Gravity Scale = 0, Freeze Rotation Z = true.
///   3. Tag the enemy "Enemy".
///   4. Create two empty GameObjects in the scene as patrol points and assign them.
///   5. In the Animator, make sure every clip name matches exactly (case-sensitive).
///   6. To deal damage TO this enemy, call enemy.TakeDamage(amount) from your
///      projectile / sword hit script.
/// </summary>
public class DiveEnemy : MonoBehaviour
{
    // ──────────────────────────────────────────── REFERENCES
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private Transform player;
    private PlayerHealth playerHealth;

    // ──────────────────────────────────────────── PATROL
    [Header("Patrol")]
    public Transform patrolPointA;
    public Transform patrolPointB;
    public float patrolSpeed = 3f;
    private Transform patrolTarget;

    // ──────────────────────────────────────────── DETECTION
    [Header("Detection — edit in Inspector")]
    [Tooltip("How far left/right the enemy can see the player.")]
    public float detectionRangeX = 6f;
    [Tooltip("How far BELOW the enemy to detect the player. Enemy won't attack if player is above.")]
    public float detectionRangeY = 8f;
    [Tooltip("Horizontal tolerance before the dive starts — lower = must be more directly below.")]
    public float attackAlignX = 1.2f;

    // ──────────────────────────────────────────── ATTACK
    [Header("Attack")]
    public float diveSpeed = 14f;
    public int attackDamage = 1;
    public float attackCooldown = 2f;
    [Tooltip("Speed at which the enemy floats back up after attacking.")]
    public float returnSpeed = 5f;
    [Tooltip("Radius around enemy centre that counts as a hit on the player.")]
    public float hitRadius = 0.9f;

    // ──────────────────────────────────────────── HEALTH
    [Header("Health")]
    public int maxHealth = 3;
    private int currentHealth;

    // ──────────────────────────────────────────── INTERNAL STATE
    private bool isDead;
    private bool isHit;
    private bool canAttack = true;
    private Vector3 restPosition;   // Y-height the enemy returns to after diving

    private enum State { Patrol, Chase, AttackStart, AttackLoop, AttackEnd, Hit, Dead }
    private State state;

    // ═══════════════════════════════════════════════════════════ UNITY CALLBACKS

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // Flying enemy — no gravity, no rotation
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        currentHealth = maxHealth;
        restPosition = transform.position;
        patrolTarget = patrolPointA;

        // Find player by tag
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }

        EnterState(State.Patrol);
    }

    void Update()
    {
        if (isDead || isHit) return;

        switch (state)
        {
            case State.Patrol: TickPatrol(); break;
            case State.Chase: TickChase(); break;
                // All other states are driven by coroutines — nothing extra needed here.
        }
    }

    // ═══════════════════════════════════════════════════════════ STATE TICKS

    // ── PATROL ──────────────────────────────────────────────────
    void TickPatrol()
    {
        if (PlayerIsDetected()) { EnterState(State.Chase); return; }

        FlyToward(patrolTarget.position, patrolSpeed);

        if (Vector2.Distance(transform.position, patrolTarget.position) < 0.2f)
            patrolTarget = patrolTarget == patrolPointA ? patrolPointB : patrolPointA;
    }

    // ── CHASE ───────────────────────────────────────────────────
    void TickChase()
    {
        if (!PlayerIsDetected()) { EnterState(State.Patrol); return; }

        // Drift horizontally above the player, keep current Y
        Vector3 abovePlayer = new Vector3(player.position.x, transform.position.y, 0f);
        FlyToward(abovePlayer, patrolSpeed);
        FlipToward(player.position.x);

        bool alignedHorizontally = Mathf.Abs(transform.position.x - player.position.x) <= attackAlignX;
        bool playerIsBelow = player.position.y < transform.position.y;

        if (alignedHorizontally && playerIsBelow && canAttack)
            StartCoroutine(DiveAttackRoutine());
    }

    // ═══════════════════════════════════════════════════════════ ATTACK COROUTINE

    IEnumerator DiveAttackRoutine()
    {
        canAttack = false;
        restPosition = transform.position; // remember height to return to

        // ── 1. ATTACK START (wind-up) ──
        EnterState(State.AttackStart);
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(ClipLength("AttackStart"));

        // ── 2. ATTACK LOOP (dive straight down) ──
        EnterState(State.AttackLoop);
        bool hitPlayer = false;
        float timer = 0f;
        float maxDive = 3f; // safety cap so the enemy doesn't fall forever

        while (timer < maxDive)
        {
            timer += Time.deltaTime;
            rb.linearVelocity = new Vector2(0f, -diveSpeed);

            if (player != null && Vector2.Distance(transform.position, player.position) < hitRadius)
            {
                hitPlayer = true;
                break;
            }

            yield return null;
        }

        // ── 3. ATTACK END (impact / landing) ──
        rb.linearVelocity = Vector2.zero;
        EnterState(State.AttackEnd);

        if (hitPlayer && playerHealth != null)
            playerHealth.TakeDamage(attackDamage, transform.position);

        yield return new WaitForSeconds(ClipLength("AttackEnd"));

        // ── 4. FLOAT BACK UP to original height ──
        PlayAnim("Fly");
        while (Mathf.Abs(transform.position.y - restPosition.y) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, restPosition, returnSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = new Vector3(transform.position.x, restPosition.y, 0f);

        // ── 5. COOLDOWN then resume AI ──
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
        EnterState(PlayerIsDetected() ? State.Chase : State.Patrol);
    }

    // ═══════════════════════════════════════════════════════════ DAMAGE / DEATH

    /// <summary>
    /// Call this from your projectile or melee hit script to damage this enemy.
    /// e.g.  enemy.GetComponent<DiveEnemy>().TakeDamage(1);
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead || isHit) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            // Mark dead first so nothing can interrupt
            isDead = true;
            StopAllCoroutines();
            StartCoroutine(DieSequence());
        }
        else
        {
            StartCoroutine(HitSequence());
        }
    }

    IEnumerator HitSequence()
    {
        isHit = true;
        rb.linearVelocity = Vector2.zero;
        EnterState(State.Hit);
        yield return new WaitForSeconds(ClipLength("Hit"));
        isHit = false;
        EnterState(PlayerIsDetected() ? State.Chase : State.Patrol);
    }

    IEnumerator DieSequence()
    {
        rb.linearVelocity = Vector2.zero;
        EnterState(State.Dead);
        yield return new WaitForSeconds(ClipLength("Die"));
        Destroy(gameObject);
    }

    // ═══════════════════════════════════════════════════════════ HELPERS

    /// <summary>
    /// Returns true when the player is within the detection box AND below the enemy.
    /// </summary>
    bool PlayerIsDetected()
    {
        if (player == null) return false;
        float dx = Mathf.Abs(transform.position.x - player.position.x);
        float dy = transform.position.y - player.position.y; // positive = enemy is above player
        return dx <= detectionRangeX && dy >= 0f && dy <= detectionRangeY;
    }

    void FlyToward(Vector3 target, float speed)
    {
        Vector3 dir = (target - transform.position).normalized;
        rb.linearVelocity = new Vector2(dir.x * speed, dir.y * speed);
        FlipToward(target.x);
    }

    void FlipToward(float targetX) => sr.flipX = targetX < transform.position.x;

    // ── State → Animation mapping ────────────────────────────────
    void EnterState(State newState)
    {
        state = newState;
        switch (newState)
        {
            case State.Patrol:
            case State.Chase: PlayAnim("Fly"); break;
            case State.AttackStart: PlayAnim("AttackStart"); break;
            case State.AttackLoop: PlayAnim("AttackLoop"); break;
            case State.AttackEnd: PlayAnim("AttackEnd"); break;
            case State.Hit: PlayAnim("Hit"); break;
            case State.Dead: PlayAnim("Die"); break;
        }
    }

    /// <summary>CrossFade by clip/state name — works regardless of Animator parameter setup.</summary>
    void PlayAnim(string clipName) => anim.CrossFade(clipName, 0.05f, 0);

    /// <summary>Reads the actual clip length so coroutines wait the right amount of time.</summary>
    float ClipLength(string clipName)
    {
        foreach (var clip in anim.runtimeAnimatorController.animationClips)
            if (clip.name == clipName) return clip.length;
        return 0.5f; // fallback if clip name doesn't match
    }

    // ═══════════════════════════════════════════════════════════ GIZMOS (Scene view)

    void OnDrawGizmosSelected()
    {
        // Yellow = detection zone (box below enemy)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            transform.position - new Vector3(0f, detectionRangeY * 0.5f, 0f),
            new Vector3(detectionRangeX * 2f, detectionRangeY, 0f));

        // Red = horizontal alignment needed to trigger dive
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(attackAlignX * 2f, 0.4f, 0f));

        // Cyan = hit radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.9f);
    }
}