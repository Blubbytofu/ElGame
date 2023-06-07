using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerObject;

public class AngronBall : MonoBehaviour, IDamageable
{
    [SerializeField] private int damage;
    [SerializeField] private float lifetime;

    public void ReceiveDamage(int damage)
    {
        
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        PlayerInventory inv = collision.gameObject.GetComponent<PlayerInventory>();
        if (inv != null)
        {
            inv.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
