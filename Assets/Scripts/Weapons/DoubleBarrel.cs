using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleBarrel : WeaponBase
{
    [Header("Shotgun Settings")]
    public int pelletCount = 10;
    public float spreadAngle = 5f;
    public LayerMask shootMask;

    protected override void Awake()
    {
        base.Awake();

        // Weapon identity
        ammoType = AmmoType.Shell;
        damage = 10f;
        fireRate = 0.8f;
        range = 50f;

        magazineSize = 2;
        reserveAmmo = 10;
        reloadTime = 2.5f;

        recoilMin = new Vector3(-4f, -60f, 0f);
        recoilMax = new Vector3(4f, -80f, 0f);

        kickbackAmount = 0.48f;
        recoilKickAngle = 13f;
        maxRecoilAngle = 40f;
        kickbackRecoverySpeed = 5.5f;
        recoilAngleRecoverySpeed = 9f;

        useTracer = true;
        tracerEveryNthShot = 1;
        tracerDuration = 0.11f;
        tracerWidth = 0.11f;

        useMuzzleFlash = true;
        muzzleFlashParticleCount = 20;
        muzzleFlashDuration = 0.06f;
        muzzleFlashSize = 0.28f;
        muzzleFlashSpeed = 10f;
        muzzleFlashLightIntensity = 3.8f;
        muzzleFlashLightRange = 3.2f;

        // Push muzzle FX forward to the shotgun barrel tip.
        muzzleLocalOffset = new Vector3(0f, 0.02f, 0.9f);
    }

    protected override void Fire()
    {
        Debug.Log("Double Barrel Fired!");

        Camera camera = Camera.main;
        Ray centerRay = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 tracerStart = GetTracerStartPosition(centerRay);
        Vector3 tracerEndPoint = centerRay.origin + centerRay.direction * range;
        bool hitSomething = false;

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 spreadDirection = GetSpreadDirection(centerRay.direction);

            Ray ray = new Ray(centerRay.origin, spreadDirection);
            RaycastHit hit;

            Debug.DrawRay(ray.origin, ray.direction * range, Color.yellow, 1f);

            if (Physics.Raycast(ray, out hit, range, shootMask))
            {
                if (!hitSomething)
                {
                    tracerEndPoint = hit.point;
                    hitSomething = true;
                }

                if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage);
                }
            }
        }

        SpawnTracer(tracerStart, tracerEndPoint);
    }

    Vector3 GetSpreadDirection(Vector3 baseDirection)
    {
        float randomYaw = Random.Range(-spreadAngle, spreadAngle);
        float randomPitch = Random.Range(-spreadAngle, spreadAngle);

        Quaternion spreadRotation = Quaternion.Euler(randomPitch, randomYaw, 0f);

        return spreadRotation * baseDirection;
    }
}
