using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pistol : WeaponBase
{
    public LayerMask shootMask;

    protected override void Fire()
    {
        Debug.Log("Pistol Fired");

        RaycastHit hit;
        Vector3 origin = firePoint.position;
        Vector3 direction = firePoint.forward;

        Debug.DrawRay(origin, direction * range, Color.red, 0.5f);

        if (Physics.Raycast(origin, direction, out hit, range, shootMask))
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
