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
            SFXManager.Instance.Playbubblegum();

            Activate();
        }
    }


    void Activate()
    {
        isUsed = true;
        if (anim != null) anim.SetTrigger("isActivated");

        if (GameManager.instance != null)
        {
            // ── use SetCheckpoint so the flag is properly set ──
            GameManager.instance.SetCheckpoint(transform.position);

            // Refill lives when reaching a checkpoint
            GameManager.instance.playerLives = GameManager.instance.maxLives;
            Debug.Log("Checkpoint Saved & Lives Refilled!");
        }
    }
}