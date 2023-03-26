using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;

namespace PlayerObject
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References-----------------------------------------------------------------------------")]
        [SerializeField] private Rigidbody playerRb;
        [SerializeField] private PlayerCamera playerCameraScript;
        [SerializeField] private Transform playerCameraTransform;
        [SerializeField] private CapsuleCollider playerCollider;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform orientationTransform;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PrefsManager prefsManager;
        [SerializeField] private LayerMask environmentMask;

        [Header("Walking-----------------------------------------------------------------------------")]
        [SerializeField] private float walkingVelMultiplier;
        [SerializeField] private bool shiftToWalk;
        public bool walkingInput { get; private set; }
        public int vInput { get; private set; }
        public int hInput { get; private set; }

        [Header("Grounded-----------------------------------------------------------------------------")]
        [SerializeField] private float maxGroundedVel;
        [SerializeField] private float groundDrag;
        [SerializeField] private float groundSnapTolerance;
        [SerializeField] private int maxGroundAngle;
        [SerializeField] private float groundSnapVel;
        [SerializeField] private float groundSnapCooldown;
        private bool doGroundSnapCooldown;
        private float movingTime;
        [SerializeField] private float maxAccelerationTime;

        [Header("In The Air-----------------------------------------------------------------------------")]
        [SerializeField] private float gravityMagnitude;
        private float maxAirVel;
        [SerializeField] private float airSpeed;
        [SerializeField] private float airDrag;
        [SerializeField] private float backwardAirSpeed;

        [Header("Environment Detection-----------------------------------------------------------------------------")]
        [SerializeField] private float groundHitLength;
        private RaycastHit groundHit;
        [SerializeField] private bool isGrounded;

        [Header("Jumping-----------------------------------------------------------------------------")]
        [SerializeField] private float jumpForce;
        private bool jumpingUpStage;
        private bool fallingDownStage;
        private bool jumpInput;
        [SerializeField] private float fallingThreshold;

        [Header("Crouching-----------------------------------------------------------------------------")]
        [SerializeField] private float crouchHeight;
        [SerializeField] private float normalHeight;
        [SerializeField] private float groundedCrouchForce;
        [SerializeField] private float crouchSpeedMultiplier;
        [SerializeField] private float crouchLerpSpeed;
        [SerializeField] private float headClearanceRadius;
        private bool groundedCrouch;
        public bool crouchInput { get; private set; }

        [Header("Water-----------------------------------------------------------------------------")]
        [SerializeField] private float waterDrag;
        [SerializeField] private float maxWaterVel;
        [SerializeField] private float waterDownForce;
        public bool inWater { get; private set; }
        private int upwardsInput;

        [Header("Ladder-----------------------------------------------------------------------------")]
        [SerializeField] private float maxLadderVel;
        [SerializeField] private float ladderDrag;
        [SerializeField] private float ladderLeaveForce;
        [SerializeField] private float ladderJumpForce;
        [SerializeField] private float ladderFirstMoveDelay;
        private bool ladderCanMove;
        private bool ladderJumpInput;
        private Vector3 ladderDirection;
        private bool onLadder;
        private bool ladderLeave;

        [Header("NoClip-----------------------------------------------------------------------------")]
        [SerializeField] private float maxNoClipVel;
        [SerializeField] private float noClipDrag;
        private bool doNoClip;

        private enum MovementState
        {
            GROUNDED,
            AIR,
            LADDER,
            WATER,
            NOCLIP,
            STUNNED
        }

        private MovementState movementState;

        private void Start()
        {
            Physics.gravity = Physics.gravity.ReplaceField(newY: -gravityMagnitude);
        }

        private void Update()
        {
            if (playerInventory.isDead || playerInventory.wonLevel || prefsManager.settingsOpen)
            {
                return;
            }

            groundHitLength = playerCollider.height * 0.5f * transform.localScale.y + 0.1f;
            isGrounded = Physics.Raycast(orientationTransform.position, -orientationTransform.up, out groundHit, groundHitLength, environmentMask);
            GetInput();

            DetermineJumpState();

            DetermineMovementState();
        }

        private void FixedUpdate()
        {
            if (playerInventory.isDead || playerInventory.wonLevel || prefsManager.settingsOpen)
            {
                return;
            }

            switch (movementState)
            {
                case MovementState.GROUNDED:
                    MoveGround();
                    Jumping();
                    Crouching();
                    break;
                case MovementState.AIR:
                    MoveAir();
                    Crouching();
                    break;
                case MovementState.LADDER:
                    MoveLadder();
                    LadderJump();
                    Crouching();
                    break;
                case MovementState.WATER:
                    MoveWater();
                    Crouching();
                    break;
                case MovementState.NOCLIP:
                    MoveNoClip();
                    break;
                case MovementState.STUNNED:
                    //intentionally blank for now
                    break;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ladder"))
            {
                playerRb.velocity = Vector3.zero;
                ladderLeave = true;
                ladderCanMove = false;
                Invoke(nameof(StartLadderMove), ladderFirstMoveDelay);
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ladder"))
            {
                onLadder = true;
                ladderDirection = collision.gameObject.transform.position.ReplaceField(newY: orientationTransform.position.y) - orientationTransform.position;
                ladderDirection.Normalize();
                //ladderDirection returns without vertical component
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ladder"))
            {
                onLadder = false;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.CompareTag("Water"))
            {
                inWater = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Water"))
            {
                inWater = false;
            }
        }

        private void DetermineJumpState()
        {
            if (jumpingUpStage && playerRb.velocity.y <= 0 && !isGrounded)
            {
                fallingDownStage = true;
                jumpingUpStage = false;
            }

            if (fallingDownStage && isGrounded)
            {
                fallingDownStage = false;
                jumpingUpStage = false;
                doGroundSnapCooldown = true;
                StartCoroutine(ResetGroundSnap());
            }

            if (!jumpingUpStage && playerRb.velocity.y < -fallingThreshold && !fallingDownStage && !isGrounded)
            {
                fallingDownStage = true;
            }
        }

        private void DetermineMovementState()
        {
            if (doNoClip)
            {
                movementState = MovementState.NOCLIP;
            }
            else if (inWater)
            {
                movementState = MovementState.WATER;
            }
            else if (onLadder)
            {
                movementState = MovementState.LADDER;
            }
            else if (isGrounded || OnGroundSnap())
            {
                movementState = MovementState.GROUNDED;
            }
            else
            {
                movementState = MovementState.AIR;
            }
        }

        private void GetInput()
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                doNoClip = !doNoClip;
                playerRb.velocity = Vector3.zero;
                playerCollider.isTrigger = doNoClip;
            }

            if (inWater || doNoClip)
            {
                if (Input.GetKey(KeyCode.Space))
                {
                    upwardsInput = 1;
                }
                else if (Input.GetKey(KeyCode.LeftControl))
                {
                    upwardsInput = -1;
                }
                else
                {
                    upwardsInput = 0;
                }
            }

            if (Input.GetKeyDown(KeyCode.Space) && onLadder && !ladderJumpInput && ladderCanMove)
            {
                ladderJumpInput = true;
            }

            if (Input.GetKeyDown(KeyCode.Space) && !jumpInput && !inWater && isGrounded)
            {
                jumpInput = true;
            }

            if (Input.GetKey(KeyCode.Space) && !jumpInput && groundedCrouch && !inWater)
            {
                groundedCrouch = false;
                jumpInput = true;
                Jumping();
            }

            if (Input.GetKey(KeyCode.Space) && OnGroundSnap() && !inWater)
            {
                jumpInput = true;
            }

            if (Input.GetKey(KeyCode.LeftShift) && !crouchInput && !inWater)
            {
                walkingInput = shiftToWalk ? true : false;
            }
            else
            {
                walkingInput = shiftToWalk ? false : true;
            }

            if (Input.GetKeyDown(KeyCode.LeftControl) && !inWater)
            {
                if (isGrounded)
                {
                    groundedCrouch = true;
                }

                crouchInput = true;
            }

            if (!Input.GetKey(KeyCode.LeftControl) && !inWater)
            {
                if (!Physics.CheckSphere(orientationTransform.position + normalHeight * orientationTransform.up, headClearanceRadius, environmentMask))
                {
                    crouchInput = false;
                    groundedCrouch = false;
                }
            }

            if (Input.GetKey(KeyCode.W))
            {
                vInput = 1;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                vInput = -1;
            }
            else
            {
                vInput = 0;
            }

            if (Input.GetKey(KeyCode.D))
            {
                hInput = 1;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                hInput = -1;
            }
            else
            {
                hInput = 0;
            }
        }

        private void MoveNoClip()
        {
            playerRb.useGravity = false;
            playerRb.drag = noClipDrag;

            Vector3 moveD = vInput * orientationTransform.forward + hInput * orientationTransform.right + upwardsInput * orientationTransform.up;
            moveD.Normalize();

            if (playerRb.velocity.magnitude < maxNoClipVel)
            {
                playerRb.velocity += (maxNoClipVel - playerRb.velocity.magnitude) * moveD;
            }
        }

        private void StartLadderMove()
        {
            ladderCanMove = true;
        }

        private void MoveLadder()
        {
            if (!ladderCanMove)
            {
                return;
            }

            playerRb.useGravity = false;
            playerRb.drag = ladderDrag;

            float ladderAngle = Vector3.SignedAngle(ladderDirection, orientationTransform.forward, orientationTransform.up);

            Vector3 moveD;
            if (ladderAngle >= -45f && ladderAngle <= 45f)
            {
                //ladder to the front
                moveD = vInput * orientationTransform.up + hInput * orientationTransform.right;
                if (isGrounded && vInput == -1 && ladderLeave)
                {
                    ladderLeave = false;
                    playerRb.AddForce(-ladderLeaveForce * ladderDirection, ForceMode.Impulse);
                }
            }
            else if (ladderAngle > 45f && ladderAngle < 135f)
            {
                //to the left
                moveD = -hInput * orientationTransform.up + vInput * orientationTransform.right;
                if (isGrounded && hInput == 1 && ladderLeave)
                {
                    ladderLeave = false;
                    playerRb.AddForce(-ladderLeaveForce * ladderDirection, ForceMode.Impulse);
                }
            }
            else if (ladderAngle > -135f && ladderAngle < -45f)
            {
                //right
                moveD = hInput * orientationTransform.up + vInput * orientationTransform.right;
                if (isGrounded && hInput == -1 && ladderLeave)
                {
                    ladderLeave = false;
                    playerRb.AddForce(-ladderLeaveForce * ladderDirection, ForceMode.Impulse);
                }
            }
            else
            {
                //behind
                moveD = -vInput * orientationTransform.up + hInput * orientationTransform.right;
                if (isGrounded && vInput == 1 && ladderLeave)
                {
                    ladderLeave = false;
                    playerRb.AddForce(-ladderLeaveForce * ladderDirection, ForceMode.Impulse);
                }
            }

            moveD.Normalize();

            if (playerRb.velocity.magnitude < maxLadderVel)
            {
                playerRb.velocity += (maxLadderVel - playerRb.velocity.magnitude) * moveD;
            }
        }

        private void LadderJump()
        {
            if (ladderJumpInput)
            {
                ladderJumpInput = false;

                playerRb.velocity = playerRb.velocity.ReplaceField(newY: 0);

                playerRb.AddForce(-ladderJumpForce * ladderDirection, ForceMode.Impulse);
            }
        }

        private void MoveWater()
        {
            playerRb.useGravity = false;
            playerRb.drag = waterDrag;

            Vector3 moveD = vInput * playerCameraTransform.forward + hInput * playerCameraTransform.right + upwardsInput * orientationTransform.up;
            moveD.Normalize();

            if (playerRb.velocity.magnitude < maxWaterVel)
            {
                playerRb.velocity += (maxWaterVel - playerRb.velocity.magnitude) * moveD;
            }

            if (hInput == 0 && vInput == 0 && upwardsInput == 0)
            {
                playerRb.AddForce(-waterDownForce * orientationTransform.up, ForceMode.Acceleration);
            }
        }

        private void MoveAir()
        {
            fallingDownStage = true;

            playerRb.useGravity = true;
            playerRb.drag = airDrag;

            float sideSpeed = Mathf.Sqrt(playerRb.velocity.x * playerRb.velocity.x + playerRb.velocity.z * playerRb.velocity.z);
            float maxVel = maxAirVel;
            //float maxVel = walkingInput ? maxAirVel * walkingVelMultiplier : (crouchInput ? maxAirVel * crouchSpeedMultiplier : maxAirVel);

            Vector3 moveD = vInput * orientationTransform.forward + hInput * orientationTransform.right;
            moveD.Normalize();

            RaycastHit groundAngleHit;
            if (Physics.Raycast(orientationTransform.position, -orientationTransform.up, out groundAngleHit))
            {
                if (Vector3.Angle(groundAngleHit.normal, Vector3.up) >= maxGroundAngle)
                {
                    moveD = Vector3.zero;
                }
            }

            if (sideSpeed < maxVel)
            {
                playerRb.AddForce(airSpeed * moveD, ForceMode.VelocityChange);
            }
            else
            {
                playerRb.AddForce(backwardAirSpeed * -moveD, ForceMode.Impulse);
            }
        }

        private bool OnGroundSnap()
        {
            if (crouchInput || inWater || onLadder || isGrounded || jumpingUpStage || fallingDownStage)
            {
                return false;
            }

            if (doGroundSnapCooldown)
            {
                return false;
            }

            RaycastHit hit;
            if (Physics.Raycast(orientationTransform.position, -orientationTransform.up, out hit, transform.localScale.y + 0.5f))
            {
                //abort if over water
                if (hit.transform.CompareTag("Water"))
                {
                    return false;
                }
            }

            if (Physics.Raycast(orientationTransform.position, -orientationTransform.up, out hit, environmentMask))
            {
                if (hit.distance > transform.localScale.y && hit.distance < transform.localScale.y + groundSnapTolerance)
                {
                    if (Vector3.Angle(hit.normal, Vector3.up) < maxGroundAngle)
                    {
                        /*
                        if (playerRb.velocity.y > 0)
                        {
                            playerRb.velocity = new Vector3(playerRb.velocity.x, 0f, playerRb.velocity.z);
                        }

                        if (playerRb.velocity.y > -groundSnapVel && playerRb.velocity.y <= 0)
                        {
                            playerRb.velocity -= groundSnapVel * orientationTransform.up;
                        }
                        */

                        if (playerRb.velocity.y > -groundSnapVel && playerRb.velocity.y < 0)
                        {
                            playerRb.velocity -= groundSnapVel * orientationTransform.up;
                        }
                        else if (playerRb.velocity.y >= 0)
                        {
                            playerRb.velocity = new Vector3(playerRb.velocity.x, 0f, playerRb.velocity.z);
                        }

                        doGroundSnapCooldown = true;
                        StartCoroutine(ResetGroundSnap());
                        return true;
                    }

                    return false;
                }

                return false;
            }

            return false;
        }

        private IEnumerator ResetGroundSnap()
        {
            yield return new WaitForSeconds(groundSnapCooldown);
            doGroundSnapCooldown = false;
        }

        private void MoveGround()
        {
            playerRb.useGravity = false;
            playerRb.drag = groundDrag;

            Vector3 moveD = vInput * orientationTransform.forward + hInput * orientationTransform.right;
            moveD = Vector3.ProjectOnPlane(moveD, groundHit.normal);
            moveD.Normalize();

            if (moveD == Vector3.zero)
            {
                movingTime = 0;
            }
            else
            {
                movingTime += Time.deltaTime;
            }

            float maxVel = walkingInput ? maxGroundedVel * walkingVelMultiplier : (crouchInput ? maxGroundedVel * crouchSpeedMultiplier : maxGroundedVel);


            if (movingTime < maxAccelerationTime)
            {
                maxVel *= movingTime / maxAccelerationTime;
            }

            if (playerRb.velocity.magnitude < maxVel)
            {
                playerRb.velocity += (maxVel - playerRb.velocity.magnitude) * moveD;
            }

            maxAirVel = playerRb.velocity.magnitude;
        }

        private void Jumping()
        {
            if (jumpInput)
            {
                jumpingUpStage = true;

                jumpInput = false;
                playerRb.velocity = playerRb.velocity.ReplaceField(newY: 0);

                playerRb.AddForce(jumpForce * orientationTransform.up, ForceMode.Impulse);
            }
        }

        private void Crouching()
        {
            if (onLadder || inWater)
            {
                crouchInput = false;
                playerCollider.height = Mathf.Lerp(playerCollider.height, 2, crouchLerpSpeed * Time.deltaTime);
                cameraTransform.localPosition = cameraTransform.localPosition.ReplaceField(newY: Mathf.Lerp(cameraTransform.localPosition.y, 0.67f, crouchLerpSpeed * Time.deltaTime));
            }
            else if (crouchInput)
            {
                playerCollider.height = Mathf.Lerp(playerCollider.height, 1, crouchLerpSpeed * Time.deltaTime);
                cameraTransform.localPosition = cameraTransform.localPosition.ReplaceField(newY: Mathf.Lerp(cameraTransform.localPosition.y, 0.22f, crouchLerpSpeed * Time.deltaTime));

                if (groundedCrouch)
                {
                    if (playerCollider.height < 1.01f)
                    {
                        groundedCrouch = false;
                    }
                    else if(!isGrounded)
                    {
                        playerRb.AddForce(groundedCrouchForce * -orientationTransform.up, ForceMode.Impulse);
                    }
                }
            }
            else
            {
                playerCollider.height = Mathf.Lerp(playerCollider.height, 2, crouchLerpSpeed * Time.deltaTime);
                cameraTransform.localPosition = cameraTransform.localPosition.ReplaceField(newY: Mathf.Lerp(cameraTransform.localPosition.y, 0.67f, crouchLerpSpeed * Time.deltaTime));
            }
        }

        public void SetShiftToWalk(bool state)
        {
            shiftToWalk = state;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(orientationTransform.position, orientationTransform.position - new Vector3(0, groundHitLength, 0));
            Gizmos.DrawWireSphere(orientationTransform.position + normalHeight * orientationTransform.up, headClearanceRadius);
            //Gizmos.DrawWireSphere(orientationTransform.position - new Vector3(0, normalHeight, 0), 0.1f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(orientationTransform.position + (normalHeight - headClearanceRadius) * 0.5f * playerCollider.height * orientationTransform.up, headClearanceRadius);
            Gizmos.DrawWireSphere(orientationTransform.position - (normalHeight - headClearanceRadius) * 0.5f * playerCollider.height * orientationTransform.up, headClearanceRadius);
        }
    }
}