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

        damage = 12f;
        fireRate = 0.08f;
        range = 80f;

        magazineSize = 30;
        reserveAmmo = 120;
        reloadTime = 2f;

        recoilMin = new Vector3(-0.3f, -20f, 0f);
        recoilMax = new Vector3(0.3f, -30f, 0f);
    }

    protected override void Fire()
    {
        Camera camera = Camera.main;

        Ray centerRay = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 spreadDirection = ApplySpread(centerRay.direction);
        Ray ray = new Ray(camera.transform.position, spreadDirection);

        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * range, Color.green, 0.3f);

        if (Physics.Raycast(ray, out hit, range, shootMask))
        {
            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage);
            }

            Debug.Log("Hit: " + hit.collider.name);
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
