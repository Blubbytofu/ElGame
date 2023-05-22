using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerObject
{
    public class WeaponOverride : MonoBehaviour
    {
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PrefsManager prefsManager;

        [SerializeField] private Weapon weapon;
        //[SerializeField] private PlayerCamera pCamera;

        [SerializeField] private bool singleFire;
        public bool shooting { get; private set; }

        //[SerializeField] private bool zoom;
        //[SerializeField] private float zoomAmount;

        [SerializeField] private bool dualWield;
        [SerializeField] private GameObject secondGun;

        [SerializeField] private bool altBurstMode;
        [SerializeField] private bool altProjectile;
        [SerializeField] private bool altShotNumber;
        [SerializeField] private int bulletsPerShot;
        [SerializeField] private int newBulletsPerShot;

        [SerializeField] private bool affectSpread;
        [SerializeField] private float normalInitialSpread;
        [SerializeField] private float normalMaxSpread;
        [SerializeField] private float newInitialSpread;
        [SerializeField] private float newMaxSpread;

        private void Update()
        {
            if (playerInventory.isDead || playerInventory.wonLevel || prefsManager.settingsOpen)
            {
                return;
            }

            GetInput();
            ExecuteShoot();
        }

        private void GetInput()
        {
            if (singleFire)
            {
                if (Input.GetKeyDown(KeyCode.Mouse1))
                {
                    shooting = !shooting;
                }
            }
            else
            {
                if (Input.GetKey(KeyCode.Mouse1))
                {
                    shooting = true;
                }
                else
                {
                    shooting = false;
                }
            }

        }

        private void ExecuteShoot()
        {
            if (dualWield)
            {
                if (shooting)
                {
                    secondGun.SetActive(true);
                    weapon.activeSecondary = true;
                }
                else
                {
                    secondGun.SetActive(false);
                    weapon.activeSecondary = false;
                }
            }

            /*
            if (zoom)
            {
                if (shooting)
                {
                    pCamera.zoomFactor = zoomAmount;
                }
                else
                {
                    pCamera.zoomFactor = 1;
                }
            }
            */

            if (altBurstMode)
            {
                if (shooting)
                {
                    weapon.isBurstFire = true;
                }
                else
                {
                    if (weapon.currentBurst < 1)
                    {
                        weapon.isBurstFire = false;
                    }
                }
            }

            if (affectSpread)
            {
                if (shooting)
                {
                    weapon.initialSpread = newInitialSpread;
                    weapon.maxSpread = newMaxSpread;
                }
                else
                {
                    weapon.initialSpread = normalInitialSpread;
                    weapon.maxSpread = normalMaxSpread;
                }
            }

            if (altProjectile)
            {
                if (shooting)
                {
                    weapon.isHitScan = false;
                }
                else
                {
                    weapon.isHitScan = true;
                }
            }

            if (altShotNumber)
            {
                if (shooting)
                {
                    weapon.bulletsPerShot = newBulletsPerShot;
                }
                else
                {
                    weapon.bulletsPerShot = bulletsPerShot;
                }
            }
        }
    }
}
