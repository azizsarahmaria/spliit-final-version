using System.Collections;
using UnityEngine;

public class BubbleGumMachine : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Cooldown")]
    [SerializeField] private float dispenseCooldown = 2f;

    private bool canDispense = true;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && canDispense)
        {
            TriggerDispenseAnimation();
        }
    }

    public void TriggerDispenseAnimation()
    {
        if (!canDispense) return;

        // Trigger your dispense animation
        animator.SetTrigger("Dispense");

        StartCoroutine(DispenseCooldown());
    }

    IEnumerator DispenseCooldown()
    {
        canDispense = false;
        yield return new WaitForSeconds(dispenseCooldown);
        canDispense = true;
    }
}