using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Settings")]
    public int maxDashHits = 1;
    private int currentDashHits = 0;
    private bool isDead = false;

    public bool IsDead => isDead;

    private Animator anim;
    private Rigidbody2D rb;
    private Enemy movementScript;
    private Collider2D enemyCollider;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        movementScript = GetComponent<Enemy>();
        enemyCollider = GetComponent<Collider2D>();
    }

    public void HandleDashHit()
    {
        if (isDead) return;

        currentDashHits++;
        Debug.Log("Dash hits taken: " + currentDashHits);

        if (currentDashHits >= maxDashHits)
        {
            PlayDeath();
        }
        else
        {
            PlayHurt();
        }
    }

    void PlayHurt()
    {
        if (anim != null)
            anim.SetTrigger("hurt");
    }

    void PlayDeath()
    {
        isDead = true;

        if (movementScript != null) movementScript.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        if (enemyCollider != null)
            enemyCollider.enabled = false;

        if (anim != null)
        {
            anim.SetBool("isWalking", false);
            anim.SetTrigger("death");
        }

        Destroy(gameObject, 1.5f);
    }
}