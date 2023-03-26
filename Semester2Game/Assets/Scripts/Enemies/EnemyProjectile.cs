using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerObject;

public class EnemyProjectile : MonoBehaviour, IDamageable
{
    [SerializeField] private int damage;

    public void ReceiveDamage(int damage)
    {

    }

    private void OnCollisionEnter(Collision collision)
    {
        PlayerInventory playerInventory = collision.gameObject.GetComponent<PlayerInventory>();
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        IConsumable consumable = collision.gameObject.GetComponent<IConsumable>();

        if (playerInventory != null)
        {
            playerInventory.TakeDamage(damage);
            Destroy(gameObject);
        }

        if (damageable == null && consumable == null)
        {
            Destroy(gameObject);
        }
        else if (damageable != null)
        {
            damageable.ReceiveDamage(damage);
            Destroy(gameObject);
        }
    }
}
