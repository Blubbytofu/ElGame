using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PlayerObject
{
    public class HUDManager : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PrefsManager prefsManager;
        [SerializeField] private PlayerInventory playerInventory;

        [SerializeField] private GameObject levelIntro;
        [SerializeField] private GameObject gameHUD;
        [SerializeField] private GameObject gameOverMenu;
        [SerializeField] private GameObject gameWinMenu;

        [SerializeField] private TextMeshProUGUI fpsCounter;
        [SerializeField] private TextMeshProUGUI weaponNameText;
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI armorText;
        [SerializeField] private TextMeshProUGUI oxygenText;
        public GameObject oxygenIndicator;
        [SerializeField] private GameObject altIndicator;

        [SerializeField] private TextMeshProUGUI gameCompletionStatsText;

        [SerializeField] private float levelIntroTime;

        [SerializeField] private GameObject poisonIndicator;

        [SerializeField] private GameObject redKeyIndicator;
        [SerializeField] private GameObject blueKeyIndicator;

        [SerializeField] private TextMeshProUGUI textTipText;

        private void Start()
        {
            StartCoroutine(TrackFrames());

            gameHUD.SetActive(false);
            gameOverMenu.SetActive(false);
            gameWinMenu.SetActive(false);

            ShowLevelIntro();
            Invoke(nameof(HideLevelIntro), levelIntroTime);
        }

        private void Update()
        {
            if (levelIntro.activeSelf && (prefsManager.settingsOpen || playerInventory.isDead || playerInventory.wonLevel))
            {
                HideLevelIntro();
            }
        }

        public void SetTextTipText(string newText)
        {
            textTipText.text = newText;
        }

        public void SetRedKeyIndicator(bool newState)
        {
            redKeyIndicator.SetActive(newState);
        }

        public void SetBlueKeyIndicator(bool newState)
        {
            blueKeyIndicator.SetActive(newState);
        }

        public void SetPoisonIndicator(bool newState)
        {
            poisonIndicator.SetActive(newState);
        }

        private void ShowLevelIntro()
        {
            levelIntro.SetActive(true);

            if (gameManager != null)
            {
                levelIntro.GetComponent<TextMeshProUGUI>().text = gameManager.GetLevelName();
            }
        }

        private void HideLevelIntro()
        {
            levelIntro.SetActive(false);
            if (prefsManager.settingsOpen || playerInventory.isDead || playerInventory.wonLevel)
            {
                return;
            }

            gameHUD.SetActive(true);
        }

        private IEnumerator TrackFrames()
        {
            while (true)
            {
                int frames = (int)(1 / Time.unscaledDeltaTime);
                fpsCounter.text = "FPS " + frames;
                yield return new WaitForSeconds(1f);
            }
        }

        public void SetGameCompletionTime()
        {
            gameCompletionStatsText.text = gameManager.GetPassedTime();
        }

        public void SetGameHUDVisible(bool state)
        {
            gameHUD.SetActive(state);
        }

        public void SetGameOverMenuVisible(bool state)
        {
            gameOverMenu.SetActive(state);
        }

        public void SetWonGameMenuVisible(bool state)
        {
            gameWinMenu.SetActive(state);
            SetGameCompletionTime();
        }

        public void UpdateWeapon(string weaponName, int currentAmmo, int maxAmmo)
        {
            weaponNameText.text = weaponName;
            ammoText.text = "" + currentAmmo + " / " + maxAmmo;
        }
        public void IndicateAltFire(bool state)
        {
            altIndicator.SetActive(state);
        }

        public void UpdateHealth(int newHealth)
        {
            healthText.text = newHealth.ToString();
        }

        public void UpdateArmor(int newArmor)
        {
            armorText.text = newArmor.ToString();
        }

        public void UpdateOxygen(int newOxygen)
        {
            oxygenText.text = newOxygen.ToString();
        }

        public void ToggleOxygenMonitor(bool state)
        {
            oxygenIndicator.SetActive(state);
        }
    }
}
