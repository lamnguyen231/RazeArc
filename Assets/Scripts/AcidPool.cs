using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcidPool : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damagePerTick = 10.0f;
    public float tickRate = 1.0f;

    private float timer;
    // Start is called before the first frame update
    void Start()
    {
        timer = tickRate;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            timer += Time.deltaTime;

            if (timer >= tickRate)
            {
                    PlayerInventory inventory = other.GetComponent<PlayerInventory>();

                if (inventory != null)
                {
                    inventory.TakeDamage(damagePerTick);
                    timer = 0f;
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            timer = tickRate;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}
