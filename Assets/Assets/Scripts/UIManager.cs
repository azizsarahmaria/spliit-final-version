using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManagerScript : MonoBehaviour
{
    public TextMeshProUGUI coinText;
    public GameObject pauseMenuUI; // Reference to the pause menu UI GameObject
    public GameObject Losescreen;
    public GameObject Winscreen;
    public bool isPaused = false;

    public static UIManagerScript Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        Instance = this;
    }

    public void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ShowLosescreen()
    {
        Losescreen.SetActive(true);
    }

    public void ShowWinscreen()
    {
        Winscreen.SetActive(true);
    }
    

    public void ResumeGame()
    {
        Time.timeScale = 1f; // Resume the game by setting time scale back to 1
        pauseMenuUI.SetActive(false);
        isPaused = false;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;        // Ensure the game is running before restarting
        SceneManager.LoadScene("level1");  //SceneManager.GetActiveScene().buildIndex
        isPaused = false;
    }

    public void LoadLevel(int index)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(index);
    }

    public void QuitGame()
    {
        Application.Quit(); // Quit the application
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void UpdateCoinUI(int amount)
    {
        coinText.text = "Coins: " + amount;
    }
}
