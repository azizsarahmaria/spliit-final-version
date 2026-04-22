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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // REMOVED: if (collision.contacts[0].normal.y < -0.5f)
            // Now it triggers if you touch it from the side OR the top.

            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();

            // Look for Animator on the Player or its children
            Animator playerAnim = collision.gameObject.GetComponentInChildren<Animator>();

            if (rb != null)
            {
                // 1. Apply Physics
                // Resetting velocity prevents the "double force" bug if you're already moving
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

                // 2. Play Mushroom Animation
                if (mushroomAnim != null) mushroomAnim.SetTrigger(mushroomTrigger);

                // 3. Play Player Animation
                if (playerAnim != null)
                {
                    playerAnim.SetTrigger(playerJumpTrigger);
                    playerAnim.Play(playerJumpStateName, 0, 0f);
                }
            }
        }
    }
}