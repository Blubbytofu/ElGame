using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerObject;

public class GrenadePrinterGrenade : MonoBehaviour
{
    [SerializeField] private GameObject detonationEffect;

    [SerializeField] private int directDamage;
    [SerializeField] private int indirectDamage;
    [SerializeField] private int playerDamage;
    [SerializeField] private int playerLaunchPower;

    [SerializeField] private float explosionRadius;
    //public float explosionLifetime;

    private bool hasTakenDirect;

    private void Start()
    {
        Destroy(gameObject, 10);
    }

    private void Explode()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider collider in colliders)
        {
            IDamageable damageable = collider.GetComponent<IDamageable>();
            PlayerInventory playerInventory = collider.GetComponent<PlayerInventory>();
            if (damageable != null)
            {
                damageable.ReceiveDamage(indirectDamage);
            }

            if (playerInventory != null)
            {
                playerInventory.TakeDamage(playerDamage);
                playerInventory.gameObject.GetComponent<Rigidbody>().AddForce(playerLaunchPower * Vector3.up, ForceMode.Impulse);
            }
        }

        //GameObject detEffect = 
        Instantiate(detonationEffect, transform.position, Quaternion.identity);
        //Destroy(detEffect, explosionLifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null && !hasTakenDirect)
        {
            hasTakenDirect = true;
            damageable.ReceiveDamage(directDamage);
        }
        Destroy(gameObject);
        Explode();
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
