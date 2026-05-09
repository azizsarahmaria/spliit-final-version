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
        // Try to find the sprite on this object first
        sr = GetComponent<SpriteRenderer>();

        // If not found, look in child objects (like "Square")
        if (sr == null)
        {
            sr = GetComponentInChildren<SpriteRenderer>();
        }

        // Safety check to avoid errors if no sprite exists at all
        if (sr == null)
        {
            Debug.LogWarning($"RainbowColor on {gameObject.name} has no SpriteRenderer in children!");
        }
    }

    void Update()
    {
        // If sr is null, the script will skip this frame instead of crashing
        if (sr == null) return;

        hue += Time.deltaTime * cycleSpeed;
        if (hue > 1f) hue -= 1f;

        sr.color = Color.HSVToRGB(hue, saturation, brightness);
    }
}