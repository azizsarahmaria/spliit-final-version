using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header ("Joy Level Sounds")]
    
    public AudioClip footstepssound;
    public AudioClip jumpSound;
    public AudioClip slidesound;
    public AudioClip landingsound;
    public AudioClip gettingHitSound;
    public AudioClip blinklesound;
    public AudioClip spikesound;
    public AudioClip deathsound;

    [Header("Anger Level Sounds")]
    public AudioClip runningsound;
    public AudioClip jumpsound;
    public AudioClip slideSound;
    public AudioClip Landingsound;
    public AudioClip gettinghitSound;
    public AudioClip enemysound; //metel bi hollow knight 
    public AudioClip dashsound;
    public AudioClip enemyhitsound;
    public AudioClip Deathsound;


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
//JOY
    public void Playblinklesound()
    {
        audioSource.PlayOneShot(blinklesound);
    }

    public void Playslidesound()
    {
        audioSource.PlayOneShot(slidesound);
    }

    public void Playlandingsound()
    {
        audioSource.PlayOneShot(landingsound);
    }
    public void Playfootstepssound()
    {
        audioSource.PlayOneShot(footstepssound);  
    }
   

    public void PlayJumpSound()
    {
        audioSource.PlayOneShot(jumpSound);
    }
    public void Playspikesound()
    {
        audioSource.PlayOneShot(spikesound);
    }

    public void PlayGettingHitSound()
    {
      // audioSource.volume = 0.5f;
        audioSource.PlayOneShot(gettingHitSound);
      // audioSource.volume = 0.8f;

    }

//ANGER

}
