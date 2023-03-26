using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerObject
{
    public class PlayerInventory : MonoBehaviour, IDataPersistence
    {
        [SerializeField] private Transform orientationTransform;

        [SerializeField] private HUDManager hudManager;
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private WeaponManager weaponManager;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PrefsManager prefsManager;

        [SerializeField] private int partsHealthToArmor;
        [field: SerializeField] public int maxHealth { get; private set; }
        [field: SerializeField] public int health { get; private set; }
        [field: SerializeField] public int maxArmor { get; private set; }
        [field: SerializeField] public int armor { get; private set; }

        [field: SerializeField] public int maxOxygen { get; private set; }
        [field: SerializeField] public int oxygen { get; private set; }

        [SerializeField] private float oxygenDetectIntervals;
        private float lastOxygenTime;
        [SerializeField] private float hideOxygenDelay;
        [SerializeField] private int oxygenDecreaseAmount;
        [SerializeField] private int oxygenIncreaseAmount;
        [SerializeField] private int suffocationDamage;

        public bool isDead { get; private set; }
        public bool wonLevel { get; private set; }

        [field: SerializeField] public bool hasBlueKey { get; private set; }
        [field: SerializeField] public bool hasRedKey { get; private set; }

        private bool currentlyTakingDOT;

        private void Start()
        {
            hudManager.UpdateHealth(health);
            hudManager.UpdateArmor(armor);
        }

        private void Update()
        {
            if (isDead || wonLevel)
            {
                return;
            }

            ManageOxygen();
        }

        private void ManageOxygen()
        {
            if (playerCamera.GetBreathingWater())
            {
                hudManager.ToggleOxygenMonitor(true);
            }
            else
            {
                if (oxygen == maxOxygen)
                {
                    if (hudManager.oxygenIndicator.activeSelf)
                    {
                        Invoke(nameof(OffOxygenText), hideOxygenDelay);
                    }
                    lastOxygenTime = Time.time + hideOxygenDelay;
                }
            }

            if (Time.time > lastOxygenTime + oxygenDetectIntervals)
            {
                lastOxygenTime = Time.time;
                if (playerCamera.GetBreathingWater())
                {
                    ChangeOxygen(-oxygenDecreaseAmount);
                }
                else
                {
                    ChangeOxygen(oxygenIncreaseAmount);
                }
            }
        }

        private void OffOxygenText()
        {
            if (!playerCamera.GetBreathingWater())
            {
                hudManager.ToggleOxygenMonitor(false);
            }
        }

        public IEnumerator DamageOverTime(int ticks, float delay, int damage)
        {
            if (currentlyTakingDOT)
            {
                yield break;
            }

            currentlyTakingDOT = true;

            for (int i = 0; i < ticks; i++)
            {
                TakeDamage(damage);
                yield return new WaitForSeconds(delay);
            }

            currentlyTakingDOT = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Exit"))
            {
                wonLevel = true;
                hudManager.SetGameHUDVisible(false);
                hudManager.SetWonGameMenuVisible(true);
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (isDead || wonLevel)
            {
                return;
            }

            IConsumable consumable = collision.gameObject.GetComponent<IConsumable>();
            if (consumable != null)
            {
                consumable.Consume(this, weaponManager);
            }
        }

        public void ChangeHealth(int healthChange)
        {
            if (isDead || wonLevel)
            {
                return;
            }

            if (health + healthChange >= maxHealth)
            {
                health = maxHealth;
            }
            else if (health + healthChange <= 0)
            {
                health = 0;
                isDead = true;
                hudManager.SetGameHUDVisible(false);
                hudManager.SetGameOverMenuVisible(true);
            }
            else
            {
                health += healthChange;
            }

            hudManager.UpdateHealth(health);
        }

        public void ChangeArmor(int armorChange)
        {
            if (isDead || wonLevel)
            {
                return;
            }

            if (armor + armorChange >= maxArmor)
            {
                armor = maxArmor;
            }
            else if (armor + armorChange <= 0)
            {
                ChangeHealth(armorChange + armor);
                armor = 0;
            }
            else
            {
                armor += armorChange;
            }

            hudManager.UpdateArmor(armor);
        }

        public void ChangeOxygen(int O2Change)
        {
            if (isDead || wonLevel)
            {
                return;
            }

            if (oxygen + O2Change >= maxOxygen)
            {
                oxygen = maxOxygen;
            }
            else if (oxygen + O2Change <= 0)
            {
                oxygen = 0;
                ChangeHealth(-suffocationDamage);
            }
            else
            {
                oxygen += O2Change;
            }

            hudManager.UpdateOxygen(oxygen);
        }

        public void TakeDamage(int damage)
        {
            if (isDead || wonLevel)
            {
                return;
            }

            int healthDamage;
            int armorDamage;

            healthDamage = damage / partsHealthToArmor;
            armorDamage = damage - healthDamage;

            ChangeHealth(-healthDamage);
            ChangeArmor(-armorDamage);

            if (health <= 0)
            {
                isDead = true;
                hudManager.SetGameHUDVisible(false);
                hudManager.SetGameOverMenuVisible(true);
            }
        }

        //detects the void
        private void OnCollisionEnter(Collision collision)
        {
            if (isDead || wonLevel)
            {
                return;
            }

            if (collision.gameObject.CompareTag("Death"))
            {
                TakeDamage(9999);
            }
        }

        public void AddRedKey()
        {
            hasRedKey = true;
        }

        public void AddBlueKey()
        {
            hasBlueKey = true;
        }

        //deprecated
        public void LoadData(GameData data)
        {
            gameObject.transform.position = data.playerPosition;
            gameObject.transform.rotation = Quaternion.Euler(data.playerRotation.x, data.playerRotation.y, data.playerRotation.z);
        }

        public void SaveData(ref GameData data)
        {
            data.playerPosition = gameObject.transform.position;
            data.playerRotation = new Vector3(gameObject.transform.rotation.x, gameObject.transform.rotation.y, gameObject.transform.rotation.z);
        }
    }
}
