using UnityEngine;

public class WeaponDropPickup : MonoBehaviour
{
    [Header("Drop Payload")]
    public int weaponIndex = -1;
    public AmmoType ammoType = AmmoType.Pistol;
    public int duplicateAmmoAmountOverride = -1;

    [Header("Duplicate Ammo Rewards (Fixed by Type)")]
    public int pistolAmmoReward = 8;
    public int smgAmmoReward = 20;
    public int shellAmmoReward = 2;
    public int rocketAmmoReward = 1;

    [Header("Lifetime")]
    public float despawnSeconds = 60f;

    bool pickedUp;

    void Start()
    {
        if (despawnSeconds > 0f)
        {
            Destroy(gameObject, despawnSeconds);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (pickedUp)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            inventory = other.GetComponentInParent<PlayerInventory>();
        }

        WeaponSwitcher switcher = other.GetComponentInChildren<WeaponSwitcher>(true);
        if (switcher == null)
        {
            switcher = other.GetComponentInParent<WeaponSwitcher>();
        }

        if (inventory == null || switcher == null)
        {
            return;
        }

        pickedUp = true;

        if (switcher.IsWeaponUnlocked(weaponIndex))
        {
            int ammoGranted = duplicateAmmoAmountOverride >= 0
                ? duplicateAmmoAmountOverride
                : GetAmmoRewardByType(ammoType);
            if (ammoGranted > 0)
            {
                inventory.AddReserveAmmo(ammoType, ammoGranted);
            }

            WeaponBase currentWeapon = switcher.CurrentWeapon;
            if (currentWeapon != null)
            {
                currentWeapon.RefreshAmmoUI();
            }

            Debug.Log("Picked duplicate weapon drop. Ammo added: " + ammoGranted + " (" + ammoType + ")");
        }
        else
        {
            bool selected = switcher.UnlockAndSelectWeapon(weaponIndex);
            Debug.Log(selected
                ? "Picked new weapon drop. Weapon unlocked and auto-equipped."
                : "Picked weapon drop but failed to unlock/select weapon index: " + weaponIndex);
        }

        Destroy(gameObject);
    }

    int GetAmmoRewardByType(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Pistol:
                return Mathf.Max(0, pistolAmmoReward);
            case AmmoType.SMG:
                return Mathf.Max(0, smgAmmoReward);
            case AmmoType.Shell:
                return Mathf.Max(0, shellAmmoReward);
            case AmmoType.Rocket:
                return Mathf.Max(0, rocketAmmoReward);
            default:
                return 0;
        }
    }
}
