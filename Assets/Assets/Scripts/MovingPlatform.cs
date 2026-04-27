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

        target = pointB.position;
    }

    void FixedUpdate()
    {
        Vector2 newPos = Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime);

        // Track velocity BEFORE moving
        PlatformVelocity = (newPos - rb.position) / Time.fixedDeltaTime;

        rb.MovePosition(newPos);

        if (Vector2.Distance(rb.position, target) < 0.05f)
        {
            movingToB = !movingToB;
            target = movingToB ? pointB.position : pointA.position;
        }
    }
}