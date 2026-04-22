using UnityEngine;

public class Spike : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float moveSpeed = 3f;
    public float patrolDistance = 4f;

    [Header("Components")]
    public Animator anim;

    private Vector2 startPos;
    private int moveDirection = 1;
    private Vector3 originalScale;

    void Start()
    {
        startPos = transform.position;
        originalScale = transform.localScale;
        anim.SetBool("isWalking", true); // always walking
    }

    void HandleAnimations() { } // can delete this entirely

    void Update()
    {
        Patrol();
        HandleAnimations();
    }

    void Patrol()
    {
        // Move left or right
        transform.Translate(Vector2.right * moveDirection * moveSpeed * Time.deltaTime);

        // Turn around at patrol boundaries
        if (transform.position.x >= startPos.x + patrolDistance)
            moveDirection = -1;
        else if (transform.position.x <= startPos.x - patrolDistance)
            moveDirection = 1;

        // Flip sprite to face movement direction
        float scaleX = Mathf.Abs(originalScale.x) * moveDirection;
        transform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);
    }

   
}