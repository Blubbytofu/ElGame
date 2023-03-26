using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerObject;

public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] private bool broken;
    [SerializeField] private bool cannotBeDirectlyOpenedByPlayer;
    [SerializeField] private bool openStatus;
    [SerializeField] private bool obstruction;
    [SerializeField] private bool readyToMove;
    [SerializeField] private bool autoClosing;

    [SerializeField] private float doorSpeed;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    [SerializeField] private Vector3 positionOffset;

    [SerializeField] LayerMask playerMask;

    [SerializeField] private Vector3 doorDimensions;
    [SerializeField] private Vector3 doorDimensionsOffset;
    private float startOpenTime;
    [SerializeField] private float totalOpenTime;

    public bool redLocked;
    public bool blueLocked;

    public GameObject redLock;
    public GameObject blueLock;


    private void Start()
    {
        if (openStatus)
        {
            openPosition = transform.position;
            closedPosition = transform.position - positionOffset;
        }
        else
        {
            closedPosition = transform.position;
            openPosition = transform.position + positionOffset;
        }

        if (blueLock != null)
        {
            if (blueLocked)
            {
                blueLock.SetActive(true);
            }
            else
            {
                blueLock.SetActive(false);
            }
        }

        if (redLock != null)
        {
            if (redLocked)
            {
                redLock.SetActive(true);
            }
            else
            {
                redLock.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (openStatus)
        {
            transform.position = Vector3.Lerp(transform.position, openPosition, doorSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, closedPosition, doorSpeed * Time.deltaTime);
        }

        //check if player is in the open position space
        if (Physics.CheckBox(closedPosition + doorDimensionsOffset, doorDimensions, Quaternion.identity, playerMask))
        {
            obstruction = true;
        }
        else
        {
            obstruction = false;
        }

        //check if fully open or closed
        if (Vector3.Distance(transform.position, closedPosition) < 0.1f || Vector3.Distance(transform.position, openPosition) < 0.1f)
        {
            readyToMove = true;
        }
        else
        {
            readyToMove = false;
        }

        if (autoClosing && Time.time > startOpenTime + totalOpenTime && openStatus)
        {
            Interact(gameObject);
        }
    }

    public void Interact(GameObject source)
    {
        if (cannotBeDirectlyOpenedByPlayer && source.CompareTag("MainCamera"))
        {
            return;
        }

        if (!broken && readyToMove && !redLocked && !blueLocked)
        {
            if ((openStatus && !obstruction) || !openStatus)
            {
                openStatus = !openStatus;
                if (openStatus)
                {
                    startOpenTime = Time.time;
                }
            }
            return;
        }

        PlayerInventory playerInventory = source.transform.parent.GetComponentInChildren<PlayerInventory>();
        if (playerInventory == null || (redLock == null && blueLock == null))
        {
            return;
        }

        if (redLocked)
        {
            if (playerInventory.hasRedKey)
            {
                redLocked = false;
                redLock.SetActive(false);
            }
        }

        if (blueLocked)
        {
            if (playerInventory.hasBlueKey)
            {
                blueLocked = false;
                blueLock.SetActive(false);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + doorDimensionsOffset, doorDimensions);
    }
}
