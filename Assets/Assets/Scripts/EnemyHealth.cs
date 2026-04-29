using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Settings")]
    public int maxDashHits = 2; // We want exactly 2 hits to kill
    private int currentDashHits = 0;
    private bool isDead = false;

    private Animator anim;
    private Rigidbody2D rb;
    private Enemy movementScript;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        movementScript = GetComponent<Enemy>();
    }

    public void HandleDashHit()
    {
        if (isDead) return;

        currentDashHits++;
        Debug.Log("Dash hits taken: " + currentDashHits);

        if (currentDashHits == 1)
        {
            // First Dash: Play Damage
            PlayHurt();
        }
        else if (currentDashHits >= maxDashHits)
        {
            // Second Dash: Play Death
            PlayDeath();
        }
    }

    void PlayHurt()
    {
        if (anim != null)
        {
            anim.SetTrigger("hurt"); // Matches your ANIM_HURT string
        }
    }

    void PlayDeath()
    {
        isDead = true;
        if (movementScript != null) movementScript.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (anim != null)
        {
            anim.SetBool("isWalking", false);
            anim.SetTrigger("death"); // Matches your ANIM_DEATH string
        }

        Destroy(gameObject, 1.5f);
    }
}