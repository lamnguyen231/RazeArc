using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Melee : WeaponBase
{
    [Header("Melee Settings")]
    public float meleeRange = 2f;
    public float meleeRadius = 0.5f;
    public LayerMask hitMask;

    protected override void Awake()
    {
        base.Awake();

        damage = 50;
        fireRate = 0.7f;
        range = 2f;
        usesAmmo = false;

        recoilMin = new Vector3(-0.2f, -3f, 0);
        recoilMax = new Vector3(0.2f, -5f, 0);

        motionType = WeaponMotionType.Melee;
        swingAngle = 80f;
        swingSpeed = 15f;
    }

    protected override void Fire()
    {
        Vector3 origin = firePoint.position;
        Vector3 direction = firePoint.forward;

        RaycastHit hit;
        if (Physics.SphereCast(origin, meleeRadius, direction, out hit, meleeRange, hitMask))
        {
            Debug.Log("Melee hit: " + hit.collider.name);
            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position + firePoint.forward * meleeRange, meleeRadius);
        }
    }
}
