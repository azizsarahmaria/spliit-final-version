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
    private Vector2 previousPosition;

    // Track the player currently on the platform
    private Rigidbody2D playerRb;
    private bool playerOnTop = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;

        if (pointA == null || pointB == null)
        {
            Debug.LogError("MovingPlatform: PointA or PointB not assigned!", this);
            enabled = false;
            return;
        }

        target = pointB.position;
        previousPosition = rb.position;
    }

    void FixedUpdate()
    {
        previousPosition = rb.position;

        Vector2 newPos = Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        if (Vector2.Distance(rb.position, target) < 0.05f)
        {
            movingToB = !movingToB;
            target = movingToB ? pointB.position : pointA.position;
        }

        // If player is standing still on platform, carry them with it
        if (playerOnTop && playerRb != null)
        {
            float playerHorizontalInput = playerRb.linearVelocity.x;
            bool playerIsStandingStill = Mathf.Abs(playerHorizontalInput) < 0.1f;

            if (playerIsStandingStill)
            {
                Vector2 platformDelta = rb.position - previousPosition;
                playerRb.MovePosition(playerRb.position + new Vector2(platformDelta.x, 0f));
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f)
            {
                collision.transform.SetParent(transform);
                playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
                playerOnTop = true;
                break;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        collision.transform.SetParent(null);
        playerRb = null;
        playerOnTop = false;
    }
}