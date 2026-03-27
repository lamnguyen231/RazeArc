using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class WeaponSwitcher : MonoBehaviour
{
    public event Action<WeaponBase, int> WeaponSelected;

    [Header("Weapon List (Order Matters)")]
    public WeaponBase[] weapons;

    [Header("Unlock State")]
    public bool[] weaponUnlocked;

    int currentWeaponIndex = 0;

    public WeaponBase CurrentWeapon
    {
        get
        {
            if (weapons == null || weapons.Length == 0)
            {
                return null;
            }

            if (currentWeaponIndex < 0 || currentWeaponIndex >= weapons.Length)
            {
                return null;
            }

            return weapons[currentWeaponIndex];
        }
    }

    public bool IsWeaponUnlocked(int index)
    {
        if (weaponUnlocked == null)
        {
            return false;
        }

        if (index < 0 || index >= weaponUnlocked.Length)
        {
            return false;
        }

        return weaponUnlocked[index];
    }

    public bool SelectWeaponByIndex(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length)
        {
            return false;
        }

        if (!IsWeaponUnlocked(index))
        {
            return false;
        }

        SelectWeapon(index);
        return true;
    }

    public bool UnlockAndSelectWeapon(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length)
        {
            return false;
        }

        UnlockWeapon(index);
        SelectWeapon(index);
        return true;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Ensure weaponUnlocked array matches weapons array length
        if (weapons.Length != weaponUnlocked.Length)
        {
            weaponUnlocked = new bool[weapons.Length];
            Debug.LogWarning("Weapon list and unlock state list must be the same length!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleNumberKeys();
        HandleScrollWheel();
    }

    void HandleNumberKeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) TrySelectWeapon(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TrySelectWeapon(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TrySelectWeapon(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TrySelectWeapon(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) TrySelectWeapon(4);
    }

    void HandleScrollWheel()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
        {
            CycleWeapon(1);
        }
        else if (scroll < 0f)
        {
            CycleWeapon(-1);
        }
    }

    void CycleWeapon(int direction)
    {
        int startIndex = currentWeaponIndex;

        do
        {
            currentWeaponIndex += direction;

            if (currentWeaponIndex >= weapons.Length)
                currentWeaponIndex = 0;

            if (currentWeaponIndex < 0)
                currentWeaponIndex = weapons.Length - 1;

            if (weaponUnlocked[currentWeaponIndex])
            {
                SelectWeapon(currentWeaponIndex);
                return;
            }

        } while (currentWeaponIndex != startIndex);
    }

    void TrySelectWeapon(int index)
    {
        if (index >= weapons.Length)
        {
            return;
        }

        if (!weaponUnlocked[index])
        {
            return;
        }

        SelectWeapon(index);
    }

    void SelectWeapon(int index)
    {
        currentWeaponIndex = index;

        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].gameObject.SetActive(i == index);
        }

        if (index >= 0 && index < weapons.Length)
        {
            WeaponSelected?.Invoke(weapons[index], index);
        }
    }

    public void UnlockWeapon(int index)
    {
        if (index < 0 || index >= weaponUnlocked.Length)
            return;

        weaponUnlocked[index] = true;

        Debug.Log("Unlocked weapon: " + weapons[index].name);
    }
}
