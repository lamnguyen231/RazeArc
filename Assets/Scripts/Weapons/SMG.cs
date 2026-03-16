using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMG : WeaponBase
{
    public LayerMask shootMask;

    [Header("Dynamic Spread")]
    public float baseSpread = 0.75f;
    public float maxSpread = 5.25f;
    public float spreadIncreasePerShot = 0.45f;
    public float spreadRecoverPerSecond = 3.8f;

    float currentSpread;
    float lastSpreadUpdateTime;

    protected override void Awake()
    {
        base.Awake();

        // Weapon Identity
        isAutomatic = true;
        ammoType = AmmoType.SMG;

        damage = 16f;
        fireRate = 0.045f;
        range = 80f;

        magazineSize = 30;
        reserveAmmo = 60;
        reloadTime = 1.7f;

        recoilMin = new Vector3(-0.15f, -10f, 0f);
        recoilMax = new Vector3(0.15f, -15f, 0f);

        kickbackAmount = 0.08f;
        kickReturnDuration = 0.09f;
        kickReturnExponent = 2f;
        recoilKickAngle = 0.7f;
        maxRecoilAngle = 2.8f;
        recoilAngleRecoverySpeed = 24f;
        fovKickAmount = 0.06f;

        useReloadAnimation = true;
        reloadRaiseFraction = 0.055f;

        useTracer = true;
        tracerEveryNthShot = 2;
        tracerDuration = 0.065f;
        tracerWidth = 0.06f;

        useMuzzleFlash = true;
        muzzleFlashParticleCount = 8;
        muzzleFlashDuration = 0.03f;
        muzzleFlashSize = 0.11f;
        muzzleFlashSpeed = 9f;
        muzzleFlashLightIntensity = 1.4f;
        muzzleFlashLightRange = 1.4f;

        fireScreenShakeAmount = 0.06f;
        fireScreenShakeDuration = 0.04f;

        currentSpread = baseSpread;
        lastSpreadUpdateTime = Time.time;
    }

    protected override void Fire()
    {
        Camera camera = Camera.main;

        Ray centerRay = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 tracerStart = GetTracerStartPosition(centerRay);
        Vector3 spreadDirection = ApplySpread(centerRay.direction);
        Ray ray = new Ray(camera.transform.position, spreadDirection);

        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * range, Color.green, 0.3f);

        if (Physics.Raycast(ray, out hit, range, shootMask))
        {
            SpawnTracer(tracerStart, hit.point);
            SpawnImpactDecal(hit);

            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage);
            }

            Debug.Log("Hit: " + hit.collider.name);
        }
        else
        {
            SpawnTracer(tracerStart, ray.origin + ray.direction * range);
        }
    }

    Vector3 ApplySpread(Vector3 baseDirection)
    {
        float now = Time.time;
        float deltaTime = Mathf.Max(0f, now - lastSpreadUpdateTime);
        lastSpreadUpdateTime = now;

        currentSpread = Mathf.Max(
            baseSpread,
            currentSpread - (spreadRecoverPerSecond * deltaTime)
        );

        // Apply crouch accuracy bonus
        float spreadMultiplier = 1f;
        if (playerMovement != null && playerMovement.IsCrouching())
        {
            spreadMultiplier = crouchAccuracyMultiplier;
        }

        float spread = Mathf.Clamp(currentSpread * spreadMultiplier, baseSpread * spreadMultiplier, maxSpread * spreadMultiplier);
        float yaw = Random.Range(-spread, spread);
        float pitch = Random.Range(-spread, spread);

        currentSpread = Mathf.Clamp(
            currentSpread + spreadIncreasePerShot,
            baseSpread,
            maxSpread
        );

        return Quaternion.Euler(pitch, yaw, 0f) * baseDirection;
    }
}
