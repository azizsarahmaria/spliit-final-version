using UnityEngine;
using TMPro; // or use UnityEngine.UI if you're using Text instead

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;
    public int score = 0;
    public TextMeshProUGUI scoreText; // drag your UI text here in the Inspector

    private void Awake()
    {
        // Singleton pattern — only one ScoreManager can exist
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }
}