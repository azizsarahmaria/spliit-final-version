using UnityEngine;
using System.Collections;

public class collectible : MonoBehaviour
{
    public int scoreValue = 10;
    public GameObject collectParticleEffect; // back to GameObject — just drag particle object here

    private bool collected = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Trigger hit by: " + other.name + " | Tag: " + other.tag);

        if (other.CompareTag("Player") && !collected)
        {
            collected = true;

            if (ScoreManager.instance != null)
                ScoreManager.instance.AddScore(scoreValue);

            if (SFXManager.Instance != null)
                SFXManager.Instance.Playblinklesound();

            StartCoroutine(CollectRoutine());
        }
    }

    private IEnumerator CollectRoutine()
    {
        // Get SpriteRenderer from children too
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        GetComponent<Collider2D>().enabled = false;

        if (collectParticleEffect != null)
        {
            collectParticleEffect.transform.SetParent(null);
            ParticleSystem ps = collectParticleEffect.GetComponent<ParticleSystem>();
            ps.Play();
            yield return new WaitForSeconds(ps.main.duration);
            Destroy(collectParticleEffect);
        }

        Destroy(gameObject);
    }
}