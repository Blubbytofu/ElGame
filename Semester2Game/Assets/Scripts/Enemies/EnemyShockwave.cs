using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerObject;

public class EnemyShockwave : MonoBehaviour
{
    [SerializeField] private int damage;
    [SerializeField] private int knockbackForce;
    [SerializeField] private float growSpeed;
    [SerializeField] private float sizeError;
    private bool appliedDamage;

    [SerializeField] private float startingWidth;
    [SerializeField] private float startingHeight;
    [SerializeField] private float endingWidth;
    [SerializeField] private float endingHeight;

    private void Start()
    {
        transform.localScale = new Vector3(startingWidth, startingHeight, startingWidth);
    }

    private void Update()
    {
        while(transform.localScale.x < endingWidth - sizeError)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(endingWidth, endingHeight, endingWidth), growSpeed * Time.deltaTime);
            return;
        }

        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        PlayerInventory playerInventory = collision.gameObject.GetComponent<PlayerInventory>();
        if (playerInventory != null && !appliedDamage)
        {
            appliedDamage = true;
            collision.gameObject.GetComponent<Rigidbody>().AddForce(knockbackForce * Vector3.up, ForceMode.Impulse);
            Physics.IgnoreCollision(collision.gameObject.GetComponent<Collider>(), gameObject.GetComponent<Collider>());
            playerInventory.TakeDamage(damage);
        }
    }
}
