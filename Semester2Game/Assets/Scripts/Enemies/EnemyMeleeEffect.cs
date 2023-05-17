using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMeleeEffect : MonoBehaviour
{
    [HideInInspector] public Transform enemyTransform;

    private void LateUpdate()
    {
        transform.position = enemyTransform.position;
    }
}
