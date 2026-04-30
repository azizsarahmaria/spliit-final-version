using UnityEngine;
using System.Collections;

public class collectible : MonoBehaviour
{
    public int scoreValue = 10;
    public GameObject collectParticleEffect;

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
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        GetComponent<Collider2D>().enabled = false;

        if (collectParticleEffect != null)
        {
            collectParticleEffect.transform.SetParent(null);
            ParticleSystem ps = collectParticleEffect.GetComponent<ParticleSystem>();

            if (ps != null)
            {
                var main = ps.main;
                main.loop = false;

                ps.Play();
                yield return new WaitForSeconds(ps.main.duration + ps.main.startLifetime.constantMax);
                Destroy(collectParticleEffect);
            }
            else
            {
                Destroy(collectParticleEffect);
            }
        }

        Destroy(gameObject);
    }
}