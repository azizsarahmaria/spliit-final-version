using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;  // ? Add this

public class CharacterSwitcher : MonoBehaviour
{
    public GameObject character1;
    public GameObject character2;

    private int activeCharacter = 1;

    void Start()
    {
        character1.SetActive(true);
        character2.SetActive(false);
    }

    void Update()
    {
        // ? Replace Input.GetKeyDown with Keyboard.current
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

        // Sync transform
        next.transform.position = current.transform.position;
        next.transform.rotation = current.transform.rotation;

        // Sync Rigidbody2D velocity (optional but recommended)
        Rigidbody2D rbCurrent = current.GetComponent<Rigidbody2D>();
        Rigidbody2D rbNext = next.GetComponent<Rigidbody2D>();
        if (rbCurrent != null && rbNext != null)
            rbNext.linearVelocity = rbCurrent.linearVelocity;

        current.SetActive(false);
        next.SetActive(true);
        activeCharacter = activeCharacter == 1 ? 2 : 1;

        GameObject.FindObjectOfType<CameraFollowChanger>().ChangeFollowTarget(next.transform);
    }
}