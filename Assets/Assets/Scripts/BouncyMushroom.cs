using UnityEngine;

public class BouncyMushroom : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float bounceForce = 25f;

    [Header("Animation Names")]
    [SerializeField] private string mushroomTrigger = "Bounce";

    private Animator mushroomAnim;

    private void Awake()
    {
        mushroomAnim = GetComponent<Animator>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Get the contact point normal (direction of collision)
            Vector2 contactNormal = collision.contacts[0].normal;

            // Only bounce if player is hitting from above
            // Normal points DOWN (negative Y) when player lands on top
            if (contactNormal.y < -0.5f)
            {
                Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
                player playerScript = collision.gameObject.GetComponent<player>();

                if (rb != null && playerScript != null)
                {
                    // Apply bounce force
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

                    // Set player as airborne for animation
                    playerScript.isGrounded = false;

                    // IMPORTANT: Set jumps remaining to 1
                    // This allows only ONE more jump until they land
                    playerScript.jumpsRemaining = 1;

                    // Play mushroom bounce animation
                    if (mushroomAnim != null)
                    {
                        mushroomAnim.SetTrigger(mushroomTrigger);
                    }
                }
            }
            // If hitting from side or bottom, mushroom acts like a normal wall
        }
    }
}