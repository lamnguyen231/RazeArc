using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pistol : WeaponBase
{
    public LayerMask shootMask;

    protected override void Awake()
    {
        base.Awake();

        // Weapon Identity
        ammoType = AmmoType.Pistol;
        damage = 25f;
        fireRate = 0.4f;
        range = 100f;

        magazineSize = 12;
        reserveAmmo = 24;
        reloadTime = 1.5f;

        recoilMin = new Vector3(-1.25f, -25f, 0f);
        recoilMax = new Vector3(1.25f, -32f, 0f);

        kickbackAmount = 0.27f;
        kickReturnDuration = 0.12f;
        kickReturnExponent = 2.8f;
        recoilKickAngle = 6.8f;
        maxRecoilAngle = 18f;
        recoilAngleRecoverySpeed = 13.5f;
        fovKickAmount = 0.55f;

        useReloadAnimation = true;
        reloadRaiseFraction = 0.06f;

        useTracer = true;
        tracerEveryNthShot = 1;
        tracerDuration = 0.08f;
        tracerWidth = 0.07f;

        useMuzzleFlash = true;
        muzzleFlashParticleCount = 10;
        muzzleFlashDuration = 0.04f;
        muzzleFlashSize = 0.14f;
        muzzleFlashSpeed = 7f;
        muzzleFlashLightIntensity = 2f;
        muzzleFlashLightRange = 1.8f;
    }

    protected override void Fire()
    {
        Debug.Log("Pistol Fired");
        Camera camera = Camera.main;
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 tracerStart = GetTracerStartPosition(ray);

        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * range, Color.red, 0.5f);


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
            Debug.Log("Missed!");
        }
    }
}
