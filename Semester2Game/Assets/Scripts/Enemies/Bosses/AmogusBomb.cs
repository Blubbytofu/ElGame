using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerObject;

public class AmogusBomb : MonoBehaviour, IDamageable
{
    [SerializeField] private SphereCollider bombCollider;
    [SerializeField] private GameObject bomblet;
    [SerializeField] private GameObject detonationEffect;

    [SerializeField] private int directDamage;
    [SerializeField] private float minLifetime, maxLifetime;
    [SerializeField] private int bomblets;

    private void Start()
    {
        Invoke(nameof(Detonate), Random.Range(minLifetime, maxLifetime));
    }

    private void Detonate()
    {
        float angle = 0;
        float direction = 0;
        int rand = Random.Range(0, 3);
        if (rand == 0)
        {
            direction = -90;
        }
        else if (rand == 1)
        {
            direction = 0;
        }
        else
        {
            direction = 90;
        }

        for (int i = 0; i < bomblets; i++)
        {
            BombletAmogus bombletAmogus = Instantiate(bomblet, transform.position, Quaternion.Euler(0, angle, 0)).GetComponent<BombletAmogus>();
            bombletAmogus.rotationRate = direction;
            //rb.AddForce(bombletForce * rb.gameObject.transform.forward, ForceMode.Impulse);
            angle += 360 / bomblets;
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, 3f);
        foreach (Collider collider in colliders)
        {
            PlayerInventory playerInventory = collider.gameObject.GetComponent<PlayerInventory>();
            if (playerInventory != null)
            {
                playerInventory.TakeDamage(directDamage);
                playerInventory.gameObject.GetComponent<Rigidbody>().AddForce(1200 * Vector3.up, ForceMode.Impulse);
            }
        }

        GameObject detEffect = Instantiate(detonationEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        PlayerInventory inventory = collision.gameObject.GetComponent<PlayerInventory>();
        //IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();

        if (inventory != null)
        {
            inventory.TakeDamage(directDamage);
            Destroy(gameObject);
            Detonate();
        }
    }

    public void ReceiveDamage(int damage)
    {

    }
}
