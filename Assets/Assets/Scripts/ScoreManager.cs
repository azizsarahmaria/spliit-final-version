using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;
    public int score = 0;

    [Header("UI")]
    public Image collectibleIcon;
    public TextMeshProUGUI scoreText;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (scoreText == null)
            scoreText = GetComponent<TextMeshProUGUI>();

        if (scoreText == null)
            scoreText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (collectibleIcon == null)
            collectibleIcon = GetComponentInChildren<Image>(true);

        RefreshScoreUI();
    }

    public void AddScore(int amount)
    {
        score += amount;
        Debug.Log("Score is now: " + score + " | scoreText is: " + scoreText);
        RefreshScoreUI();
    }

    private void RefreshScoreUI()
    {
        if (scoreText == null) return;

        if (!scoreText.gameObject.activeSelf)
            scoreText.gameObject.SetActive(true);

        scoreText.enabled = true;
        scoreText.text = "x " + score;
    }
}
