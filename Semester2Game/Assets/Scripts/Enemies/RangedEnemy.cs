using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ExtensionMethods;
using PlayerObject;

public class RangedEnemy : MonoBehaviour, IDamageable
{
    [Header("References------------------------------------------------------------------------------------------")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private DropLoot dropLoot;
    [SerializeField] private GameObject player;
    //[SerializeField] private GameManager gameManager;
    [SerializeField] private LayerMask groundMask, playerMask;
    [SerializeField] private GameObject spawnEffect;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Collider[] childHitBoxes;

    [Header("Health------------------------------------------------------------------------------------------------")]
    [SerializeField] private int health;

    [Header("Movement---------------------------------------------------------------------------------------------")]
    [SerializeField] private float walkPointRange;
    private Vector3 walkPoint;
    private bool walkPointSet;

    [SerializeField] private float doorRange;
    [SerializeField] private float atPointRange;

    [SerializeField] private float sightRange, attackRange;
    private bool playerInSightRange, playerInAttackRange;

    [SerializeField] private bool aggressiveApproach;

    [Header("Attack--------------------------------------------------------------------------------------------------")]
    [SerializeField] private bool isHitScan;
    [SerializeField] private int hitScanDamage;
    [SerializeField] private GameObject hitScanEffect;

    [SerializeField] private float attackDelay;
    private bool alreadyAttacked;
    [SerializeField] private GameObject projectile;
    [SerializeField] private float projectileVel;
    [SerializeField] private int bulletsPerShot = 1;
    [SerializeField] private float bulletSpread;

    [SerializeField] private bool canInterruptAttack;
    [SerializeField] private float interruptAttackTime;
    private bool interrupted;

    [SerializeField] private bool canOnlyShootForwards;
    [SerializeField] private GameObject forwardsDirection;

    private bool isDead;

    private void Awake()
    {
        if (spawnEffect != null)
        {
            Instantiate(spawnEffect, transform.position, Quaternion.identity);
        }

        player = GameObject.Find("Player");
        //gameManager = GameObject.Find("Game Manager").GetComponent<GameManager>();
        //CheckDoor();
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
            Attack();
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
        if (aggressiveApproach)
        {
            Vector3 xPos = Vector3.Cross(player.transform.position.ReplaceField(newY: transform.position.y) - transform.position, Vector3.up).normalized * Random.Range(-walkPointRange, walkPointRange);
            Vector3 zPos = (player.transform.position.ReplaceField(newY: transform.position.y) - transform.position).normalized * Random.Range(0, walkPointRange);
            walkPoint = transform.position + xPos + zPos;
        }
        else
        {
            float randomZ = Random.Range(-walkPointRange, walkPointRange);
            float randomX = Random.Range(-walkPointRange, walkPointRange);

            walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);
        }

        if (Physics.Raycast(walkPoint, -transform.up, 2f, groundMask))
        {
            walkPointSet = true;
        }
    }

    private void Chase()
    {
        agent.SetDestination(player.transform.position);
    }

    private void Attack()
    {
        if (!alreadyAttacked && !interrupted)
        {
            if (isHitScan)
            {
                player.GetComponent<PlayerInventory>().TakeDamage(hitScanDamage);
                EnemyMeleeEffect effect = Instantiate(hitScanEffect, transform.position, Quaternion.LookRotation((player.transform.position - transform.position).normalized, Vector3.up)).GetComponent<EnemyMeleeEffect>();
                effect.enemyTransform = transform;
            }
            else
            {
                List<Collider> shotBullets = new List<Collider>();

                for (int i = 0; i < bulletsPerShot; i++)
                {
                    Vector3 target;
                    if (canOnlyShootForwards)
                    {
                        target = forwardsDirection.transform.forward * 10 + new Vector3(Random.Range(-bulletSpread, bulletSpread), Random.Range(-bulletSpread, bulletSpread), Random.Range(-bulletSpread, bulletSpread));
                    }
                    else
                    {
                        target = player.transform.position + new Vector3(Random.Range(-bulletSpread, bulletSpread), Random.Range(-bulletSpread, bulletSpread), Random.Range(-bulletSpread, bulletSpread));
                    }

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

                    rb.AddForce((target - attackPoint.position).normalized * projectileVel, ForceMode.Impulse);
                }
            }

            alreadyAttacked = true;
            StartCoroutine(ResetAttack());
        }
    }

    private IEnumerator ResetAttack()
    {
        yield return new WaitForSeconds(attackDelay);
        alreadyAttacked = false;
    }

    private IEnumerator InterruptAttack()
    {
        yield return new WaitForSeconds(interruptAttackTime);
        interrupted = false;
    }

    public void ReceiveDamage(int damage)
    {
        health -= damage;

        if (canInterruptAttack)
        {
            interrupted = true;
            StartCoroutine(InterruptAttack());
        }

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, doorRange);
    }
}
