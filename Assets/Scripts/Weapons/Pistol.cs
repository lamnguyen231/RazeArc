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

        recoilMin = new Vector3(-0.5f, -15f, 0f);
        recoilMax = new Vector3(0.5f, -20f, 0f);
    }

    protected override void Fire()
    {
        Debug.Log("Pistol Fired");
        Camera camera = Camera.main;
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * range, Color.red, 0.5f);


        if (Physics.Raycast(ray, out hit, range, shootMask))
        {
            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage);
            }

            Debug.Log("Hit: " + hit.collider.name);
        }
        else
        {
            Debug.Log("Missed!");
        }
    }
}
