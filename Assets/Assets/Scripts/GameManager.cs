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

    public void RestartGame()
    {
        playerLives = maxLives;
        lastCheckpointPos = Vector2.zero;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}