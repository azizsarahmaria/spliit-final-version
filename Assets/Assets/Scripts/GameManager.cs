using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Vector2 lastCheckpointPos;
    public int playerLives = 3;
    public int maxLives = 3;
    private bool isFirstLoad = true;
    private bool checkpointEverSet = false;


    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isFirstLoad)
        {
            isFirstLoad = false;
            player player = FindFirstObjectByType<player>();
            if (player != null && !checkpointEverSet)
                lastCheckpointPos = player.transform.position;
            return;
        }

        player freshPlayer = FindFirstObjectByType<player>();
        if (freshPlayer != null)
        {
            freshPlayer.Respawn(lastCheckpointPos);
            Debug.Log("Player respawned at: " + lastCheckpointPos);
        }
    }

    public void PlayerDied()
    {
        playerLives--;
        if (playerLives > 0)
        {
            Debug.Log($"Lives remaining: {playerLives}. Respawning at checkpoint.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            Debug.Log("Game Over! Restarting...");
            playerLives = maxLives;
            lastCheckpointPos = Vector2.zero; // ← fixed
            checkpointEverSet = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void RestartGame()
    {
        playerLives = maxLives;
        lastCheckpointPos = Vector2.zero; // ← fixed
        checkpointEverSet = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SetCheckpoint(Vector2 position)
    {
        lastCheckpointPos = position;
        checkpointEverSet = true;
        Debug.Log("Checkpoint saved at: " + position);
    }
}