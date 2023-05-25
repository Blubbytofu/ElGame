using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject levelSelectionMenu;
    [SerializeField] private GameObject creditsMenu;

    public void ReturnToMain(GameObject parent)
    {
        parent.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void OpenSubMenu(GameObject subMenu)
    {
        mainMenu.SetActive(false);
        subMenu.SetActive(true);
    }

    public void QuitApp()
    {
        Application.Quit();
    }
}
