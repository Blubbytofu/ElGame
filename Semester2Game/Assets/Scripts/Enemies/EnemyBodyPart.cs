using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;

public class EnemyBodyPart : MonoBehaviour, IDamageable
{
    [SerializeField] private GameObject parentDamageable;
    [field: SerializeField] public float damageMultiplier { get; private set; }

    [SerializeField] private bool facePlayer;
    [SerializeField] private bool lerpFace;
    [SerializeField] private float lerpSpeed;
    [SerializeField] private bool doNotFaceHeight;
    private Transform playerTransform;

    private void Start()
    {
        if (facePlayer)
        {
            playerTransform = GameObject.Find("Player").GetComponent<Transform>();
        }
    }

    private void LateUpdate()
    {
        if (playerTransform != null)
        {
            if (doNotFaceHeight)
            {
                transform.LookAt(playerTransform.position.ReplaceField(newY: transform.position.y));
            }
            else if (lerpFace)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation((playerTransform.position.ReplaceField(newY: transform.position.y) - transform.position).normalized, transform.up), lerpSpeed * Time.deltaTime);
            }
            else
            {
                transform.LookAt(playerTransform);
            }
        }
    }

    public void ReceiveDamage(int damage)
    {
        if (parentDamageable != null)
        {
            parentDamageable.GetComponent<IDamageable>().ReceiveDamage((int)(damage * damageMultiplier));
        }
    }
}
