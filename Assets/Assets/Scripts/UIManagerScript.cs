using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManagerScript : MonoBehaviour
{

    public GameObject pauseMenuUI; // Reference to the pause menu UI GameObject
    public void PauseGame()
    {
        pauseMenuUI.SetActive(true);

        Time.timeScale = 0f; // Pause the game by setting time scale to 0 we2if l waet bl game tabaana
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f; // Resume the game by setting time scale back to 1
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;        // Ensure the game is running before restarting
        SceneManager.LoadScene("level1");  //SceneManager.GetActiveScene().buildIndex
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
            PauseGame();
        }
    }
}
