using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SwitchFunction
{
    EndLevel,
    OpenDoor
}

public class SwitchController : MonoBehaviour
{
    [Header("Switch Settings")]
    public SwitchFunction switchAction = SwitchFunction.EndLevel;

    [Tooltip("Which button should the player press?")]
    public KeyCode interactKey = KeyCode.E;

    [Header("NextLevel")]
    public string nextLevelName;

    [Header("If Open Door")]
    public GameObject doorToOpen;

    [Header("Visual Feedback")]
    [Tooltip("Drag the 3D button model here")]
    public GameObject buttonModel;
    [Tooltip("Drag your Green Material here")]
    public Material pressedMaterial;

    private bool isPlayerInRange = false;
    private bool hasBeenPressed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            Debug.Log("Player is near the switch. Press " + interactKey.ToString() + " to interact.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }

    private void Update()
    {
        if (isPlayerInRange && !hasBeenPressed && Input.GetKeyDown(interactKey))
        {
            ActivateSwitch();
        }
    }

    private void ActivateSwitch()
    {
        hasBeenPressed = true;

        if (buttonModel != null && pressedMaterial != null)
        {
            MeshRenderer renderer = buttonModel.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = pressedMaterial;
            }
        }

        if (switchAction == SwitchFunction.EndLevel)
        {
            Debug.Log("Level finished. Loading next level.");
            SceneManager.LoadScene(nextLevelName); 
        }
        else if (switchAction == SwitchFunction.OpenDoor)
        {
            Debug.Log("Switch pressed. Opening door.");

            if (doorToOpen != null)
            {
                doorToOpen.SetActive(false);
            }
        }
    }
}
