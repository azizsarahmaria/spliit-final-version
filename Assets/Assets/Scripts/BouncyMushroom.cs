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
            Vector2 contactNormal = collision.contacts[0].normal;

            if (contactNormal.y < -0.5f) // ✅ negative for this mushroom
            {
                Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
                player playerScript = collision.gameObject.GetComponent<player>();

                if (rb != null && playerScript != null)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

                    playerScript.isGrounded = false;
                    playerScript.jumpsRemaining = 1;
                    playerScript.DisableVariableJump();

                    if (mushroomAnim != null)
                        mushroomAnim.SetTrigger(mushroomTrigger);
                }
            }
        }
    }
}