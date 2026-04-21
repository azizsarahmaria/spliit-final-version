using UnityEngine;

public class collectible : MonoBehaviour
{
    public int scoreValue = 10;   // change this per coin if you want some worth more

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ScoreManager.instance.AddScore(scoreValue);
            Destroy(gameObject);
        }
    }
}