using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject[] assignedObjects;
    [SerializeField] private bool buttonState;

    [SerializeField] private bool canUseOnce;
    private bool used;

    public void Interact(GameObject source)
    {
        if (canUseOnce)
        {
            if (!used)
            {
                used = true;
                foreach (GameObject obj in assignedObjects)
                {
                    obj.GetComponent<IInteractable>().Interact(gameObject);
                }
                buttonState = !buttonState;
            }
        }
        else
        {
            foreach (GameObject obj in assignedObjects)
            {
                obj.GetComponent<IInteractable>().Interact(gameObject);
            }
            buttonState = !buttonState;
        }
    }
}
