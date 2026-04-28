using UnityEngine;

public class collectible : MonoBehaviour
{
    public int scoreValue = 10;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (ScoreManager.instance != null)
                ScoreManager.instance.AddScore(scoreValue);
            else
                Debug.LogWarning("ScoreManager instance is missing from the scene!");

            if (SFXManager.Instance != null)
                SFXManager.Instance.Playblinklesound();
            else
                Debug.LogWarning("SFXManager instance is missing from the scene!");

            Destroy(gameObject);
        }
    }
}