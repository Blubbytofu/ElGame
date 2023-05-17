using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class AmonGusBoss : MonoBehaviour, IDamageable
{
    [SerializeField] private Slider bar;
    [SerializeField] private Image healthBar;
    [SerializeField] private TextMeshProUGUI healthNumber;
    [SerializeField] private GameObject shield;
    [SerializeField] private GameObject bossLight;
    [SerializeField] private GameObject entryDoor;

    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private GameObject player;
    [SerializeField] private LayerMask groundMask, playerMask;
    [SerializeField] private Collider[] childHitBoxes;
    [SerializeField] private DropLoot dropLoot;

    private bool canMove;
    private bool canChase;
    private float switchChaseTime;
    [SerializeField] float canChaseInterval;

    [SerializeField] private float firstDelay;

    private float health;
    [SerializeField] private float maxHealth;

    private Vector3 walkPoint;
    private bool walkPointSet;
    [SerializeField] private float walkPointRange;

    [SerializeField] private bool canAttack;
    private float startAttackTime;
    [SerializeField] private float attackDelay;

    [SerializeField] private int pelletsPerBurst;
    [SerializeField] private float verticalSpread;
    [SerializeField] private float timeBetweenShots;
    [SerializeField] private float maxShotAngle;
    private float shotAngle;
    [SerializeField] private int maxBulletBurst;
    private int bulletsLeftInBurst;
    [SerializeField] private GameObject projectile;
    [SerializeField] private float projectileVel;
    [SerializeField] private int maxDistFromPlayer;

    private float damageReduction;
    private bool specialMoved;

    [SerializeField] private GameObject beam;
    [SerializeField] private int maxBeamBurst;
    private int beamBurst;
    [SerializeField] private float beamDelay;
    [SerializeField] private float beamSpread;

    [SerializeField] private GameObject bomb;
    [SerializeField] private int maxBombsPerBurst;
    private int bombsPerBurst;
    [SerializeField] private float bombBurstDelay;

    [SerializeField] private Color32 regularHealth = new Color32(180, 0, 0, 255);
    [SerializeField] private Color32 defendedHealth = new Color32(130, 0, 0, 255);
    [SerializeField] private Color32 invincibleHealth = new Color32(181, 181, 181, 255);

    [SerializeField] private GameObject box;

    private bool isDead;

    private void Awake()
    {
        health = maxHealth;
        bar.maxValue = maxHealth;
        player = GameObject.Find("Player");
        canMove = true;
        canAttack = false;
        damageReduction = 0;
        Invoke(nameof(StartActions), firstDelay);
        healthBar.color = regularHealth;
        bossLight = GameObject.Find("Boss Light");
        entryDoor = GameObject.Find("Boss Flesh Door");
        bossLight.SetActive(false);
        entryDoor.GetComponent<MeshRenderer>().enabled = true;
        entryDoor.GetComponent<MeshCollider>().enabled = true;
    }

    private void StartActions()
    {
        canAttack = true;
    }

    private void Update()
    {
        BehaviorLogic();

        bar.value = health;
        healthNumber.text = health + " / " + maxHealth;

        if (health <= maxHealth / 2 && !specialMoved)
        {
            SpecialMove();
        }
    }
    
    private void SpecialMove()
    {
        specialMoved = true;
        healthBar.color = invincibleHealth;
        agent.SetDestination(transform.position);
        transform.LookAt(player.transform);
        damageReduction = 1f;
        canAttack = false;
        canMove = false;
        Invoke(nameof(EndSpecial), 13f);
        shield.SetActive(true);

        beamBurst =  3 * maxBeamBurst;
        Invoke(nameof(SkyBeams), 3 * beamDelay);
    }

    private void EndSpecial()
    {
        damageReduction = 0.5f;
        canAttack = true;
        canMove = true;
        healthBar.color = defendedHealth;
        shield.SetActive(false);
    }

    private void BehaviorLogic()
    {
        //movement
        if (canMove)
        {
            if (Vector3.Distance(transform.position, player.transform.position) > maxDistFromPlayer && canChase)
            {
                Chase();
            }
            else
            {
                if (Time.time > switchChaseTime + canChaseInterval)
                {
                    canChaseInterval = Random.Range(3f, 6f);
                    switchChaseTime = Time.time;
                    walkPointSet = false;
                    ResumeChase();
                }
                Patrol();
            }
        }

        //attacks
        if (canAttack)
        {
            if (Time.time > startAttackTime + attackDelay)
            { 
                int action = Random.Range(0, 4);
                if (action == 0)
                {
                    if (specialMoved)
                    {
                        attackDelay = 3f;
                    }
                    else
                    {
                        attackDelay = 4f;
                    }
                    shotAngle = maxShotAngle;
                    bulletsLeftInBurst = maxBulletBurst;
                    ShotgunBurst();
                }
                else if (action == 1)
                {
                    if (specialMoved)
                    {
                        attackDelay = 9f;
                        maxBombsPerBurst = 5;
                    }
                    else
                    {
                        attackDelay = 8f;
                        maxBombsPerBurst = 3;
                    }
                    bombsPerBurst = maxBombsPerBurst;
                    Bomb();
                }
                else if (action == 2)
                {
                    if (specialMoved)
                    {
                        attackDelay = 7f;
                        maxBombsPerBurst = 5;
                    }
                    else
                    {
                        attackDelay = 7f;
                        maxBombsPerBurst = 3;
                    }
                    bombsPerBurst = maxBombsPerBurst;
                    Bomb();
                }
                else
                {
                    if (specialMoved)
                    {
                        attackDelay = 6f;
                        beamBurst = maxBeamBurst;
                        SkyBeams();
                    }
                    else
                    {
                        attackDelay = 4f;
                        shotAngle = maxShotAngle;
                        bulletsLeftInBurst = maxBulletBurst;
                        ShotgunBurst();
                    }
                }

                startAttackTime = Time.time;
            }
        }
    }

    private void ResumeChase()
    {
        canChase = !canChase;
    }

    private void Patrol()
    {
        if (walkPointSet)
        {
            if (specialMoved)
            {
                agent.speed = 14;
                agent.acceleration = 20f;
            }
            else
            {
                agent.speed = 9;
                agent.acceleration = 8;
            }

            agent.SetDestination(walkPoint);
        }
        else
        {
            SearchWalkPoint();
        }

        Vector3 distanceToWalkPoint = transform.position - walkPoint;
        if (distanceToWalkPoint.magnitude < 1f)
        {
            walkPointSet = false;
        }
    }

    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        RaycastHit groundCheck;
        if (Physics.Raycast(walkPoint, -transform.up, out groundCheck, 7, groundMask))
        {
            if (groundCheck.distance > 0.1f)
            {
                walkPointSet = true;
            }
        }
    }

    private void Chase()
    {
        agent.speed = 7;
        agent.SetDestination(player.transform.position);
    }

    private void SpawnBox()
    {
        float angle = Random.Range(0f, 360f);
        transform.LookAt(player.transform);
        transform.Rotate(0, angle, 0);
        Rigidbody rb = Instantiate(box, transform.position, Quaternion.identity).GetComponent<Rigidbody>();

        foreach (Collider collider in childHitBoxes)
        {
            Physics.IgnoreCollision(rb.gameObject.GetComponent<Collider>(), collider, true);
        }

        rb.AddForce(transform.forward * Random.Range(200, 300), ForceMode.Impulse);
        rb.AddForce(transform.up * Random.Range(200, 300), ForceMode.Impulse);
    }

    private void ShotgunBurst()
    {
        List<Collider> shotBullets = new List<Collider>();

        float angle = -shotAngle;
        for (int k = 0; k < pelletsPerBurst; k++)
        {
            angle = Random.Range(-shotAngle, shotAngle);
            transform.LookAt(player.transform);
            float vSpread = Random.Range(-verticalSpread, verticalSpread);
            transform.Rotate(vSpread, angle, 0);
            Rigidbody rb = Instantiate(projectile, transform.position, Quaternion.identity).GetComponent<Rigidbody>();
            shotBullets.Add(rb.GetComponent<Collider>());

            foreach (Collider collider in childHitBoxes)
            {
                Physics.IgnoreCollision(rb.gameObject.GetComponent<Collider>(), collider, true);
            }

            foreach (Collider collider in shotBullets)
            {
                Physics.IgnoreCollision(rb.gameObject.GetComponent<Collider>(), collider, true);
            }

            rb.AddForce(transform.forward * projectileVel, ForceMode.Impulse);
        }

        bulletsLeftInBurst--;
        if (bulletsLeftInBurst > 0)
        {
            Invoke(nameof(ShotgunBurst), timeBetweenShots);
        }
        else
        {
            Invoke(nameof(SpawnBox), 0.25f);
        }
    }

    private void SkyBeams()
    {
        Instantiate(beam, player.transform.position + new Vector3(Random.Range(-beamSpread, beamSpread), 0, Random.Range(-beamSpread, beamSpread)), Quaternion.identity);
        beamBurst--;

        if (beamBurst > 0)
        {
            Invoke(nameof(SkyBeams), beamDelay);
        }
        else
        {
            Invoke(nameof(SpawnBox), 0.25f);
        }
    }

    private void Bomb()
    {
        List<Collider> shotBullets = new List<Collider>();

        float angle = Random.Range(0f, 360f);
        transform.LookAt(player.transform);
        transform.Rotate(0, angle, 0);
        Rigidbody rb = Instantiate(bomb, transform.position, Quaternion.identity).GetComponent<Rigidbody>();
        shotBullets.Add(rb.GetComponent<Collider>());

        foreach (Collider collider in childHitBoxes)
        {
            Physics.IgnoreCollision(rb.gameObject.GetComponent<Collider>(), collider, true);
        }

        foreach (Collider collider in shotBullets)
        {
            Physics.IgnoreCollision(rb.gameObject.GetComponent<Collider>(), collider, true);
        }

        rb.AddForce(transform.forward * Random.Range(18f, 24f), ForceMode.Impulse);
        rb.AddForce(transform.up * Random.Range(18f, 24f), ForceMode.Impulse);
        bombsPerBurst--;

        if (bombsPerBurst > 0)
        {
            Invoke(nameof(Bomb), bombBurstDelay);
        }
        else
        {
            Invoke(nameof(SpawnBox), 0.25f);
        }
    }

    public void ReceiveDamage(int damage)
    {
        health -= damage * (1 - damageReduction);

        if (health <= 0 && !isDead)
        {
            isDead = true;
            bossLight.SetActive(true);
            dropLoot.InstantiateLoot();
            Destroy(gameObject);
        }
    }
}
