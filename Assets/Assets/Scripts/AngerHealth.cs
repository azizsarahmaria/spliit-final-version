using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AngerHealth : MonoBehaviour
{
    private const string IsDeadParameter = "isDead";

    [Header("Health Settings")]
    public int maxHealth = 3;
    public int currentHealth;

    [Header("Death Settings")]
    [SerializeField] private float deathAnimationDuration = 1f;

    [Header("Flash Settings")]
    [SerializeField] private float flashSpeed = 0.08f;
    [SerializeField] private int flashCycles = 4;
    [SerializeField] private Material flashMaterial;
    private Material normalMaterial;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 7f;
    [SerializeField] private float knockbackDuration = 0.2f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D[] colliders;
    private Color originalColor;
    private bool isInvulnerable = false;
    private bool isDead = false;
    private player playerController;

    void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        colliders = GetComponents<Collider2D>();
        playerController = GetComponent<player>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            normalMaterial = spriteRenderer.material;
        }

        currentHealth = maxHealth;
        HealthUI.instance.UpdateHearts(currentHealth, maxHealth);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((other.CompareTag("Spike") || other.CompareTag("Enemy")) && !isInvulnerable && !ShouldIgnoreHit(other))
            ApplyHit(other.transform.position);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if ((collision.gameObject.CompareTag("Spike") || collision.gameObject.CompareTag("Enemy")) && !isInvulnerable && !ShouldIgnoreHit(collision.collider))
            ApplyHit(collision.transform.position);
    }

    private void ApplyHit(Vector2 hazardPosition)
    {
        if (isDead) return;

        currentHealth--;
        HealthUI.instance.UpdateHearts(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            StartCoroutine(DeathRoutine());
            return;
        }

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

    public void TakeDamage(int damage, Vector2 sourcePosition)
    {
        if (isInvulnerable || isDead) return;
        if (playerController != null && playerController.IsDashing) return;
        ApplyHit(sourcePosition);
    }

    private bool ShouldIgnoreHit(Collider2D other)
    {
        if (other == null) return false;
        if (!other.CompareTag("Enemy") && !other.CompareTag("Spike")) return false;

        return playerController != null && playerController.IsDashing;
    }
    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null) yield break;
        isInvulnerable = true;

        for (int i = 0; i < flashCycles; i++)
        {
            spriteRenderer.material = flashMaterial;
            yield return new WaitForSeconds(flashSpeed);
            spriteRenderer.material = normalMaterial;
            yield return new WaitForSeconds(flashSpeed);
        }

        spriteRenderer.material = normalMaterial;
        isInvulnerable = false;
   
    }

    private IEnumerator DeathRoutine()
    {
        isDead = true;
        isInvulnerable = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (colliders != null)
        {
            foreach (Collider2D hitbox in colliders)
            {
                if (hitbox != null)
                    hitbox.enabled = false;
            }
        }

        if (animator != null)
            animator.SetBool(IsDeadParameter, true);

        yield return new WaitForSeconds(deathAnimationDuration);
        SceneManager.LoadScene(1);
    }
}
