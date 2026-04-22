using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform pointA;
    public Transform pointB;

    [Header("Settings")]
    public float speed = 2f;

    private Rigidbody2D rb;
    private Vector3 target;
    private bool movingToB = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        target = pointB.position;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
    }

    void FixedUpdate()
    {
        Vector2 newPos = Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        if (Vector2.Distance(rb.position, target) < 0.05f)
        {
            movingToB = !movingToB;
            target = movingToB ? pointB.position : pointA.position;
        }
    }

    // Instead of parenting, we manually move any player touching the top
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Only move the player if they are on TOP of the platform
            if (collision.contacts[0].normal.y < -0.5f)
            {
                Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
                Vector2 platformVelocity = (target - transform.position).normalized * speed;

                // Add the platform's horizontal movement to the player
                playerRb.position += new Vector2(platformVelocity.x, 0) * Time.fixedDeltaTime;
            }
        }
    }
}