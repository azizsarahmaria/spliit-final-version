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
        if (activeCharacter == 1)
        {
            character1.SetActive(false);
            character2.SetActive(true);
            activeCharacter = 2;
        }
        else
        {
            character1.SetActive(true);
            character2.SetActive(false);
            activeCharacter = 1;
        }
    }
}