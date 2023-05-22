using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;

namespace PlayerObject
{
    public class Weapon : MonoBehaviour
    {
        [Header("References-----------------------------------------------------------------------------")]
        [SerializeField] private Camera playerCam;
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private Transform attackPoint;
        [SerializeField] private GameObject bulletImpact;
        [SerializeField] private GameObject critImpact;
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private LayerMask environmentMask, enemyMask;
        [SerializeField] private Animator weaponAnimator;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PrefsManager prefsManager;
        [SerializeField] private WeaponManager weaponManager;
        [SerializeField] private Rigidbody playerRb;
        private RaycastHit rayHit;

        [Header("Weapon Identity-----------------------------------------------------------------------------")]
        public bool owned;
        [field: SerializeField] public string weaponName { get; private set; }
        [SerializeField] private int weaponIndex;
        [SerializeField] private int damage;
        [SerializeField] private float timeBetweenShooting;
        [SerializeField] private float range;
        //bullets shot per instance of shooting
        public int bulletsPerShot;
        //bullets subtracted from ammo pool
        [SerializeField] private int bulletsSubtracted;
        [SerializeField] private bool singleFire;
        private bool shooting;
        private bool readyToShoot;
        public bool isHitScan;
        [SerializeField] private GameObject projectile;
        [SerializeField] private bool inheritPlayerVel;
        [SerializeField] private float projectileZVel;
        [SerializeField] private float projectileYVel;

        [Header("Spread-----------------------------------------------------------------------------")]
        public float initialSpread;
        public float maxSpread;
        [SerializeField] private float currentSpread;
        [SerializeField] private float deltaSpread;
        private float lastShotTime;
        [SerializeField] private float spreadDecreaseDelay;
        [SerializeField] private float spreadDecreaseRate;
        private Vector3 totalSpread;

        [Header("Recoil-----------------------------------------------------------------------------")]
        [SerializeField] private float lowerRecoilXMagnitude;
        [SerializeField] private float upperRecoilXMagnitude;
        [SerializeField] private float lowerRecoilYMagnitude;
        [SerializeField] private float upperRecoilYMagnitude;

        [Header("Tracer-------------------------------------------------------------------------------")]
        [SerializeField] private GameObject tracer;
        private Vector3 tracerEndPoint;
        [SerializeField] private float tracerSpawnDistance;
        [SerializeField] private float tracerLifetime;
        [SerializeField] private int bulletsBetweenTracers;
        private int currentBulletsBetweenTracers;

        [Header("Magazine-----------------------------------------------------------------------------")]
        [SerializeField] private int maxMagSize;
        [field: SerializeField] public bool hasMag { get; private set; }
        [field: SerializeField] public int currentMagSize { get; private set; }
        //part 1 is before updating ammo
        [SerializeField] private float reloadTimePart1;
        //part 2 is interval between updating hud and shooting
        [SerializeField] private float reloadTimePart2;
        //total reload time is part1 + part2
        public bool isReloading { get; private set; }
        [SerializeField] private bool shotgunReload;
        private bool firstShotgunReload;
        private bool queuedStop;

        [Header("Burst Fire-----------------------------------------------------------------------------")]
        [SerializeField] private int maxBulletsPerBurst;
        public bool isBurstFire;
        [SerializeField] private float burstDelay;

        public int currentBurst { get; private set; }

        [Header("Heat Based-----------------------------------------------------------------------------")]
        [SerializeField] private bool isHeatBased;
        private bool takenOverheatDamage;
        private bool alreadyRecharging;
        [SerializeField] private float heatStartRegenDelay;
        [SerializeField] private int regenPerTick;
        [SerializeField] private float fullEmptyDelay;
        [SerializeField] private float normalRegenDelay;
        [SerializeField] private int overheatDamage;

        [Header("Special Properties-----------------------------------------------------------------------------")]
        //is delay between fire input and actual shot
        [SerializeField] private float fireInputDelay;
        public bool isParentWeapon;
        public Weapon childWeapon;
        [SerializeField] private bool isChildWeapon;
        [SerializeField] private Weapon parentWeapon;
        public bool activeSecondary;
        [SerializeField] private bool queueShotProperty;
        private bool inQueueShotInterval;
        private bool queueNextShot;
        [SerializeField] private float queueInterval;

        private void Awake()
        {
            readyToShoot = true;
            if (hasMag)
            {
                currentMagSize = maxMagSize;
            }
        }

        public void StartEquip()
        {
            queueNextShot = false;
            readyToShoot = false;
            isReloading = false;
        }

        public void FinishEquip()
        {
            readyToShoot = true;
        }

        private void Update()
        {
            if (playerInventory.isDead || playerInventory.wonLevel || prefsManager.settingsOpen)
            {
                return;
            }

            if (isChildWeapon && !parentWeapon.isActiveAndEnabled)
            {
                gameObject.SetActive(false);
            }

            if (isParentWeapon)
            {
                shooting = (singleFire ? Input.GetKeyDown(KeyCode.Mouse0) : Input.GetKey(KeyCode.Mouse0)) && (childWeapon.readyToShoot || !activeSecondary);
            }
            else if (isChildWeapon)
            {
                shooting = !parentWeapon.isReloading && (singleFire ? Input.GetKeyDown(KeyCode.Mouse0) : Input.GetKey(KeyCode.Mouse0));
            }
            else
            {
                shooting = singleFire ? Input.GetKeyDown(KeyCode.Mouse0) : Input.GetKey(KeyCode.Mouse0);
            }

            if (queueShotProperty && inQueueShotInterval && shooting && !queueNextShot && !isReloading)
            {
                queueNextShot = true;
            }

            if (readyToShoot && !isReloading && currentBurst < 1)
            {
                ShootInput();
            }

            if (shotgunReload && isReloading && !queuedStop && shooting)
            {
                queuedStop = true;
            }

            if ((currentMagSize == 0 || Input.GetKeyDown(KeyCode.R)) && (hasMag && currentMagSize < maxMagSize && weaponManager.GetCurrentAmmo(weaponIndex) > 0 && !isReloading && currentBurst < 1))
            {
                if (isChildWeapon && weaponManager.GetCurrentAmmo(weaponIndex) == 1)
                {
                    return;
                }

                isReloading = true;

                if (shotgunReload)
                {
                    firstShotgunReload = true;
                }

                StartCoroutine(ReloadMag());
            }

            if (isHeatBased)
            {
                RechargeHeatAmmo();
            }
        }

        private void RechargeHeatAmmo()
        {
            if (!alreadyRecharging && Time.time > lastShotTime + heatStartRegenDelay && weaponManager.GetCurrentAmmo(weaponIndex) < weaponManager.GetMaxAmmo(weaponIndex))
            {
                alreadyRecharging = true;
                if (weaponManager.GetCurrentAmmo(weaponIndex) == 0)
                {
                    Invoke(nameof(ResetRecharge), fullEmptyDelay);
                }
                else
                {
                    Invoke(nameof(ResetRecharge), normalRegenDelay);
                }
            }

            if (weaponManager.GetCurrentAmmo(weaponIndex) == 0 && !takenOverheatDamage)
            {
                takenOverheatDamage = true;
                playerInventory.TakeDamage(overheatDamage);
            }
        }

        private void ResetRecharge()
        {
            weaponManager.AddAmmo(weaponIndex, regenPerTick);
            takenOverheatDamage = false;
            alreadyRecharging = false;
        }

        private void ShootInput()
        {
            if ((shooting || queueNextShot) && (hasMag? currentMagSize >= bulletsSubtracted : weaponManager.GetCurrentAmmo(weaponIndex) >= bulletsSubtracted))
            {
                if (isBurstFire)
                {
                    if (hasMag? currentMagSize >= maxBulletsPerBurst : weaponManager.GetCurrentAmmo(weaponIndex) >= maxBulletsPerBurst)
                    {
                        currentBurst = maxBulletsPerBurst;
                    }
                    else
                    {
                        currentBurst = (hasMag? currentMagSize : weaponManager.GetCurrentAmmo(weaponIndex));
                    }
                }

                if (queueNextShot)
                {
                    queueNextShot = false;
                }

                readyToShoot = false;
                Invoke(nameof(Shoot), fireInputDelay);
            }

            if (!shooting && Time.time > lastShotTime + spreadDecreaseDelay)
            {
                currentSpread = Mathf.Lerp(currentSpread, initialSpread, Time.deltaTime * spreadDecreaseRate);
            }
        }

        public void MuzzleFlash()
        {
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }
        }

        private void Shoot()
        {
            //weaponManager.PlayAttackSound(weaponIndex);

            if (weaponAnimator != null)
            {
                weaponAnimator.SetTrigger("_shoot");
            }

            if (hasMag)
            {
                currentMagSize -= bulletsSubtracted;
            }
            else
            {
                weaponManager.SubtractAmmo(weaponIndex, bulletsSubtracted);
            }

            for (int shotBullets = 0; shotBullets < bulletsPerShot; shotBullets++)
            {
                if (isHitScan)
                {
                    float xSpread = Random.Range(-currentSpread, currentSpread);
                    float ySpread = Random.Range(-currentSpread, currentSpread);

                    totalSpread = playerCam.transform.right * xSpread + playerCam.transform.up * ySpread;

                    if (Physics.Raycast(playerCam.transform.position, playerCam.transform.forward + totalSpread, out rayHit, range, enemyMask) ||
                    Physics.Raycast(playerCam.transform.position, playerCam.transform.forward + totalSpread, out rayHit, range, environmentMask))
                    {
                        if (bulletImpact != null)
                        {
                            EnemyBodyPart hitPart = rayHit.transform.gameObject.GetComponent<EnemyBodyPart>();
                            if (hitPart != null)
                            {
                                if (hitPart.damageMultiplier == 1)
                                {
                                    Instantiate(bulletImpact, rayHit.point, Quaternion.LookRotation(rayHit.normal));
                                }
                                else if (hitPart.damageMultiplier < 1)
                                {

                                }
                                else if (hitPart.damageMultiplier > 1)
                                {
                                    GameObject hit = Instantiate(critImpact, rayHit.point, Quaternion.LookRotation(rayHit.normal));
                                    Destroy(hit, 1f);
                                }
                            }
                            else
                            {
                                Instantiate(bulletImpact, rayHit.point, Quaternion.LookRotation(rayHit.normal));
                            }
                        }

                        IDamageable damageable = rayHit.collider.gameObject.GetComponent<IDamageable>();
                        if (damageable != null)
                        {
                            damageable.ReceiveDamage(damage);
                        }

                        if (tracer != null && currentBulletsBetweenTracers == bulletsBetweenTracers)
                        {
                            tracerEndPoint = rayHit.point;
                        }
                    }
                    else
                    {
                        if (tracer != null && currentBulletsBetweenTracers == bulletsBetweenTracers)
                        {
                            tracerEndPoint = playerCam.transform.position + range * (totalSpread + playerCam.transform.forward);
                        }
                    }

                    if (tracer != null)
                    {
                        if (currentBulletsBetweenTracers == bulletsBetweenTracers)
                        {
                            LineRenderer tracerObj = Instantiate(tracer, attackPoint.position, Quaternion.identity).GetComponent<LineRenderer>();
                            tracerObj.gameObject.GetComponent<BulletTracer>().PassConstraints(attackPoint, tracerSpawnDistance, tracerEndPoint);
                            Destroy(tracerObj.gameObject, tracerLifetime);
                            currentBulletsBetweenTracers = 0;
                        }
                        else
                        {
                            currentBulletsBetweenTracers++;
                        }
                    }
                }
                else
                {
                    GameObject playerProjectile = Instantiate(projectile, attackPoint.position, Quaternion.identity);

                    Ray directionRay = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

                    Vector3 projectileDestination;

                    if (Physics.Raycast(directionRay, out rayHit))
                    {
                        projectileDestination = rayHit.point;
                    }
                    else
                    {
                        projectileDestination = directionRay.GetPoint(100f);
                    }

                    Vector3 projectileDirection = projectileDestination - attackPoint.position;
                    playerProjectile.transform.forward = projectileDirection.normalized;

                    Rigidbody projectileRb = playerProjectile.GetComponent<Rigidbody>();

                    projectileRb.AddForce(playerProjectile.transform.forward * projectileZVel, ForceMode.Impulse);
                    projectileRb.AddForce(playerProjectile.transform.up * projectileYVel, ForceMode.Impulse);

                    if (inheritPlayerVel)
                    {
                        projectileRb.AddForce(playerRb.velocity, ForceMode.VelocityChange);
                    }
                }


                float xRecoil = Random.Range(lowerRecoilXMagnitude, upperRecoilXMagnitude);
                float yRecoil = Random.Range(lowerRecoilYMagnitude, upperRecoilYMagnitude);
                playerCamera.SimulateRecoil(xRecoil, yRecoil);
            }

            if (currentSpread <= maxSpread)
            {
                currentSpread += deltaSpread;
            }

            lastShotTime = Time.time;

            if (isBurstFire)
            {
                currentBurst--;
                if (currentBurst > 0)
                {
                    Invoke(nameof(Shoot), burstDelay);
                }
                else
                {
                    StartCoroutine(ResetShot());
                }
            }
            else
            {
                StartCoroutine(ResetShot());
            }
        }

        private IEnumerator ResetShot()
        {
            if (queueShotProperty)
            {
                yield return new WaitForSeconds(timeBetweenShooting - queueInterval);
                inQueueShotInterval = true;
                yield return new WaitForSeconds(queueInterval);
                inQueueShotInterval = false;
                readyToShoot = true;
            }
            else
            {
                yield return new WaitForSeconds(timeBetweenShooting);
                readyToShoot = true;
            }
        }

        private IEnumerator ReloadMag()
        {
            if (queueShotProperty && queueNextShot)
            {
                queueNextShot = false;
            }

            if (weaponAnimator != null)
            {
                if (shotgunReload)
                {
                    if (firstShotgunReload)
                    {
                        firstShotgunReload = false;
                        weaponAnimator.SetTrigger("_reload");
                    }
                    else
                    {
                        weaponAnimator.SetTrigger("_continueReload");
                    }
                }
                else
                {
                    weaponAnimator.SetTrigger("_reload");
                }
            }

            int wishReload;
            int bulletsInStock = 0;

            if (shotgunReload)
            {
                wishReload = 1;
            }
            else
            {
                wishReload = maxMagSize - currentMagSize;

                if (isParentWeapon)
                {
                    bulletsInStock = weaponManager.GetCurrentAmmo(weaponIndex);
                    if (activeSecondary)
                    {
                        //even stock
                        if (bulletsInStock % 2 == 0)
                        {
                            bulletsInStock = weaponManager.GetCurrentAmmo(weaponIndex) / 2;
                        }
                        else
                        {
                            bulletsInStock = weaponManager.GetCurrentAmmo(weaponIndex) / 2 + 1;
                        }
                    }
                }
                else if (isChildWeapon)
                {
                    bulletsInStock = weaponManager.GetCurrentAmmo(weaponIndex) / 2;
                }
                else
                {
                    bulletsInStock = weaponManager.GetCurrentAmmo(weaponIndex);
                }

                if (bulletsInStock < wishReload)
                {
                    wishReload = bulletsInStock;
                }
            }

            yield return new WaitForSeconds(reloadTimePart1);

            currentMagSize += wishReload;
            weaponManager.SubtractAmmo(weaponIndex, wishReload);

            yield return new WaitForSeconds(reloadTimePart2);

            if (shotgunReload)
            {
                if (queuedStop)
                {
                    queuedStop = false;
                    isReloading = false;
                }
                else
                {
                    if (currentMagSize < maxMagSize && weaponManager.GetCurrentAmmo(weaponIndex) > 0)
                    {
                        StartCoroutine(ReloadMag());
                    }
                    else
                    {
                        isReloading = false;
                    }
                }
            }
            else
            {
                isReloading = false;
            }
        }
    }
}
