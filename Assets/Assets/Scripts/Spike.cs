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

        // Safety check: only set the bool if the animator actually exists
        if (anim != null)
        {
            anim.SetBool("isWalking", true);
        }
    }

    void Update()
    {
        Patrol();
    }

    void Patrol()
    {
        transform.Translate(Vector2.right * moveDirection * moveSpeed * Time.deltaTime);

        if (transform.position.x >= startPos.x + patrolDistance)
            moveDirection = -1;
        else if (transform.position.x <= startPos.x - patrolDistance)
            moveDirection = 1;

        float scaleX = Mathf.Abs(originalScale.x) * moveDirection;
        transform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);
    }
}