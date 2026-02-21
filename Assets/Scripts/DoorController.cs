using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Object")]
    public GameObject doorMesh;

    [Header("Open Condition")]
    public bool isNormalDoor = false;
    public string requiredKey = "None";
    public bool isRoomCleared = false;
    [Tooltip("Drag the enemies for this specific room into this list")]
    public GameObject[] roomEnemies;

    [Header("Close Settings")]
    public bool autoClose = true;

    [Header("Animation Settings")]
    public float slideHeight = 5f;
    public float slideSpeed = 5f;

    private bool isOpening = false;
    private bool isClosing = false;
    private Vector3 targetPosition;
    private Vector3 startPosition;

    // 1. Remember where the floor is when the game starts
    void Start()
    {
        if (doorMesh != null)
        {
            startPosition = doorMesh.transform.position;
        }
    }

    // 2. Open when entering the box
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CheckDoorConditions(other.gameObject);
        }
    }

    // 3. Close when leaving the box
    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && autoClose)
        {
            CloseDoor();
        }
    }

    private void CheckDoorConditions(GameObject player)
    {
        if (isNormalDoor)
        {
            OpenDoor();
            return;
        }

        if (requiredKey != "None")
        {
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();

            if (requiredKey == "Red" && inventory.hasRedKey)
            {
                OpenDoor();
                return;
            }
            else if (requiredKey == "Blue" && inventory.hasBlueKey)
            {
                OpenDoor();
                return;
            }
            else if (requiredKey == "Green" && inventory.hasGreenKey)
            {
                OpenDoor();
                return;
            }
            else
            {
                Debug.Log("Door Locked: You need the " + requiredKey + " key!");
            }
        }

        if (isRoomCleared)
        {
            if (AreEnemiesDead())
            {
                OpenDoor();
                return;
            }
            else
            {
                Debug.Log("Door Locked: You must clear the room first!");
                return;
            }
        }
    }

    private bool AreEnemiesDead()
    {
        if (roomEnemies.Length == 0) return true;

        foreach (GameObject enemy in roomEnemies)
        {
            if (enemy != null)
            {
                return false;
            }
        }

        return true;
    }

    private void OpenDoor()
    {
        Debug.Log("Opening Door");

        targetPosition = startPosition + new Vector3(0, slideHeight, 0);

        isOpening = true;
        isClosing = false; 

        this.enabled = true;
    }

    private void CloseDoor()
    {
        Debug.Log("Closing Door");

        targetPosition = startPosition;

        isOpening = false;
        isClosing = true;

        this.enabled = true;
    }

    void Update()
    {
        if (isOpening || isClosing)
        {
            doorMesh.transform.position = Vector3.MoveTowards(
                doorMesh.transform.position,
                targetPosition,
                slideSpeed * Time.deltaTime
            );

            if (doorMesh.transform.position == targetPosition)
            {
                isOpening = false;
                isClosing = false;
                this.enabled = false;
                Debug.Log("Door finished moving. Script disabled.");
            }
        }
    }
}