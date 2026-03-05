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

        recoilMin = new Vector3(-0.3f, -10f, 0f);
        recoilMax = new Vector3(0.3f, -15f, 0f);
    }

    protected override void Fire()
    {
        Camera camera = Camera.main;
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

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
}
