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
    private Material[] normalMaterials;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 7f;
    [SerializeField] private float knockbackDuration = 0.2f;

    private SpriteRenderer[] spriteRenderers;
    private SpriteRenderer[] flashOverlayRenderers;
    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D[] colliders;
    private bool isInvulnerable = false;
    private bool isDead = false;
    private Anger angerController;

    void Start()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        colliders = GetComponents<Collider2D>();
        angerController = GetComponent<Anger>();

        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            normalMaterials = new Material[spriteRenderers.Length];
            flashOverlayRenderers = new SpriteRenderer[spriteRenderers.Length];

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] == null)
                    continue;

                normalMaterials[i] = spriteRenderers[i].material;
                flashOverlayRenderers[i] = CreateFlashOverlay(spriteRenderers[i], i);
            }
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
        if (angerController != null && angerController.IsDashing) return;
        ApplyHit(sourcePosition);
    }

    private bool ShouldIgnoreHit(Collider2D other)
    {
        if (other == null) return false;
        if (!other.CompareTag("Enemy") && !other.CompareTag("Spike")) return false;

        return angerController != null && angerController.IsDashing;
    }
    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) yield break;
        isInvulnerable = true;

        for (int i = 0; i < flashCycles; i++)
        {
            ShowFlashOverlays();
            yield return new WaitForSeconds(flashSpeed);
            HideFlashOverlays();
            yield return new WaitForSeconds(flashSpeed);
        }

        HideFlashOverlays();
        isInvulnerable = false;
   
    }

    private SpriteRenderer CreateFlashOverlay(SpriteRenderer sourceRenderer, int index)
    {
        GameObject overlayObject = new GameObject(sourceRenderer.gameObject.name + "_FlashOverlay");
        overlayObject.hideFlags = HideFlags.HideAndDontSave;
        overlayObject.transform.SetParent(sourceRenderer.transform, false);
        overlayObject.transform.localPosition = Vector3.zero;
        overlayObject.transform.localRotation = Quaternion.identity;
        overlayObject.transform.localScale = Vector3.one;

        SpriteRenderer overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
        overlayRenderer.enabled = false;
        overlayRenderer.material = flashMaterial != null ? flashMaterial : sourceRenderer.material;
        overlayRenderer.color = Color.white;
        overlayRenderer.maskInteraction = sourceRenderer.maskInteraction;
        overlayRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        overlayRenderer.sortingOrder = sourceRenderer.sortingOrder + 1 + index;

        return overlayRenderer;
    }

    private void ShowFlashOverlays()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sourceRenderer = spriteRenderers[i];
            SpriteRenderer overlayRenderer = flashOverlayRenderers != null && i < flashOverlayRenderers.Length
                ? flashOverlayRenderers[i]
                : null;

            if (sourceRenderer == null || overlayRenderer == null)
                continue;

            overlayRenderer.sprite = sourceRenderer.sprite;
            overlayRenderer.drawMode = sourceRenderer.drawMode;
            overlayRenderer.size = sourceRenderer.size;
            overlayRenderer.flipX = sourceRenderer.flipX;
            overlayRenderer.flipY = sourceRenderer.flipY;
            overlayRenderer.transform.localPosition = Vector3.zero;
            overlayRenderer.transform.localRotation = Quaternion.identity;
            overlayRenderer.transform.localScale = Vector3.one;
            overlayRenderer.enabled = true;
        }
    }

    private void HideFlashOverlays()
    {
        if (flashOverlayRenderers == null) return;

        for (int i = 0; i < flashOverlayRenderers.Length; i++)
        {
            if (flashOverlayRenderers[i] != null)
                flashOverlayRenderers[i].enabled = false;
        }
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
