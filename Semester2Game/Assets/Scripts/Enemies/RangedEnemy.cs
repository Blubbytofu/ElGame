using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RangedEnemy : MonoBehaviour, IDamageable
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private DropLoot dropLoot;
    [SerializeField] private GameObject player;
    //[SerializeField] private GameManager gameManager;
    [SerializeField] private LayerMask groundMask, playerMask;
    //[SerializeField] private GameObject spawnEffect;

    [SerializeField] private float health;

    private Vector3 walkPoint;
    private bool walkPointSet;
    [SerializeField] private float walkPointRange;

    [SerializeField] private float doorRange;
    [SerializeField] private float atPointRange;

    [SerializeField] private float timeBetweenAttacks;
    private bool alreadyAttacked;
    [SerializeField] private GameObject projectile;
    [SerializeField] private float projectileVel;
    [SerializeField] private float bulletSpread;

    [SerializeField] private float sightRange, attackRange;
    private bool playerInSightRange, playerInAttackRange;

    private bool isDead;

    private void Awake()
    {
        //Instantiate(spawnEffect, transform.position, Quaternion.identity);
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
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);
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
        Vector3 target = player.transform.position + new Vector3(Random.Range(-bulletSpread, bulletSpread), Random.Range(-bulletSpread, bulletSpread), Random.Range(-bulletSpread, bulletSpread));

        transform.LookAt(new Vector3(target.x, transform.position.y, target.z));

        if (!alreadyAttacked)
        {
            Rigidbody rb = Instantiate(projectile, transform.position, Quaternion.identity).GetComponent<Rigidbody>();
            Physics.IgnoreCollision(rb.gameObject.GetComponent<Collider>(), GetComponent<Collider>(), true);
            rb.AddForce((target - transform.position).normalized * projectileVel, ForceMode.Impulse);
            alreadyAttacked = true;
            StartCoroutine(ResetAttack());
        }
    }

    private IEnumerator ResetAttack()
    {
        yield return new WaitForSeconds(timeBetweenAttacks);
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
