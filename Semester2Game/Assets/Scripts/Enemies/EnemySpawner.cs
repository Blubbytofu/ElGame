using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject[] enemies;
    [SerializeField] private Vector3[] correspondingSpawnPoints;

    private bool used;

    public void Interact(GameObject source)
    {
        if (!used)
        {
            used = true;

            int counter = 0;
            foreach (GameObject enemy in enemies)
            {
                Instantiate(enemy, correspondingSpawnPoints[counter], Quaternion.identity);
                counter++;
            }
        }
    }
}
