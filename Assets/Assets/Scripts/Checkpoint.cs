using UnityEngine;

public class BubblegumMachine : MonoBehaviour
{
    private Animator anim;
    private bool isUsed = false;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isUsed)
        {
            Activate();
        }
    }

    void Activate()
    {
        isUsed = true;
        if (anim != null) anim.SetTrigger("isActivated");

        if (GameManager.instance != null)
        {
            // Save Position
            GameManager.instance.lastCheckpointPos = transform.position;

            // OPTIONAL: Refill lives when reaching a checkpoint
            GameManager.instance.playerLives = GameManager.instance.maxLives;

            Debug.Log("Checkpoint Saved & Lives Refilled!");
        }
    }
}