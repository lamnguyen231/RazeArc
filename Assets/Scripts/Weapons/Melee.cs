using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Melee : WeaponBase
{
    [Header("Melee Settings")]
    public float meleeRange = 2f;
    public float meleeRadius = 0.5f;
    public LayerMask hitMask;

    [Header("Melee Audio")]
    public AudioClip slashClip;
    [Range(0.3f, 1f)]
    public float slashVolume = 0.8f;

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
        meleeWindupEuler = new Vector3(0f, 0f, 30f);
        meleeSwingEuler = new Vector3(0f, 0f, -110f);
        meleeWindupTime = 0.04f;
        meleeAttackTime = 0.045f;
        meleeRecoverTime = 0.12f;
        meleeLungeDistance = 0.2f;
    }

    protected override void Fire()
    {
        Transform attackSource = firePoint != null ? firePoint : transform;
        Vector3 attackCenter = attackSource.position + attackSource.forward * meleeRange;

        Collider[] hits = Physics.OverlapSphere(
            attackCenter,
            meleeRadius,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        bool hitSomething = false;
    PlaySlashSound();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            IDamageable damageable =
                hit.GetComponent<IDamageable>()
                ?? hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
            {
                continue;
            }

            if (damagedTargets.Add(damageable))
            {
                damageable.TakeDamage(damage);
                hitSomething = true;
                Debug.Log("Melee hit: " + hit.name);
            }
        }

        if (!hitSomething)
        {
            Debug.Log("Melee missed.");
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

    void PlaySlashSound()
    {
        if (slashClip != null)
        {
            AudioSource source = GetComponent<AudioSource>();
            if (source != null)
            {
                source.PlayOneShot(slashClip, slashVolume);
            }
        }
    }
}
