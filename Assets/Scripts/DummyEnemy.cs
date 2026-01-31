using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyEnemy : MonoBehaviour, IDamageable
{
    public float health = 100f;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log($"DummyEnemy took {amount} damage, remaining health: {health}");

        if (health <= 0)
        {
            Debug.Log("DummyEnemy defeated!");
            Destroy(gameObject);
        }
    }
}
