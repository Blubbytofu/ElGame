using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerObject;

public class TextTip : MonoBehaviour
{
    [field: SerializeField] public string message { get; private set; }

    [SerializeField] private bool isSecret;
    private bool secretClaimed;
    private GameManager gameManager;

    private void Start()
    {
        if (isSecret)
        {
            gameManager = GameObject.Find("Game Manager").GetComponent<GameManager>();
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (!isSecret)
        {
            return;
        }

        PlayerInventory playerInventory = collision.gameObject.GetComponent<PlayerInventory>();
        if (playerInventory != null && !secretClaimed)
        {
            gameManager.AddSecretFound();
            secretClaimed = true;
        }
    }
}
