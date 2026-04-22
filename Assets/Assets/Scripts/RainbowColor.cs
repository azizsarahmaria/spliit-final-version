using UnityEngine;

public class RainbowColor : MonoBehaviour
{
    [Header("Settings")]
    public float cycleSpeed = 1f;       // How fast colors cycle
    public float saturation = 1f;       // Color richness (0-1)
    public float brightness = 1f;       // Brightness (0-1)

    private SpriteRenderer sr;
    private float hue = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        hue += Time.deltaTime * cycleSpeed;
        if (hue > 1f) hue -= 1f;

        sr.color = Color.HSVToRGB(hue, saturation, brightness);
    }
}