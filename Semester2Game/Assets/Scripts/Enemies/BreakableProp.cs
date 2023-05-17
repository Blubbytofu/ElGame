using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableProp : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth;
    private int health;

    private bool dead;
    [SerializeField] private GameObject deathSpawn;
    [SerializeField] private float deathSpawnAliveTime;
    [SerializeField] private float explosionForce;
    [SerializeField] private float torqueForce;

    [SerializeField] private DropLoot dropLoot;

    [SerializeField] private bool willDespawn;
    [SerializeField] private float lifetime;

    public void ReceiveDamage(int damage)
    {
        health -= damage;
        if (health <= 0 && !dead)
        {
            dead = true;
            Destroy(gameObject);

            if (deathSpawn != null)
            {
                GameObject deathSpawnObj = Instantiate(deathSpawn, transform.position, transform.rotation);
                Rigidbody[] rb = deathSpawnObj.GetComponentsInChildren<Rigidbody>();
                foreach (Rigidbody rigidBody in rb)
                {
                    rigidBody.AddForce((transform.position - rigidBody.transform.position) * explosionForce, ForceMode.Impulse);
                    rigidBody.AddTorque(Vector3.up * torqueForce, ForceMode.Impulse);
                }

                Destroy(deathSpawnObj, deathSpawnAliveTime);
            }

            if (dropLoot != null)
            {
                dropLoot.InstantiateLoot();
            }
        }
    }

    public void Start()
    {
        health = maxHealth;

        if (willDespawn)
        {
            Destroy(gameObject, lifetime);
        }
    }
}
