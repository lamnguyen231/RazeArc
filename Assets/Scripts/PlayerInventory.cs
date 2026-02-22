using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerInventory : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float playerHealth = 100;
    public float playerMaxHealth = 100;

    [Header("UI")]
    public TextMeshProUGUI healthText;

    [Header("Keycards")]
    public bool hasRedKey = false;
    public bool hasBlueKey = false;
    public bool hasGreenKey = false;

    // Start is called before the first frame update
    void Start()
    {
        UpdateHealthUI();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void PickUpKey(string keyColor)
    {
        if (keyColor == "Red") hasRedKey = true;
        else if (keyColor == "Blue") hasBlueKey = true;
        else if (keyColor == "Green") hasGreenKey = true;
    }

    public void TakeDamage(float damageAmount)
    {
        playerHealth -= damageAmount;
        Debug.Log($"You have taken ${damageAmount} amount of damage.");

        UpdateHealthUI();

        if (playerHealth <= 0)
        {
            Debug.Log("You are ded.");
            //SceneManager.LoadScene("GameOverRetry");
        }
    }

    public void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = "Health: " + playerHealth;
        }
    }
}
