using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Behavior")]
    public bool usesAmmo = true;

    [Header("Weapon Stats")]
    public float damage = 25;
    public float fireRate = 0.5f;
    public float range = 100f;
    public bool isAutomatic = false;

    [Header("Ammo Settings")]
    public int magazineSize = 12;
    public int reserveAmmo = 36;
    public float reloadTime = 1.5f;

    [Header("Recoil Settings")]
    public Vector3 recoilMin = new Vector3(-0.5f, -15f, 0f);
    public Vector3 recoilMax = new Vector3(0.5f, -20f, 0f);

    [Header("Motion Type")]
    public WeaponMotionType motionType = WeaponMotionType.Gun;

    [Header("Gun Kickback")]
    public float kickbackAmount = 0.15f;
    public float kickbackRecoverySpeed = 8f;
    Vector3 originalLocalPosition;

    [Header("Melee Swing")]
    public float swingAngle = 60f;
    public float swingSpeed = 12f;
    Quaternion originalLocalRotation;
    bool isSwinging = false;

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
        if (usesAmmo)
        {
            currentAmmo = magazineSize;
        }

        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
    }

    void Update()
    {
        HandleInput();

        if (motionType == WeaponMotionType.Gun)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                originalLocalPosition,
                kickbackRecoverySpeed * Time.deltaTime
            );
        }
    }

    protected virtual void HandleInput()
    {
        if (usesAmmo && Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            reloadCoroutine = StartCoroutine(Reload());
            return;
        }

        if (isAutomatic)
        {
            if (Input.GetMouseButton(0))
            {
                TryFire();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                TryFire();
            }
        }
    }

    protected void TryFire()
    {
        if (isReloading)
        {
            CancelReload();
        }

        if (usesAmmo && currentAmmo <= 0)
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
            if (usesAmmo)
            {
                currentAmmo--;
            }

            // Weapon attack animation
            if (motionType == WeaponMotionType.Gun)
            {
                if (cameraRecoil != null)
                {
                    Vector3 recoil = new Vector3(
                        Random.Range(recoilMin.x, recoilMax.x),
                        Random.Range(recoilMin.y, recoilMax.y),
                        0f
                    );

                    cameraRecoil.AddRecoil(recoil);
                }

                transform.localPosition -= Vector3.forward * kickbackAmount;
            }
            else if (motionType == WeaponMotionType.Melee)
            {
                if (!isSwinging)
                {
                    StartCoroutine(PerformSwing());
                }
            }


            Fire();
        }
    }

    protected IEnumerator Reload()
    {
        if (!usesAmmo)
        {
            yield break; // This weapon doesn't use ammo
        }

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

    IEnumerator PerformSwing()
    {
        isSwinging = true;

        Quaternion startRot = originalLocalRotation;

        // downward diagonal slash
        Quaternion swingRot = (
            originalLocalRotation *
            Quaternion.Euler(-90f, -5f, 15f)
        );

        float attackTime = 0.08f;
        float recoverTime = 0.18f;

        float t = 0;

        // ATTACK (fast)
        while (t < attackTime)
        {
            t += Time.deltaTime;
            float progress = t / attackTime;

            transform.localPosition =
                originalLocalPosition + Vector3.forward * 0.12f;

            transform.localRotation =
                Quaternion.Slerp(startRot, swingRot, progress);

            yield return null;
        }

        t = 0;

        // RECOVERY (slower)
        while (t < recoverTime)
        {
            t += Time.deltaTime;
            float progress = t / recoverTime;

            transform.localPosition = originalLocalPosition;

            transform.localRotation =
                Quaternion.Slerp(swingRot, startRot, progress);

            yield return null;
        }

        transform.localRotation = startRot;
        isSwinging = false;
    }

    protected abstract void Fire();
}
