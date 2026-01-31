using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PistolShoot : MonoBehaviour
{
    float lastShotTime = -999f;
    public float fireRate = 0.3f; // Time in seconds between shots
    public float damage = 25f;

    public LayerMask shootMask;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time - lastShotTime >= fireRate)
            {
                Debug.Log("Bang!");
                lastShotTime = Time.time;

                RaycastHit hit;
                if (Physics.Raycast(transform.position, transform.forward, out hit, 100f, shootMask))
                {
                    Debug.DrawRay(transform.position, transform.forward * 100f, Color.red, 0.5f);

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
    }
}
