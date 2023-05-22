using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerObject
{
    public class PlayerCamera : MonoBehaviour
    {
        [Header("References-----------------------------------------------------------------------------")]
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PrefsManager prefsManager;
        [SerializeField] private Transform orientation;
        [SerializeField] private Transform cameraPosition;
        [SerializeField] private Transform playerCamera;
        [SerializeField] private Camera primaryCamera;
        [SerializeField] private Camera secondaryCamera;
        RaycastHit interactHit;

        private float mouseX;
        private float mouseY;
        private float xRotation;
        private float yRotation;

        [Header("Player Settings-----------------------------------------------------------------------------")]
        [SerializeField] private float xSensitivity = 2f;
        [SerializeField] private float ySensitivity = 2f;
        [SerializeField] private int FOV;
        [SerializeField] private int secondaryFOV;
        //[HideInInspector] public float zoomFactor;

        [Header("Other-----------------------------------------------------------------------------")]
        [SerializeField] private float headRadius;
        private bool breathingWater;
        [SerializeField] private float interactRange;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            //zoomFactor = 1;
        }

        private void Update()
        {
            if (playerInventory.isDead || playerInventory.wonLevel || prefsManager.settingsOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            TryInteract();
            DetectHead();
            GetChildrenZoom();

            primaryCamera.fieldOfView = FOV; // * (1.0f / zoomFactor);
            secondaryCamera.fieldOfView = secondaryFOV; // * (1.0f / zoomFactor);
        }

        private void LateUpdate()
        {
            if (playerInventory.isDead || playerInventory.wonLevel || prefsManager.settingsOpen)
            {
                return;
            }

            playerCamera.position = cameraPosition.position;

            MouseLook();
        }

        private void MouseLook()
        {
            mouseX = Input.GetAxisRaw("Mouse X");
            mouseY = Input.GetAxisRaw("Mouse Y");

            yRotation += mouseX * xSensitivity;
            xRotation -= mouseY * ySensitivity;

            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            playerCamera.rotation = Quaternion.Euler(xRotation, yRotation, 0);
            orientation.rotation = Quaternion.Euler(0, yRotation, 0);
        }

        private void GetChildrenZoom()
        {

        }

        private void TryInteract()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (Physics.Raycast(transform.position, transform.forward, out interactHit, interactRange))
                {
                    IInteractable interactable = interactHit.transform.gameObject.GetComponent<IInteractable>();
                    if (interactable != null)
                    {
                        interactable.Interact(gameObject);
                    }
                }
            }
        }

        private void DetectHead()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, headRadius);
            foreach (Collider col in colliders)
            {
                if (col.CompareTag("Water"))
                {
                    breathingWater = true;
                    break;
                }
                else
                {
                    breathingWater = false;
                }
            }
        }

        public void SimulateRecoil(float x, float y)
        {
            yRotation += x;
            xRotation -= y;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, headRadius);
        }

        //accessors
        public bool GetBreathingWater()
        {
            return breathingWater;
        }

        //mutators
        public void SetXSens(float newX)
        {
            xSensitivity = newX;
        }

        public void SetYSens(float newY)
        {
            ySensitivity = newY;
        }

        public void SetFOV(float newFOV)
        {
            FOV = (int) newFOV;
        }

        public void SetSecondaryFOV(float newFOV)
        {
            secondaryFOV = (int) newFOV;
        }
    }
}