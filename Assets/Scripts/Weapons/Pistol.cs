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
        damage = 25f;
        fireRate = 0.4f;
        range = 100f;

        magazineSize = 12;
        reserveAmmo = 36;
        reloadTime = 1.5f;

        recoilMin = new Vector3(-0.75f, -18f, 0f);
        recoilMax = new Vector3(0.75f, -24f, 0f);

        kickbackAmount = 0.19f;
        recoilKickAngle = 4.4f;
        maxRecoilAngle = 14f;
        recoilAngleRecoverySpeed = 15f;

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
