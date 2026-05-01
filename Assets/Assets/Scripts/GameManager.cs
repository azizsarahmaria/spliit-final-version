using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Vector2 lastCheckpointPos;
    public int playerLives = 3;
    public int maxLives = 3;

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

    // Called by the player whenever they die
    public void PlayerDied()
    {
        playerLives--;

        if (playerLives > 0)
        {
            // Lives remaining — respawn at checkpoint (handled by player script)
            Debug.Log($"Lives remaining: {playerLives}. Respawning at checkpoint.");
        }
        else
        {
            // No lives left — reset everything and reload the scene
            Debug.Log("Game Over! Restarting...");
            playerLives = maxLives;
            lastCheckpointPos = Vector2.zero;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void RestartGame()
    {
        playerLives = maxLives;
        lastCheckpointPos = Vector2.zero;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}