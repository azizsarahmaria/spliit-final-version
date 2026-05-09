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
        // Hide the collectible sprite and disable its collider immediately
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        GetComponent<Collider2D>().enabled = false;

        if (collectParticleEffect != null)
        {
            // Create a brand new copy of the effect at this collectible's position
            GameObject effect = Instantiate(collectParticleEffect, transform.position, Quaternion.identity);
            effect.SetActive(true);

            // Read the animation clip length so we wait exactly the right amount of time
            Animator effectAnim = effect.GetComponent<Animator>();
            float waitTime = 1f; // fallback in case there's no Animator

            if (effectAnim != null)
            {
                // Wait one frame so the Animator has time to start playing
                yield return null;
                AnimatorClipInfo[] clips = effectAnim.GetCurrentAnimatorClipInfo(0);
                if (clips.Length > 0)
                    waitTime = clips[0].clip.length - 0.6f;
            }

            yield return new WaitForSeconds(waitTime);
            Destroy(effect);
        }

        Destroy(gameObject);
    }
}