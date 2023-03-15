using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropLoot : MonoBehaviour
{
    [SerializeField] private bool randomiseElseDropAll;

    [SerializeField] private GameObject[] lootTable;
    [SerializeField] private int[] lootChances;

    public void InstantiateLoot()
    {
        for (int i = 0; i < lootTable.Length; i++)
        {
            if ((randomiseElseDropAll && CheckLootChancesPass(i)) || !randomiseElseDropAll)
            {
                Instantiate(lootTable[i], transform.position, Quaternion.identity);
            }
        }
    }

    private bool CheckLootChancesPass(int index)
    {
        if (lootChances[index] > Random.Range(0, 100))
        {
            return true;
        }

        return false;
    }
}
