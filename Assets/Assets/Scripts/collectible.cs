using UnityEngine;

public class collectible : MonoBehaviour
{
    public int scoreValue = 10;
    public GameObject collectParticlePrefab; // drag your particle prefab here

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

            // Spawn and play particle effect before destroying
            if (collectParticlePrefab != null)
            {
                GameObject particles = Instantiate(collectParticlePrefab, transform.position, Quaternion.identity);
                ParticleSystem ps = particles.GetComponent<ParticleSystem>();
                if (ps != null) ps.Play();
            }
            else
                Debug.LogWarning("No particle prefab assigned on collectible!");

            Destroy(gameObject);
        }
    }
}