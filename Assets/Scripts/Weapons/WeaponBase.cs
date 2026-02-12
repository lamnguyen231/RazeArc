using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Stats")]
    public float damage = 25;
    public float fireRate = 0.5f;
    public float range = 100f;

    [Header("Ammo Settings")]
    public int magazineSize = 12;
    public int reserveAmmo = 36;
    public float reloadTime = 1.5f;

    [Header("Recoil Settings")]
    public Vector3 recoilMin = new Vector3(-0.5f, 1f, 0f);
    public Vector3 recoilMax = new Vector3(0.5f, 2f, 0f);

    [Header("References")]
    public Transform firePoint;
    public CameraRecoil cameraRecoil;

    protected float lastShotTime = -999f;
    protected int currentAmmo;
    protected bool isReloading = false;

    protected bool reloadCancelledThisFrame = false;
    protected Coroutine reloadCoroutine;

    protected virtual void Awake()
    {
    }

    protected virtual void Start()
    {
        currentAmmo = magazineSize;
    }

    void Update()
    {
        HandleInput();
    }

    protected virtual void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            reloadCoroutine = StartCoroutine(Reload());
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryFire();
        }
    }

    protected void TryFire()
    {
        if (isReloading)
        {
            CancelReload();
        }

        if (currentAmmo <= 0)
        {
            if (reserveAmmo > 0 && !reloadCancelledThisFrame)
            {
                reloadCoroutine = StartCoroutine(Reload());
            }
            else
            {
                Debug.Log("Click! (Dry Fire)");
            }

            reloadCancelledThisFrame = false;
            return;
        }

        if (Time.time >= lastShotTime + fireRate)
        {
            lastShotTime = Time.time;
            currentAmmo--;

            if (cameraRecoil != null)
            {
                Vector3 recoil = new Vector3(
                    Random.Range(recoilMin.x, recoilMax.x),
                    Random.Range(recoilMin.y, recoilMax.y),
                    0f
                );

                cameraRecoil.AddRecoil(recoil);
            }

            Fire();
        }
    }

    protected IEnumerator Reload()
    {
        if (currentAmmo == magazineSize)
        {
            yield break; // Magazine is already full
        }

        if (reserveAmmo <= 0)
        {
            yield break; // No reserve ammo to reload
        }

        isReloading = true;
        Debug.Log("Reloading...");

        yield return new WaitForSeconds(reloadTime);

        int ammoNeeded = magazineSize - currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToReload;
        reserveAmmo -= ammoToReload;

        isReloading = false;
        Debug.Log("Reloaded. Ammo: " + currentAmmo + "/" + reserveAmmo);
    }

    protected void CancelReload()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        isReloading = false;
        reloadCancelledThisFrame = true;

        Debug.Log("Reload Cancelled");
    }

    protected abstract void Fire();
}
