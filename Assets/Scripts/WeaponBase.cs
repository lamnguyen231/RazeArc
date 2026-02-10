using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Stats")]
    public float damage = 25;
    public float fireRate = 0.5f;
    public float range = 100f;

    [Header("Recoil Settings")]
    public Vector3 recoilMin = new Vector3(-0.5f, 1f, 0f);
    public Vector3 recoilMax = new Vector3(0.5f, 2f, 0f);

    protected float lastShotTime = -999f;

    [Header("References")]
    public Transform firePoint;
    public CameraRecoil cameraRecoil;

    // Update is called once per frame
    void Update()
    {
        HandleInput();
    }

    protected virtual void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryFire();
        }
    }

    protected void TryFire()
    {
        if (Time.time >= lastShotTime + fireRate)
        {
            lastShotTime = Time.time;
            
            if (cameraRecoil != null)
            {
                Vector3 recoil = new Vector3(
                    Random.Range(recoilMin.x, recoilMax.x),
                    Random.Range(recoilMin.y, recoilMax.y),
                    0f
                );
                cameraRecoil.AddRecoil(recoil);
            }

            Fire();
        }
    }

    protected abstract void Fire();
}
