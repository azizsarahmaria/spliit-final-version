using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSwitcher : MonoBehaviour
{
    public GameObject character1;
    public GameObject character2;

    [Tooltip("Optional: assign virtual cameras here (CinemachineCamera, CinemachineVirtualCamera, or our custom CameraFollow). " +
             "Their Follow target / target transform will be updated to track the active character on every switch.")]
    public MonoBehaviour[] camerasToFollow;

    private int activeCharacter = 1;

    void Start()
    {
        character1.SetActive(true);
        character2.SetActive(false);
        UpdateCameraFollow(character1.transform);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame ||
            Keyboard.current.rightCtrlKey.wasPressedThisFrame)
        {
            SwitchCharacter();
        }
    }

    void SwitchCharacter()
    {
        GameObject current = activeCharacter == 1 ? character1 : character2;
        GameObject next = activeCharacter == 1 ? character2 : character1;

        // Capture pose + velocity from the outgoing character BEFORE deactivating.
        Vector3 pos = current.transform.position;
        Quaternion rot = current.transform.rotation;
        Vector2 vel = Vector2.zero;
        Rigidbody2D rbCurrent = current.GetComponent<Rigidbody2D>();
        if (rbCurrent != null) vel = rbCurrent.linearVelocity;

        current.SetActive(false);
        next.SetActive(true);

        // Apply pose AFTER activation. This is important: the newly active character's Start()
        // (only on first activation) will respawn at the GameManager checkpoint, which would
        // otherwise teleport us away from where we just were. Setting the transform here overrides that.
        next.transform.position = pos;
        next.transform.rotation = rot;
        Rigidbody2D rbNext = next.GetComponent<Rigidbody2D>();
        if (rbNext != null) rbNext.linearVelocity = vel;

        activeCharacter = activeCharacter == 1 ? 2 : 1;

        UpdateCameraFollow(next.transform);
    }

    // Updates the Follow / target field on each assigned camera. Uses reflection so it works
    // with any Cinemachine version (2.x or 3.x) and with our custom CameraFollow script.
    void UpdateCameraFollow(Transform target)
    {
        if (camerasToFollow == null) return;

        foreach (MonoBehaviour cam in camerasToFollow)
        {
            if (cam == null) continue;
            System.Type type = cam.GetType();

            // Try Cinemachine's public Follow property (3.x and 2.x).
            PropertyInfo followProp = type.GetProperty("Follow", BindingFlags.Public | BindingFlags.Instance);
            if (followProp != null && followProp.CanWrite && typeof(Transform).IsAssignableFrom(followProp.PropertyType))
            {
                followProp.SetValue(cam, target);
                continue;
            }

            // Fallback: legacy m_Follow serialized field.
            FieldInfo followField = type.GetField("m_Follow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (followField != null && typeof(Transform).IsAssignableFrom(followField.FieldType))
            {
                followField.SetValue(cam, target);
                continue;
            }

            // Fallback for our own CameraFollow.cs which exposes a 'playerTransform' field.
            FieldInfo playerField = type.GetField("playerTransform", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (playerField != null && typeof(Transform).IsAssignableFrom(playerField.FieldType))
            {
                playerField.SetValue(cam, target);
            }
        }
    }
}
