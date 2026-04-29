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

    public Vector2 PlatformVelocity { get; private set; }

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

        rb.position = pointA.position; // ← snap platform to start
        target = pointB.position;
    }

    void FixedUpdate()
    {
        Vector2 newPos = Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime);

        PlatformVelocity = (newPos - rb.position) / Time.fixedDeltaTime;

        rb.MovePosition(newPos);

        // ✅ Use newPos — rb.position is still the old value here
        if (Vector2.Distance(newPos, target) < 0.05f)
        {
            movingToB = !movingToB;
            target = movingToB ? pointB.position : pointA.position;
        }
    }
}