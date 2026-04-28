using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    public AudioClip shootingSound;
    public AudioClip jumpSound;
    public AudioClip gettingHitSound;
    public AudioClip blinklesound;

    public AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        audioSource = this.GetComponent<AudioSource>();
    }

    public void Playblinklesound()
    {
        audioSource.PlayOneShot(blinklesound);
    }
    public void PlayShootingSound()
    {
        audioSource.volume = 0.5f;
        audioSource.PlayOneShot(shootingSound);
        audioSource.volume = 0.8f;
    }

    public void PlayJumpSound()
    {
        audioSource.PlayOneShot(jumpSound);
    }

    public void PlayGettingHitSound()
    {
      // audioSource.volume = 0.5f;
        audioSource.PlayOneShot(gettingHitSound);
      // audioSource.volume = 0.8f;

    }
}
