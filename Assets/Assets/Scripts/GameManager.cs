using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Vector2 lastCheckpointPos;
    public int playerLives = 3;
    public int maxLives = 3;

    // Tracks which scene owns the current checkpoint so it resets on level transition
    private string checkpointScene = "";

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
        // If this is a new/different scene (level transition), clear the checkpoint.
        // Characters' Start() methods will then store their spawn position as the default.
        // If it's the same scene reloading (death), the stored checkpoint is intentionally kept.
        if (scene.name != checkpointScene)
        {
            checkpointScene = scene.name;
            lastCheckpointPos = Vector2.zero;
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
            Debug.Log("Game Over! Restarting from scene start.");
            playerLives = maxLives;
            // Clear checkpoint so the scene restarts from the beginning
            lastCheckpointPos = Vector2.zero;
            checkpointScene = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void RestartGame()
    {
        playerLives = maxLives;
        lastCheckpointPos = Vector2.zero;
        checkpointScene = "";
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SetCheckpoint(Vector2 position)
    {
        lastCheckpointPos = position;
        checkpointScene = SceneManager.GetActiveScene().name;
        Debug.Log("Checkpoint saved at: " + position);
    }
}
