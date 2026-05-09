using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to any instant-kill hazard (pit, lava, etc.).
/// Delegates death to the character script so all respawn logic stays
/// inside GameManager — no duplicate checkpoint state.
/// </summary>
public class Hazard : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tag used to identify the player GameObject.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Optional delay (seconds) before death triggers — useful for brief death effects.")]
    [SerializeField] private float deathDelay = 0.5f;

    [Tooltip("Trigger on Enter (default) or Stay — useful for damage-over-time zones.")]
    [SerializeField] private bool triggerOnStay = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOnStay && other.CompareTag(playerTag))
            StartCoroutine(KillRoutine(other.gameObject));
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (triggerOnStay && other.CompareTag(playerTag))
            StartCoroutine(KillRoutine(other.gameObject));
    }

    private IEnumerator KillRoutine(GameObject playerObj)
    {
        // Temporarily disable collider to prevent re-triggering during the delay
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        yield return new WaitForSeconds(deathDelay);

        if (playerObj == null)
        {
            if (col != null) col.enabled = true;
            yield break;
        }

        // Level 1: Joy character
        player joyScript = playerObj.GetComponent<player>();
        if (joyScript != null)
        {
            joyScript.Die();
            if (col != null) col.enabled = true;
            yield break;
        }

        // Level 2: Anger character
        AngerHealth angerHealth = playerObj.GetComponent<AngerHealth>();
        if (angerHealth != null)
        {
            angerHealth.TakeDamage(1, transform.position);
            if (col != null) col.enabled = true;
            yield break;
        }

        // Fallback: let GameManager handle it directly
        if (GameManager.instance != null)
            GameManager.instance.PlayerDied();

        if (col != null) col.enabled = true;
    }
}
