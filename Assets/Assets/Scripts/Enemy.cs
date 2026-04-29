using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Health Logic")]
    public int hitsTaken = 0;
    private bool isDead = false;

    [Header("Components")]
    public Animator anim;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // If anim is null, try to find it in children
        if (anim == null) anim = GetComponentInChildren<Animator>();
    }

    public void TakeDashDamage()
    {
        if (isDead) return;

        hitsTaken++;
        Debug.Log("Enemy hit! Total hits: " + hitsTaken);

        if (hitsTaken == 1)
        {
            // First hit: Force the hurt animation
            anim.SetTrigger("hurt");
        }
        else if (hitsTaken >= 2)
        {
            // Second hit: Kill it
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Enemy is Dying now!");

        // Stop all movement immediately
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Trigger the animation
        anim.SetTrigger("die");

        // Disable collision so the player doesn't keep hitting it
        GetComponent<Collider2D>().enabled = false;

        Destroy(gameObject, 1.5f);
    }
}