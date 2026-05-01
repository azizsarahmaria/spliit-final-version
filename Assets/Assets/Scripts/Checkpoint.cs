using UnityEngine;

public class BubblegumCheckpoint : MonoBehaviour
{
    private Animator anim;
    private bool isUsed = false;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object touching us is the Player
        if (other.CompareTag("Player") && !isUsed)
        {
            DispenseGum();
        }
    }

    void DispenseGum()
    {
        isUsed = true; // Prevents the player from triggering it multiple times

        if (anim != null)
        {
            anim.SetTrigger("isActivated");
        }

        // Add your logic here (e.g., saving the player's position, adding score)
        Debug.Log("Bubblegum Dispensed!");
    }
}