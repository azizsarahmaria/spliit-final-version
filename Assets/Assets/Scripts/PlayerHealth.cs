using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private float flashSpeed = 0.08f;
    [SerializeField] private int flashCycles = 4;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 7f;
    [SerializeField] private float knockbackDuration = 0.2f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Color originalColor;
    private bool isInvulnerable = false;

    void Start()
    {
        // This will find SpriteRenderer even if it's on a child object
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // Debug check to see if components are missing
        if (spriteRenderer == null) Debug.LogError("Missing SpriteRenderer on Player!");
        if (rb == null) Debug.LogError("Missing Rigidbody2D on Player!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check for both Spike and Enemy tags
        if ((other.CompareTag("Spike") || other.CompareTag("Enemy")) && !isInvulnerable)
        {
            ApplyHit(other.transform.position);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check for both Spike and Enemy tags
        if ((collision.gameObject.CompareTag("Spike") || collision.gameObject.CompareTag("Enemy")) && !isInvulnerable)
        {
            ApplyHit(collision.transform.position);
        }
    }

    private void ApplyHit(Vector2 hazardPosition)
    {
        Debug.Log("Hit! Starting Flash and Knockback.");
        StartCoroutine(HitFlashRoutine());
        StartCoroutine(KnockbackRoutine(hazardPosition));
    }

    private IEnumerator KnockbackRoutine(Vector2 hazardPosition)
    {
        if (rb == null) yield break;

        Vector2 moveDirection = (transform.position - (Vector3)hazardPosition).normalized;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(moveDirection * knockbackForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null) yield break;

        isInvulnerable = true;

        for (int i = 0; i < flashCycles; i++)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(flashSpeed);

            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(flashSpeed);
        }

        spriteRenderer.color = originalColor;
        isInvulnerable = false;
    }
}