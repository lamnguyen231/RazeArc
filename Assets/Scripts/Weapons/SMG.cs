using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMG : WeaponBase
{
    public LayerMask shootMask;

    protected override void Awake()
    {
        base.Awake();

        // Weapon Identity
        isAutomatic = true;
        ammoType = AmmoType.SMG;

        damage = 12f;
        fireRate = 0.08f;
        range = 80f;

        magazineSize = 30;
        reserveAmmo = 60;
        reloadTime = 2f;

        recoilMin = new Vector3(-0.3f, -20f, 0f);
        recoilMax = new Vector3(0.3f, -30f, 0f);

        kickbackAmount = 0.15f;
        recoilKickAngle = 1.4f;
        maxRecoilAngle = 5.5f;
        recoilAngleRecoverySpeed = 24f;

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
        float spread = 1.5f;
        float yaw = Random.Range(-spread, spread);
        float pitch = Random.Range(-spread, spread);

        return Quaternion.Euler(pitch, yaw, 0f) * baseDirection;
    }
}
