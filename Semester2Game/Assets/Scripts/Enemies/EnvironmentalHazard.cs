using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerObject;

public class EnvironmentalHazard : MonoBehaviour
{
    [SerializeField] private int ticks;
    [SerializeField] private float delay;
    [SerializeField] private int damage;

    private void OnTriggerStay(Collider other)
    {
        PlayerInventory playerInventory = other.GetComponent<PlayerInventory>();

        if (playerInventory != null)
        {
            StartCoroutine(playerInventory.DamageOverTime(ticks, delay, damage));
        }
    }
}
