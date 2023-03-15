using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TrainingDummy : MonoBehaviour, IDamageable
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material normalMat;
    [SerializeField] private Material hurtMat;

    [SerializeField] private int maxHealth;
    [SerializeField] private int health;
    [SerializeField] private float damageMultiplier;
    [SerializeField] private int damageAbsorption;
    [SerializeField] private float hurtTime;
    [SerializeField] private float despawnTime;

    [SerializeField] private bool doPatrol;
    [SerializeField] private Vector3[] patrolPoints;
    [SerializeField] private float minDistToPoint;
    private bool hasPoint;
    private Vector3 destination;

    private void Start()
    {
        ReAnimate();
    }

    private void Update()
    {
        if (doPatrol)
        {
            Patrol();
        }
    }

    private void Patrol()
    {
        if (hasPoint)
        {
            agent.SetDestination(destination);

            if ((transform.position - destination).magnitude < minDistToPoint)
            {
                hasPoint = false;
            }
        }
        else
        {
            GetPatrolPoint();
        }
    }

    private void GetPatrolPoint()
    {
        int rand = Random.Range(0, patrolPoints.Length);
        if (destination != patrolPoints[rand])
        {
            hasPoint = true;
            destination = patrolPoints[rand];
        }
    }

    public void ReceiveDamage(int damage)
    {
        float newDamage = (damage - damageAbsorption) * damageMultiplier;

        if ((int) newDamage > 0)
        {
            meshRenderer.material = hurtMat;
            Invoke(nameof(ResetMat), hurtTime);
        }

        if (health - (int) newDamage <= 0)
        {
            health = 0;
            Die();
        }
        else
        {
            health -= (int) newDamage;
        }
    }

    private void ResetMat()
    {
        meshRenderer.material = normalMat;
    }

    private void Die()
    {
        gameObject.SetActive(false);
        Invoke(nameof(ReAnimate), despawnTime);
    }

    private void ReAnimate()
    {
        gameObject.SetActive(true);
        health = maxHealth;
    }
}
