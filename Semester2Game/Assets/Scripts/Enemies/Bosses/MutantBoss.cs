using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ExtensionMethods;
using UnityEngine.UI;
using TMPro;

public class MutantBoss : MonoBehaviour, IDamageable
{
    public Slider bar;
    public TextMeshProUGUI healthNumber;

    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private DropLoot dropLoot;
    [SerializeField] private GameObject player;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LayerMask groundMask, playerMask;
    [SerializeField] private GameObject spawnEffect;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Collider[] childHitBoxes;

    private int health;
    [SerializeField] private int maxHealth;

    [SerializeField] private float firstSpawnDelay;

    private Vector3 walkPoint;
    private bool walkPointSet;
    [SerializeField] private float walkPointStrafeRange, walkPointForwardRange;

    [SerializeField] private float doorRange;
    [SerializeField] private float atPointRange;

    private bool alreadyAttacked;
    [SerializeField] private GameObject projectile;
    [SerializeField] private float projectileVel;
    [SerializeField] private float upVel;
    [SerializeField] private int bulletsPerShot = 1;
    [SerializeField] private float bulletSpread;
    [SerializeField] private float shotgunDelay;

    [SerializeField] private float sightRange, attackRange;
    private bool playerInSightRange, playerInAttackRange;

    private bool isDead;

    [SerializeField] private GameObject toxicPuddle;
    [SerializeField] private float puddleDelay;
    [SerializeField] private float puddleLifetime;

    private void Awake()
    {
        health = maxHealth;
        bar.maxValue = maxHealth;

        if (spawnEffect != null)
        {
            Instantiate(spawnEffect, transform.position, Quaternion.identity);
        }

        player = GameObject.Find("Player");
        gameManager = GameObject.Find("Game Manager").GetComponent<GameManager>();
        //CheckDoor();

        StartCoroutine(SpawnPuddle());

        alreadyAttacked = true;
        StartCoroutine(ResetAttack(firstSpawnDelay));
    }

    private IEnumerator SpawnPuddle()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(puddleDelay);

            GameObject puddle = Instantiate(toxicPuddle, transform.position - new Vector3(0, 1), Quaternion.identity);
            Destroy(puddle, puddleLifetime);
        }
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

    private void Update()
    {
        bar.value = health;
        healthNumber.text = health + " / " + maxHealth;

        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, playerMask);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, playerMask);

        if (!playerInSightRange && !playerInAttackRange)
        {
            Patrol();
        }
        else if (playerInSightRange && !playerInAttackRange)
        {
            Chase();
        }
        else if (playerInAttackRange && playerInSightRange)
        {
            Patrol();
        }

        RollAttack();
    }

    private void RollAttack()
    {
        if (!alreadyAttacked)
        {
            alreadyAttacked = true;
            ShotgunAttack();
            StartCoroutine(ResetAttack(shotgunDelay));
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

    private void ShotgunAttack()
    {
        List<Collider> shotBullets = new List<Collider>();

        for (int i = 0; i < bulletsPerShot; i++)
        {
            Vector3 target = player.transform.position + new Vector3(Random.Range(-bulletSpread, bulletSpread), Random.Range(-bulletSpread, bulletSpread), Random.Range(-bulletSpread, bulletSpread));

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

            rb.AddForce(projectileVel * (target - attackPoint.position).normalized, ForceMode.Impulse);
            rb.AddForce(upVel * Vector3.up, ForceMode.Impulse);
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
            gameManager.AddEnemyKilled();

            if (dropLoot != null)
            {
                dropLoot.InstantiateLoot();
            }
        }
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
