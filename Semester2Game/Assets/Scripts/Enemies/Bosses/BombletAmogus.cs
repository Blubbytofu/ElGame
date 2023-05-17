using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerObject;

public class BombletAmogus : MonoBehaviour, IDamageable
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private int damage;
    [SerializeField] private float lifetime;
    [HideInInspector] public float rotationRate;

    public void ReceiveDamage(int damage)
    {

    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Rotate(0, rotationRate * Time.deltaTime, 0);
    }

    private void FixedUpdate()
    {
        rb.AddForce(transform.forward * 10, ForceMode.Acceleration);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInventory playerInventory = other.gameObject.GetComponent<PlayerInventory>();
        if (playerInventory != null)
        {
            playerInventory.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
