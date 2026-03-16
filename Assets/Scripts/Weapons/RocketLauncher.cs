using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketLauncher : WeaponBase
{
    [Header("Rocket Settings")]
    public GameObject rocketPrefab;
    public float rocketSpeed = 30f;

    protected override void Awake()
    {
        base.Awake();

        // Weapon identity
        isAutomatic = false;
        ammoType = AmmoType.Rocket;

        damage = 100f;
        fireRate = 1.2f;
        range = 200f;

        magazineSize = 1;
        reserveAmmo = 2;
        reloadTime = 2.5f;

        recoilMin = new Vector3(-2f, -100f, 0f);
        recoilMax = new Vector3(2f, -120f, 0f);

        kickbackAmount = 0.4f;
        kickReturnDuration = 0.18f;
        kickReturnExponent = 3.1f;
        recoilKickAngle = 11f;
        maxRecoilAngle = 30f;
        recoilAngleRecoverySpeed = 12f;
        fovKickAmount = 1.2f;

        useReloadAnimation = true;
        reloadTiltEuler = new Vector3(38f, 0f, 0f);
        reloadPositionOffset = new Vector3(0f, -0.1f, -0.16f);
        reloadLowerFraction = 0.26f;
        reloadRaiseFraction = 0.1f;

        useMuzzleFlash = true;
        muzzleFlashParticleCount = 24;
        muzzleFlashDuration = 0.07f;
        muzzleFlashSize = 0.34f;
        muzzleFlashSpeed = 11f;
        muzzleFlashLightIntensity = 4.5f;
        muzzleFlashLightRange = 3.8f;

        // Lift and push muzzle FX to the launcher tube opening.
        muzzleLocalOffset = new Vector3(-0.1f, 0.08f, -0.6f);
    }

    protected override void Fire()
    {
        Camera camera = Camera.main;
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 direction = ray.direction;
        Vector3 muzzlePosition = GetGunMuzzlePosition();

        Debug.Log("Rocket spawn: " + muzzlePosition);

        GameObject rocket = Instantiate(
            rocketPrefab,
            muzzlePosition,
            Quaternion.LookRotation(direction)
        );

        RocketProjectile projectile = rocket.GetComponent<RocketProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(direction, rocketSpeed, damage);
        }
    }
}
