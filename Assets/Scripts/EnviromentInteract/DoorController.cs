using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    public enum DoorType
    {
        Free,
        Locked,
        ArenaEntry,
        ArenaExit
    }

    private enum DoorState
    {
        Idle,
        Opening,
        Closing
    }

    [Header("Door Object")]
    public GameObject doorMesh;

    [Header("Door SFX")]
    public AudioClip doorSound;

    [Header("Door Type")]
    public DoorType doorType = DoorType.Free;

    [Header("Key Settings (only used when Door Type = Locked)")]
    public string requiredKey = "Red";

    [Header("Enemy Settings")]
    [Tooltip("Drag the parent object containing all the room's enemies into this slot.")]
    public Transform enemyContainer;

    [Header("Close Settings")]
    public bool autoClose = true;

    [Header("Animation Settings")]
    public float slideHeight = 5f;
    public float slideSpeed = 5f;

    private DoorState state = DoorState.Idle;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isLockedInArena = false;
    private AudioSource audioSource;

    private void Start()
    {
        if (doorMesh == null)
        {
            Debug.LogError($"[DoorController] '{name}': doorMesh is not assigned!", this);
            return;
        }

        startPosition = doorMesh.transform.position;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; 
        audioSource.playOnAwake = false;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        CheckDoorConditions(other.gameObject);
    }

    public void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (doorType == DoorType.ArenaEntry && !isLockedInArena && !AreEnemiesDead())
        {
            isLockedInArena = true;
            CloseDoor();
            StartCoroutine(ScanForDefeatedEnemies());
            return;
        }

        if (autoClose)
            CloseDoor();
    }

    private void Update()
    {
        if (state != DoorState.Idle && doorMesh != null)
        {
            doorMesh.transform.position = Vector3.MoveTowards(
                doorMesh.transform.position,
                targetPosition,
                slideSpeed * Time.deltaTime
            );

            if (doorMesh.transform.position == targetPosition)
                state = DoorState.Idle;
        }
    }

    private IEnumerator ScanForDefeatedEnemies()
    {
        while (!AreEnemiesDead())
            yield return new WaitForSeconds(0.5f);

        isLockedInArena = false;
        OpenDoor();
    }

    private void CheckDoorConditions(GameObject player)
    {
        switch (doorType)
        {
            case DoorType.Free:
                OpenDoor();
                break;

            case DoorType.ArenaEntry:
                HandleArenaDoorEntry();
                break;

            case DoorType.Locked:
                HandleKeyDoorEntry(player);
                break;

            case DoorType.ArenaExit:
                HandleRoomClearedDoorEntry();
                break;
        }
    }

    private void HandleArenaDoorEntry()
    {
        if (isLockedInArena)
        {
            if (AreEnemiesDead())
            {
                isLockedInArena = false;
                OpenDoor();
            }
            else
            {
                Debug.Log("[DoorController] Arena locked: defeat all enemies to escape!");
            }
        }
        else
        {
            OpenDoor();
        }
    }

    private void HandleKeyDoorEntry(GameObject player)
    {
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();

        if (inventory == null)
        {
            Debug.LogWarning($"[DoorController] '{name}': Player is missing a PlayerInventory component.");
            return;
        }

        bool hasKey = requiredKey switch
        {
            "Red" => inventory.hasRedKey,
            "Blue" => inventory.hasBlueKey,
            "Green" => inventory.hasGreenKey,
            _ => false
        };

        if (hasKey)
            OpenDoor();
        else
            Debug.Log($"[DoorController] Door locked: you need the {requiredKey} key!");
    }

    private void HandleRoomClearedDoorEntry()
    {
        if (AreEnemiesDead())
            OpenDoor();
        else
            Debug.Log("[DoorController] Door locked: clear the room first!");
    }

    private bool AreEnemiesDead()
    {
        if (enemyContainer == null) return true;
        if (enemyContainer.childCount == 0) return true;

        foreach (Transform childEnemy in enemyContainer)
        {
            if (childEnemy == null) continue;

            EnemyAI enemy = childEnemy.GetComponent<EnemyAI>();

            if (enemy != null && !enemy.isDead)
                return false;
        }

        return true;
    }

    private void OpenDoor()
    {
        if (doorMesh == null) return;
        targetPosition = startPosition + new Vector3(0, slideHeight, 0);
        state = DoorState.Opening;
        if (doorSound != null) audioSource.PlayOneShot(doorSound);
    }

    private void CloseDoor()
    {
        if (doorMesh == null) return;
        targetPosition = startPosition;
        state = DoorState.Closing;
        if (doorSound != null) audioSource.PlayOneShot(doorSound);
    }
}