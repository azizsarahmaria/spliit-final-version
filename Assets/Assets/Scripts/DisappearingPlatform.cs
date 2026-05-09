using System.Collections;
using UnityEngine;

public class DisappearingPlatform : MonoBehaviour
{
    [Header("Settings")]
    public float delayBeforeFade = 0.5f;
    public float fadeDuration = 1.0f;
    public float respawnDelay = 3.0f;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D cloudCollider;
    private Color originalColor;
    private bool isProcessing = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        cloudCollider = GetComponent<BoxCollider2D>();
        originalColor = spriteRenderer.color;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Only trigger if the player lands on top
        if (collision.gameObject.CompareTag("Player") && !isProcessing)
        {
            // Optional: Check if the player is above the platform
            if (collision.contacts[0].normal.y < -0.5f)
            {
                StartCoroutine(FadingRoutine());
            }
        }
    }

    IEnumerator FadingRoutine()
    {
        isProcessing = true;

        // 1. Wait a moment after the player touches it
        yield return new WaitForSeconds(delayBeforeFade);

        // 2. Gradually fade the alpha
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);
            yield return null;
        }

        // 3. Turn off visuals and physics
        spriteRenderer.enabled = false;
        cloudCollider.enabled = false;

        // 4. Wait and then respawn
        yield return new WaitForSeconds(respawnDelay);
        ResetCloud();
    }

    void ResetCloud()
    {
        spriteRenderer.color = originalColor;
        spriteRenderer.enabled = true;
        cloudCollider.enabled = true;
        isProcessing = false;
    }
}