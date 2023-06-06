using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerObject;

public class AngronShield : MonoBehaviour
{
    [SerializeField] private int damage;
    [SerializeField] private int knockbackForce;

    private void OnTriggerEnter(Collider collision)
    {
        PlayerInventory playerInventory = collision.gameObject.GetComponent<PlayerInventory>();
        if (playerInventory != null)
        {
            playerInventory.gameObject.GetComponent<Rigidbody>().AddForce(knockbackForce * (transform.forward + Vector3.up).normalized, ForceMode.Impulse);
            playerInventory.TakeDamage(damage);
        }
    }
}
