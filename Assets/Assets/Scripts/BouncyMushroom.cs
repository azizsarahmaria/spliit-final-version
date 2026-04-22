using UnityEngine;

public class BouncyMushroom : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float bounceForce = 25f;

    [Header("Animation Names")]
    [SerializeField] private string mushroomTrigger = "Bounce";
    [SerializeField] private string playerJumpTrigger = "Jump";
    [SerializeField] private string playerJumpStateName = "Player_Jump"; // The actual name of the animation clip/state

    private Animator mushroomAnim;

    private void Awake()
    {
        mushroomAnim = GetComponent<Animator>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.contacts[0].normal.y < -0.5f)
            {
                Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
                Animator playerAnim = collision.gameObject.GetComponent<Animator>();

                if (rb != null)
                {
                    // 1. Apply Physics
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

                    // 2. Play Mushroom Animation
                    if (mushroomAnim != null) mushroomAnim.SetTrigger(mushroomTrigger);

                    // 3. Play Player Animation
                    if (playerAnim != null)
                    {
                        // Strategy A: Fire the trigger
                        playerAnim.SetTrigger(playerJumpTrigger);

                        // Strategy B: Force the state (The "Hammer" approach)
                        // Use this if the trigger is being ignored by your transitions
                        playerAnim.Play(playerJumpStateName, 0, 0f);
                    }
                }
            }
        }
    }
}