using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine; // or: using Cinemachine;

public class CharacterSwitcher : MonoBehaviour
{
    public GameObject character1;
    public GameObject character2;

    // ? Drag your Virtual Camera here in the Inspector
    public CinemachineCamera virtualCamera;

    private int activeCharacter = 1;

    void Start()
    {
        character1.SetActive(true);
        character2.SetActive(false);

        // ? Set initial follow target
        virtualCamera.Follow = character1.transform;
    }

    void Update()
    {
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

        next.transform.position = current.transform.position;
        next.transform.rotation = current.transform.rotation;

        Rigidbody2D rbCurrent = current.GetComponent<Rigidbody2D>();
        Rigidbody2D rbNext = next.GetComponent<Rigidbody2D>();
        if (rbCurrent != null && rbNext != null)
            rbNext.linearVelocity = rbCurrent.linearVelocity;

        current.SetActive(false);
        next.SetActive(true);

        // ? Tell Cinemachine to follow the new character
        virtualCamera.Follow = next.transform;

        activeCharacter = activeCharacter == 1 ? 2 : 1;
    }
}