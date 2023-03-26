using PlayerObject;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyConsumable : MonoBehaviour, IConsumable
{
    [SerializeField] private bool isRedKey;
    [SerializeField] private bool isBlueKey;

    public void Consume(PlayerInventory playerInventory, WeaponManager weaponManager)
    {
        Destroy(gameObject);

        if (isRedKey)
        {
            playerInventory.AddRedKey();
        }

        if (isBlueKey)
        {
            playerInventory.AddBlueKey();
        }
    }
}
