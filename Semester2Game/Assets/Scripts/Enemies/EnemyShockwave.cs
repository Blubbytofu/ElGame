using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShockwave : MonoBehaviour
{
    void Start()
    {
        transform.localScale = new Vector3(0.01f, 1, 0.01f);
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1, 1, 1), 4 * Time.deltaTime);
    }
}
