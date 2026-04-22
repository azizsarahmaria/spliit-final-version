using UnityEngine;

public class BouncyMushroom : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float bounceForce = 25f;

    [Header("Animation Names")]
    [SerializeField] private string mushroomTrigger = "Bounce";
    [SerializeField] private string playerJumpTrigger = "Jump";
    [SerializeField] private string playerJumpStateName = "Player_Jump";

    private Animator mushroomAnim;

    private void Awake()
    {
        mushroomAnim = GetComponent<Animator>();
    }

    // Switched to Trigger so the player doesn't "hit a wall"
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            Animator playerAnim = other.GetComponentInChildren<Animator>();

            if (rb != null)
            {
                // Kill downward velocity so the bounce is consistent
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

                // Use Force instead of Impulse if it feels too "teleporty" 
                // but Impulse is usually better for trampolines
                rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

                if (mushroomAnim != null) mushroomAnim.SetTrigger(mushroomTrigger);

                if (playerAnim != null)
                {
                    // 1. Apply Physics again for consistent bounce
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

                    // 2. Play Mushroom Animation
                    if (mushroomAnim != null) mushroomAnim.SetTrigger(mushroomTrigger);

                    // 3. Play Player Animation
                    playerAnim.SetTrigger(playerJumpTrigger);
                    playerAnim.Play(playerJumpStateName, 0, 0f);
                }
            }
        }
    }
}