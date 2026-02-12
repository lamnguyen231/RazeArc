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
        damage = 10f;
        fireRate = 0.8f;
        range = 50f;

        magazineSize = 2;
        reserveAmmo = 20;
        reloadTime = 2.5f;

        recoilMin = new Vector3(-2f, -30f, 0f);
        recoilMax = new Vector3(2f, -40f, 0f);
    }

    protected override void Fire()
    {
        Debug.Log("Double Barrel Fired!");

        Camera camera = Camera.main;
        Ray centerRay = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 spreadDirection = GetSpreadDirection(centerRay.direction);

            Ray ray = new Ray(centerRay.origin, spreadDirection);
            RaycastHit hit;

            Debug.DrawRay(ray.origin, ray.direction * range, Color.yellow, 1f);

            if (Physics.Raycast(ray, out hit, range, shootMask))
            {
                if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage);
                }
            }
        }
    }

    Vector3 GetSpreadDirection(Vector3 baseDirection)
    {
        float randomYaw = Random.Range(-spreadAngle, spreadAngle);
        float randomPitch = Random.Range(-spreadAngle, spreadAngle);

        Quaternion spreadRotation = Quaternion.Euler(randomPitch, randomYaw, 0f);

        return spreadRotation * baseDirection;
    }
}
