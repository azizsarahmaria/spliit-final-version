using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to any hazard object (spikes, lava, enemy, etc.)
/// Kills the player on contact and respawns them at the last
/// activated checkpoint, or restarts the scene if none exist.
/// </summary>
public class Hazard2 : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tag used to identify the player GameObject.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Optional delay (seconds) before respawning — useful for death effects.")]
    [SerializeField] private float respawnDelay = 0.5f;

    [Tooltip("Trigger on Enter (default) or Stay — useful for damage-over-time zones.")]
    [SerializeField] private bool triggerOnStay = false;

    // ?? Checkpoint static state ????????????????????????????????????????????
    // Static so all hazards and scenes share the same checkpoint data.
    private static Vector3? lastCheckpointPosition = null;

    /// <summary>
    /// Call this from your Checkpoint script when the player activates it.
    /// </summary>
    public static void RegisterCheckpoint(Vector3 position)
    {
        lastCheckpointPosition = position;
        Debug.Log($"[Hazard] Checkpoint registered at {position}");
    }

    /// <summary>
    /// Clears the stored checkpoint (e.g. when starting a new game).
    /// </summary>
    public static void ClearCheckpoint()
    {
        lastCheckpointPosition = null;
    }

    // ?? Collision detection ????????????????????????????????????????????????
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOnStay && other.CompareTag(playerTag))
            HandlePlayerDeath(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (triggerOnStay && other.CompareTag(playerTag))
            HandlePlayerDeath(other.gameObject);
    }

    // ?? Death / respawn logic ??????????????????????????????????????????????
    private void HandlePlayerDeath(GameObject player)
    {
        // Prevent multiple triggers while the coroutine is running
        // by disabling the hazard collider temporarily.
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        StartCoroutine(RespawnRoutine(player, col));
    }

    private IEnumerator RespawnRoutine(GameObject player, Collider2D hazardCollider)
    {
        // ?? Optional: disable player input / play death anim here ??????????
        // Example: player.GetComponent<PlayerController>().SetDead(true);

        yield return new WaitForSeconds(respawnDelay);

        if (lastCheckpointPosition.HasValue)
        {
            // Respawn at the last activated checkpoint
            player.transform.position = lastCheckpointPosition.Value;

            // ?? Optional: re-enable player input here ??????????????????????
            // Example: player.GetComponent<PlayerController>().SetDead(false);

            Debug.Log($"[Hazard] Player respawned at checkpoint {lastCheckpointPosition.Value}");
        }
        else
        {
            // No checkpoint — reload from scene index 0
            Debug.Log("[Hazard] No checkpoint found. Reloading scene index 0.");
            SceneManager.LoadScene(1);
            yield break; // Scene is reloading; stop the coroutine
        }

        // Re-enable the hazard collider after respawn
        if (hazardCollider != null)
            hazardCollider.enabled = true;
    }
}