using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerObject;

public class AmogusBeam : MonoBehaviour, IDamageable
{
    [SerializeField] private LayerMask environmentMask;
    [SerializeField] private GameObject player;
    [SerializeField] private CapsuleCollider capsuleCollider;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameObject indicator;

    [SerializeField] private int radiusGrowSpeed;
    [SerializeField] private int radiusShrinkSpeed;
    [SerializeField] private float maxRadius;
    private float radius;
    private int currentDamage;
    [SerializeField] private int maxDamage;
    [SerializeField] private float speed;
    [SerializeField] private float maxedTime;
    [SerializeField] private float lifetime;
    [SerializeField] private float indicatorLifetime;
    [SerializeField] private float startMoveTime;

    private bool move;
    private bool hasMaxed;

    private void Start()
    {
        radius = 0;
        player = GameObject.Find("Body");
        GameObject indicate = Instantiate(indicator, new Vector3(transform.position.x, -13f, transform.position.z), Quaternion.identity);
        Destroy(indicate, indicatorLifetime);
    }

    private void Update()
    {
        transform.Rotate(0, 720 * Time.deltaTime, 0);

        if (radius < 2f && !hasMaxed)
        {
            currentDamage = 0;
            radius = Mathf.Lerp(radius, radius + 0.1f, radiusGrowSpeed * Time.deltaTime);
        }
        else if (radius < maxRadius - 0.1f && !hasMaxed)
        {
            currentDamage = maxDamage;
            radius = Mathf.Lerp(radius, maxRadius, 50 * Time.deltaTime);
        }
        else
        {
            Invoke(nameof(Max), maxedTime);
            if (hasMaxed)
            {
                radius = Mathf.Lerp(radius, 0, radiusShrinkSpeed * Time.deltaTime);
                currentDamage = 0;
                Destroy(gameObject, lifetime - maxedTime);
            }
        }
        transform.localScale = new Vector3(radius, 15f, radius);
    }

    private void Max()
    {
        hasMaxed = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInventory playerInventory = other.gameObject.GetComponent<PlayerInventory>();
        IDamageable damageable = other.gameObject.GetComponent<IDamageable>();
        if (playerInventory != null)
        {
            playerInventory.TakeDamage(currentDamage);
            //launch the player into the sky
            if (currentDamage > 0)
            {
                playerInventory.gameObject.GetComponent<Rigidbody>().AddForce(Vector3.up * 4000, ForceMode.Impulse);
            }
            currentDamage = 0;
        }
    }

    public void ReceiveDamage(int damage)
    {

    }
}
