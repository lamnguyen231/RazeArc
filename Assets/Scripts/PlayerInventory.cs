using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerInventory : MonoBehaviour, IDamageable
{
    [System.Serializable]
    public class AmmoPool
    {
        public AmmoType ammoType;
        public int amount;
    }

    [Header("Stats")]
    public float playerHealth = 100;
    public float playerMaxHealth = 100;

    [Header("UI")]
    public TextMeshProUGUI healthText;

    [Header("Keycards")]
    public bool hasRedKey = false;
    public bool hasBlueKey = false;
    public bool hasGreenKey = false;

    [Header("Ammo Reserves")]
    public List<AmmoPool> ammoPools = new List<AmmoPool>();

    [Header("Default Ammo")]
    public int defaultPistolAmmo = 24;
    public int defaultSMGAmmo = 60;
    public int defaultShellAmmo = 10;
    public int defaultRocketAmmo = 2;

    // Start is called before the first frame update
    void Start()
    {
        InitializeDefaultAmmoPools();
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

    public void EnsureAmmoPool(AmmoType ammoType, int initialAmount)
    {
        AmmoPool pool = GetAmmoPool(ammoType);
        if (pool != null)
        {
            // Keep the larger startup value so pre-created zeroed pools don't break reloads.
            pool.amount = Mathf.Max(pool.amount, Mathf.Max(0, initialAmount));
            return;
        }

        AmmoPool newPool = new AmmoPool();
        newPool.ammoType = ammoType;
        newPool.amount = Mathf.Max(0, initialAmount);
        ammoPools.Add(newPool);
    }

    public int GetReserveAmmo(AmmoType ammoType)
    {
        AmmoPool pool = GetAmmoPool(ammoType);
        if (pool == null)
        {
            return 0;
        }

        return Mathf.Max(0, pool.amount);
    }

    public int ConsumeReserveAmmo(AmmoType ammoType, int amount)
    {
        int requested = Mathf.Max(0, amount);
        if (requested <= 0)
        {
            return 0;
        }

        AmmoPool pool = GetAmmoPool(ammoType);
        if (pool == null)
        {
            return 0;
        }

        int consumed = Mathf.Min(requested, Mathf.Max(0, pool.amount));
        pool.amount -= consumed;
        return consumed;
    }

    public void AddReserveAmmo(AmmoType ammoType, int amount)
    {
        int addAmount = Mathf.Max(0, amount);
        if (addAmount <= 0)
        {
            return;
        }

        AmmoPool pool = GetAmmoPool(ammoType);
        if (pool == null)
        {
            pool = new AmmoPool();
            pool.ammoType = ammoType;
            pool.amount = 0;
            ammoPools.Add(pool);
        }

        pool.amount += addAmount;
    }

    AmmoPool GetAmmoPool(AmmoType ammoType)
    {
        for (int i = 0; i < ammoPools.Count; i++)
        {
            if (ammoPools[i].ammoType == ammoType)
            {
                return ammoPools[i];
            }
        }

        return null;
    }

    void InitializeDefaultAmmoPools()
    {
        EnsureAmmoPool(AmmoType.Pistol, defaultPistolAmmo);
        EnsureAmmoPool(AmmoType.SMG, defaultSMGAmmo);
        EnsureAmmoPool(AmmoType.Shell, defaultShellAmmo);
        EnsureAmmoPool(AmmoType.Rocket, defaultRocketAmmo);
    }
}
