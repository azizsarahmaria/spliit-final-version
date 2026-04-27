using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;
    public int score = 0;

    [Header("UI")]
    public GameObject collectibleIconPrefab;  // UI Image prefab of your sprite
    public Transform iconPanel;               // empty panel in Canvas to hold icons

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void AddScore(int amount)
    {
        score += amount;
        SpawnIcon();
    }

    private void SpawnIcon()
    {
        // every collectible picked up = one icon appears in the panel
        Instantiate(collectibleIconPrefab, iconPanel);
    }
}