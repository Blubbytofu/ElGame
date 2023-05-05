using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ExtensionMethods;
using UnityEngine.UI;
using TMPro;
using PlayerObject;

public class RigidbodyBoss : MonoBehaviour, IDamageable
{
    public Slider bar;
    public TextMeshProUGUI healthNumber;

    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private DropLoot dropLoot;
    [SerializeField] private GameObject player;
    //[SerializeField] private GameManager gameManager;
    [SerializeField] private LayerMask groundMask, playerMask;
    [SerializeField] private GameObject spawnEffect;
    [SerializeField] private Collider[] childHitBoxes;
    [SerializeField] private Rigidbody bossRb;

    private int health;
    [SerializeField] private int maxHealth;

    [SerializeField] private float firstSpawnDelay;

    private Vector3 walkPoint;
    private bool walkPointSet;
    [SerializeField] private float walkPointStrafeRange, walkPointForwardRange;

    [SerializeField] private float doorRange;
    [SerializeField] private float atPointRange;

    private bool alreadyAttacked;

    [SerializeField] private float sightRange, attackRange;
    private bool playerInSightRange, playerInAttackRange;

    private bool isDead;

    private bool rigidbodyMode;

    [SerializeField] private GameObject shockwave;
    [SerializeField] private Transform highShockwavePoint;
    [SerializeField] private Transform lowShockwavePoint;
    [SerializeField] private int shockwaveIterations;
    [SerializeField] private float shockWaveFinalDelay;

    [SerializeField] private GameObject projectile;
    [SerializeField] private int projectileVel;
    [SerializeField] private int bulletsPerShot;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private int dashIterations;
    [SerializeField] private float dashForce;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashDelay;
    [SerializeField] private float dashFinalDelay;

    [SerializeField] private GameObject slowShockwave;
    [SerializeField] private int chargeIterations;

    private int lastAttackIndex;

    private void Awake()
    {
        health = maxHealth;
        bar.maxValue = maxHealth;

        if (spawnEffect != null)
        {
            Instantiate(spawnEffect, transform.position, Quaternion.identity);
        }

        player = GameObject.Find("Player");
        //gameManager = GameObject.Find("Game Manager").GetComponent<GameManager>();
        //CheckDoor();

        alreadyAttacked = true;
        StartCoroutine(ResetAttack(firstSpawnDelay));
    }

    private IEnumerator CheckDoor()
    {
        while (!isDead)
        {
            Collider[] possibleDoor = Physics.OverlapSphere(transform.position, doorRange, groundMask);
            foreach (Collider obj in possibleDoor)
            {
                if (obj.CompareTag("Door"))
                {
                    IInteractable interactable = obj.GetComponent<IInteractable>();
                    if (interactable != null)
                    {
                        interactable.Interact(gameObject);
                    }
                }
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    private void ActivateRbMode()
    {
        agent.SetDestination(transform.position);
        rigidbodyMode = true;
        agent.enabled = false;
        bossRb.isKinematic = false;
        bossRb.useGravity = true;
    }

    private void DeactivateRbMode()
    {
        rigidbodyMode = false;
        agent.enabled = true;
        bossRb.isKinematic = true;
        bossRb.useGravity = false;
    }

    private void Update()
    {
        bar.value = health;
        healthNumber.text = health + " / " + maxHealth;

        if (!rigidbodyMode && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.SetDestination(player.transform.position);
        }

        RollAttack();

        transform.LookAt(player.transform.position.ReplaceField(newY: transform.position.y));
    }

    private void RollAttack()
    {
        if (!alreadyAttacked)
        {
            alreadyAttacked = true;

            int randNum = Random.Range(0, 3);

            while (randNum == lastAttackIndex)
            {
                randNum = Random.Range(0, 3);
            }

            if (randNum == 0)
            {
                StartCoroutine(Charge());
            }
            else if (randNum == 1)
            {
                StartCoroutine(GroundPound());
            }
            else 
            {
                StartCoroutine(DashShoot());
            }

            lastAttackIndex = randNum;
        }
    }

    private void Patrol()
    {
        if (walkPointSet)
        {
            agent.SetDestination(walkPoint);

            if (!agent.hasPath)
            {
                walkPointSet = false;
            }
        }
        else
        {
            SearchWalkPoint();
        }

        Vector3 distanceToWalkPoint = transform.position - walkPoint;
        if (distanceToWalkPoint.magnitude < atPointRange)
        {
            walkPointSet = false;
        }
    }

    private void SearchWalkPoint()
    {
        Vector3 xPos = Vector3.Cross(player.transform.position.ReplaceField(newY: transform.position.y) - transform.position, Vector3.up).normalized * Random.Range(-walkPointStrafeRange, walkPointStrafeRange);
        Vector3 zPos = (player.transform.position.ReplaceField(newY: transform.position.y) - transform.position).normalized * Random.Range(walkPointForwardRange - 1, walkPointForwardRange);
        walkPoint = transform.position + xPos + zPos;

        if (Physics.Raycast(walkPoint, -transform.up, 2f, groundMask))
        {
            walkPointSet = true;
        }
    }

    private void Chase()
    {
        agent.SetDestination(player.transform.position);
    }

    private IEnumerator GroundPound()
    {
        ActivateRbMode();

        for (int i = 0; i < shockwaveIterations; i++)
        {
            bossRb.AddForce(25f * transform.up, ForceMode.Impulse);
            yield return new WaitForSeconds(0.75f);
            bossRb.velocity = Vector3.zero;
            bossRb.AddForce(-60f * transform.up, ForceMode.Impulse);
            yield return new WaitForSeconds(0.4f);
            SpawnShockwave();
            yield return new WaitForSeconds(0.1f);
        }

        DeactivateRbMode();
        StartCoroutine(ResetAttack(shockWaveFinalDelay));
    }

    private void SpawnShockwave()
    {
        Vector3 spawnPoint;
        if (Random.Range(0, 2) == 0)
        {
            spawnPoint = lowShockwavePoint.position;
        }
        else
        {
            spawnPoint = highShockwavePoint.position;
        }

        GameObject wave = Instantiate(shockwave, spawnPoint, Quaternion.identity);
        foreach (Collider collider in childHitBoxes)
        {
            Physics.IgnoreCollision(wave.GetComponent<Collider>(), collider, true);
        }
    }
    
    private IEnumerator Charge()
    {
        ActivateRbMode();

        for (int i = 0; i < chargeIterations; i++)
        {
            Vector3 finalPosition = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
            yield return new WaitForSeconds(0.25f);
            bossRb.AddForce(50 * (finalPosition - transform.position).normalized, ForceMode.Impulse);
            yield return new WaitForSeconds(0.25f);
            bossRb.velocity = Vector3.zero;

            GameObject wave = Instantiate(slowShockwave, attackPoint.position, Quaternion.LookRotation(transform.forward, Vector3.Cross((player.transform.position.ReplaceField(newY: transform.position.y) - transform.position).normalized, Vector3.up)));
            foreach (Collider collider in childHitBoxes)
            {
                Physics.IgnoreCollision(wave.GetComponent<Collider>(), collider, true);
            }

            yield return new WaitForSeconds(0.25f);
        }

        DeactivateRbMode();
        StartCoroutine(ResetAttack(0.5f));
    }

    private IEnumerator DashShoot()
    {
        ActivateRbMode();

        for (int i = 0; i < dashIterations; i++)
        {
            int randNum;
            if (Random.Range(0, 2) == 0)
            {
                randNum = -2;
            }
            else
            {
                randNum = 2;
            }

            bossRb.AddForce(dashForce * ((randNum * transform.right) + transform.forward).normalized, ForceMode.Impulse);
            yield return new WaitForSeconds(dashTime);
            bossRb.velocity = Vector3.zero;
            yield return new WaitForSeconds(0.1f);
            SpawnProjectile();
            yield return new WaitForSeconds(dashDelay);
        }

        DeactivateRbMode();
        StartCoroutine(ResetAttack(dashFinalDelay));
    }

    private void SpawnProjectile()
    {
        List<Collider> shotBullets = new List<Collider>();

        for (int i = 0; i < bulletsPerShot; i++)
        {
            Rigidbody rb = Instantiate(projectile, attackPoint.position, Quaternion.identity).GetComponent<Rigidbody>();

            if (bulletsPerShot > 1)
            {
                shotBullets.Add(rb.GetComponent<Collider>());
            }

            foreach (Collider collider in childHitBoxes)
            {
                Physics.IgnoreCollision(rb.gameObject.GetComponent<Collider>(), collider, true);
            }

            foreach (Collider collider in shotBullets)
            {
                Physics.IgnoreCollision(rb.gameObject.GetComponent<Collider>(), collider, true);
            }

            rb.AddForce(projectileVel * (player.transform.position - attackPoint.position).normalized, ForceMode.Impulse);
        }
    }

    private IEnumerator ResetAttack(float attackDelay)
    {
        yield return new WaitForSeconds(attackDelay);
        alreadyAttacked = false;
    }

    public void ReceiveDamage(int damage)
    {
        health -= damage;

        if (health <= 0 && !isDead)
        {
            isDead = true;
            Destroy(gameObject);
            //gameManager.enemiesKilled++;

            if (dropLoot != null)
            {
                dropLoot.InstantiateLoot();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        /*
        PlayerInventory playerInventory = collision.gameObject.GetComponent<PlayerInventory>();
        if (playerInventory != null)
        {
            Rigidbody playerRb = playerInventory.gameObject.GetComponent<Rigidbody>();
            playerRb.AddForce(2000 * (player.transform.position - transform.position).normalized, ForceMode.Impulse);
            playerRb.AddForce(1000 * Vector3.up, ForceMode.Impulse);
            playerInventory.TakeDamage(10);
        }
        */
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, doorRange);
        Gizmos.DrawWireSphere(walkPoint, 1f);
    }
}
