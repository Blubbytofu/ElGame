using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ExtensionMethods;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private Transform attackPoint;
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
        agent.enabled = false;
        bossRb.isKinematic = false;
        bossRb.useGravity = true;
    }

    private void DeactivateRbMode()
    {
        agent.enabled = true;
        bossRb.isKinematic = true;
        bossRb.useGravity = false;
    }

    private void Update()
    {
        bar.value = health;
        healthNumber.text = health + " / " + maxHealth;

        if (agent.isActiveAndEnabled)
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
            }
        }

        RollAttack();
    }

    private void RollAttack()
    {
        if (!alreadyAttacked)
        {
            alreadyAttacked = true;

            int randNum = Random.Range(0, 2);

            if (randNum == 1)
            {
                ActivateRbMode();
                Invoke(nameof(DeactivateRbMode), 1f);
            }
            else
            {

            }

            StartCoroutine(ResetAttack(1f));
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
