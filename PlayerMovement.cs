/*
 * - Edited by PrzemyslawNowaczyk (11.10.17)
 *   -----------------------------
 *   Deleting unused variables
 *   Changing obsolete methods
 *   Changing used input methods for consistency
 *   -----------------------------
 *
 * - Edited by NovaSurfer (31.01.17).
 *   -----------------------------
 *   Rewriting from JS to C#
 *   Deleting "Spawn" and "Explode" methods, deleting unused varibles
 *   -----------------------------
 * Just some side notes here.
 *
 * - Should keep in mind that idTech's cartisian plane is different to Unity's:
 *    Z axis in idTech is "up/down" but in Unity Z is the local equivalent to
 *    "forward/backward" and Y in Unity is considered "up/down".
 *
 * - Code's mostly ported on a 1 to 1 basis, so some naming convensions are a
 *   bit fucked up right now.
 *
 * - UPS is measured in Unity units, the idTech units DO NOT scale right now.
 *
 * - Default values are accurate and emulates Quake 3's feel with CPM(A) physics.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Contains the command the user wishes upon the character
struct Cmd {
    public float forwardMove;
    public float rightMove;
    public float upMove;
}

public class PlayerMovement : MonoBehaviour {

    [Header("References")]
    public Transform playerView;     // Camera
    [SerializeField] private ProjectileGun shootingScript;

    [Header("Camera")]
    [SerializeField] private float playerViewYOffset = 0.6f; // The height at which the camera is bound to
    [SerializeField] private float xMouseSensitivity = 30.0f;
    [SerializeField] private float yMouseSensitivity = 30.0f;
    private float defaultXMouseSensitivity, defaultYMouseSensitivity;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 15.0f;                // Ground walk speed
    [SerializeField] private float walkSpeed;
    [SerializeField] private float ADSSpeed;                        // Speed while aiming
    [SerializeField] private float runAcceleration = 14.0f;         // Ground accel
    [SerializeField] private float runDeacceleration = 10.0f;       // Deacceleration that occurs when running on the ground
    [SerializeField] private float airAcceleration = 2.0f;          // Air accel (air control)
    [SerializeField] private float airDecceleration = 2.0f;         // Deeacceleration experienced when opposite strafing
    [SerializeField] private float sideStrafeAcceleration = 1.0f;   // How fast acceleration occurs to get up to sideStrafeSpeed 
    [SerializeField] private float sideStrafeSpeed = 0.7f;          // What the max speed to generate when side strafing
    [SerializeField] private float airControl = 0.9f;               // How precise air control is

    public float gravity = 20.0f;

    [SerializeField] private float friction = 6.0f; //Ground friction
    [SerializeField] private bool isMoving;

    [Header("Sprinting")]
    [SerializeField] private float sprintSpeed;
    [SerializeField] private bool sprinting;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Jumping")]
    [SerializeField] private float jumpSpeed = 8.0f;                // The speed at which the character's up axis gains when hitting jump
    private float jumpSpeedWhenCrouched;
    private float initalJumpSpeed;
    public bool isGrounded;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private bool holdJumpToBhop = false;           // When enabled allows player to just hold jump button to keep on bhopping perfectly. Beware: smells like casual.

    [Header("Crouching")]
    [SerializeField] private float changeHeightSpeed = 0.2f;
    [SerializeField] private float crouchSpeed = 9.0f;
    [SerializeField] private float standHeight;
    [SerializeField] private float crouchHeight;
    [SerializeField] private float detectCeilingDistance = 2.0f;
    [SerializeField] private float forceWhenMidAirCrouching;
    [SerializeField] private bool detectedCeiling;
    [SerializeField] private bool HaveCrouchToggled = true;
    [SerializeField] private bool HoldToCrouch; 
    [SerializeField] private bool crouching;
    [SerializeField] private bool stayCrouched;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    bool isCrouched;

    [Header("GUI Style")]
    [SerializeField] private GUIStyle style;

    [Header("Frame Count")]
    [SerializeField] private float fpsDisplayRate = 4.0f; // 4 updates per sec

    private int frameCount = 0;
    private float dt = 0.0f;
    private float fps = 0.0f;

    private CharacterController _controller;

    [Header("Hit Ceiling Fix :)")]
    [SerializeField] 
    float gravityChange = 60.0f;
    public float initalGravity;

    // Camera rotations
    private float rotX = 0.0f;
    private float rotY = 0.0f;
    private float desiredX;

    private Vector3 moveDirectionNorm = Vector3.zero;
    [HideInInspector]
    public Vector3 playerVelocity = Vector3.zero;
    private float playerTopVelocity = 0.0f;

    // Q3: players can queue the next jump just before he hits the ground
    private bool wishJump = false;

    // Used to display real time fricton values
    private float playerFriction = 0.0f;

    // Player commands, stores wish commands that the player asks for (Forward, back, jump, etc)
    private Cmd _cmd;

    private void Start() {
        // Hide the cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (playerView == null) {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                playerView = mainCamera.gameObject.transform;
        }

        // Put the camera inside the capsule collider
        playerView.position = new Vector3(
            transform.position.x,
            transform.position.y + playerViewYOffset,
            transform.position.z);

        defaultXMouseSensitivity = xMouseSensitivity;
        defaultYMouseSensitivity = yMouseSensitivity;

        _controller = GetComponent<CharacterController>();
        shootingScript = GetComponentInChildren<ProjectileGun>();

        standHeight = _controller.height;
        crouchHeight = standHeight / 2f;

        initalGravity = gravity;
        initalJumpSpeed = jumpSpeed;
        jumpSpeedWhenCrouched = (jumpSpeed / 2f);

        walkSpeed = moveSpeed;

        detectedCeiling = false;

    }

    private void Update() {
        // Do FPS calculation
        frameCount++;
        dt += Time.deltaTime;
        if (dt > 1.0 / fpsDisplayRate) {
            fps = Mathf.Round(frameCount / dt);
            frameCount = 0;
            dt -= 1.0f / fpsDisplayRate;
        }

        /* Camera rotation stuff, mouse controls this */
        rotX -= Input.GetAxisRaw("Mouse Y") * xMouseSensitivity * 0.02f;
        rotY += Input.GetAxisRaw("Mouse X") * yMouseSensitivity * 0.02f;

        // Clamp the X rotation
        if (rotX < -90)
            rotX = -90;
        else if (rotX > 90)
            rotX = 90;

        this.transform.rotation = Quaternion.Euler(0, rotY, 0); // Rotates the collider
        playerView.rotation = Quaternion.Euler(rotX, rotY, 0); // Rotates the camera

        // Is Grounded
        if (Physics.Raycast(transform.position, Vector3.down, 2f, whatIsGround)) {
            isGrounded = true;
        } else {
            isGrounded = false;
        }

        /* Movement, here's the important part */
        QueueJump();
        if (_controller.isGrounded)
            GroundMove();
        else if (!_controller.isGrounded)
            AirMove();

        if ((Mathf.Abs(playerVelocity.x)) > 0 || (Mathf.Abs(playerVelocity.z)) > 0) {
            isMoving = true;
        } else {
            isMoving = false;
        }

        // Move the controller
        _controller.Move(playerVelocity * Time.deltaTime);

        /* Calculate top velocity */
        Vector3 udp = playerVelocity;
        udp.y = 0.0f;
        if (udp.magnitude > playerTopVelocity)
            playerTopVelocity = udp.magnitude;

        //Need to move the camera after the player has been moved because otherwise the camera will clip the player if going fast enough and will always be 1 frame behind.
        // Set the camera's position to the transform
        playerView.position = new Vector3(
            transform.position.x,
            transform.position.y + playerViewYOffset,
            transform.position.z);

        // Hit ceiling
        detectedCeiling = Physics.Raycast(transform.position, Vector3.up, detectCeilingDistance, whatIsGround);

        if (detectedCeiling && !_controller.isGrounded) {
            gravity = gravityChange;
            moveSpeed = crouchSpeed;
        } else {
            gravity = initalGravity;
            moveSpeed = walkSpeed;
        }

        if (!crouching) {
            detectCeilingDistance = 5f;
        } else {
            detectCeilingDistance = 3f;
        }

        // Toggle crouch
        if (HaveCrouchToggled) {
            if (Input.GetKeyDown(crouchKey) && !isCrouched) {
                crouching = true;
                isCrouched = true;
            } else if (Input.GetKeyDown(crouchKey) && isCrouched) {
                crouching = false;
                isCrouched = false;
            }
        }

        // Hold to crouch
        if (HoldToCrouch) crouching = Input.GetKey(crouchKey);

        playerView.transform.localPosition = new Vector3(0f, _controller.height, 0f);

        // Sprinting
        Sprinting();
        sprinting = Input.GetKey(sprintKey);

        // Apply gravity
        playerVelocity.y -= gravity * Time.deltaTime;
    }

    private void FixedUpdate() {
        Crouching();
    }

    /*******************************************************************************************************\
   |* MOVEMENT
   \*******************************************************************************************************/

    /// <summary>
    /// Sets the movement direction based on player input
    /// </summary>
    private void SetMovementDir() {
        _cmd.forwardMove = Input.GetAxisRaw("Vertical");
        _cmd.rightMove = Input.GetAxisRaw("Horizontal");
    }

    /// <summary>
    /// Queues the next jump just like in Q3
    /// </summary>
    private void QueueJump() {
        if (holdJumpToBhop) {
            wishJump = Input.GetButton("Jump");
            return;
        }

        if (Input.GetButtonDown("Jump") && !wishJump)
            wishJump = true;
        if (Input.GetButtonUp("Jump"))
            wishJump = false;
    }

    
    /// <summary>
    /// Execs when the player is in the air
    /// </summary>
    private void AirMove() {
        Vector3 wishdir;
        float wishVel = airAcceleration;
        float accel;

        SetMovementDir();

        wishdir = new Vector3(_cmd.rightMove, 0, _cmd.forwardMove);
        wishdir = transform.TransformDirection(wishdir);

        float wishspeed = wishdir.magnitude;
        wishspeed *= moveSpeed;

        wishdir.Normalize();
        moveDirectionNorm = wishdir;

        // CPM: Aircontrol
        float wishspeed2 = wishspeed;
        if (Vector3.Dot(playerVelocity, wishdir) < 0)
            accel = airDecceleration;
        else
            accel = airAcceleration;
        // If the player is ONLY strafing left or right
        if (_cmd.forwardMove == 0 && _cmd.rightMove != 0) {
            if (wishspeed > sideStrafeSpeed)
                wishspeed = sideStrafeSpeed;
            accel = sideStrafeAcceleration;
        }

        Accelerate(wishdir, wishspeed, accel);
        if (airControl > 0)
            AirControl(wishdir, wishspeed2);
    }
    
    /// <summary>
    /// Air control occurs when the player is in the air, it allows
    /// players to move side to sie much faster rather than being
    /// 'sluggish when it comes to cornering.
    /// </summary>
    
    private void AirControl(Vector3 wishdir, float wishspeed) {
        float zspeed;
        float speed;
        float dot;
        float k;

        // Can't control movement if not moving forward or backward
        if (Mathf.Abs(_cmd.forwardMove) < 0.001 || Mathf.Abs(wishspeed) < 0.001)
            return;
        zspeed = playerVelocity.y;
        playerVelocity.y = 0;
        // Next two lines are equivalent to idTech's VectorNormalize() *//*
        speed = playerVelocity.magnitude;
        playerVelocity.Normalize();

        dot = Vector3.Dot(playerVelocity, wishdir);
        k = 32;
        k *= airControl * dot * dot * Time.deltaTime;

        // Change direction while slowing down
        if (dot > 0) {
            playerVelocity.x = playerVelocity.x * speed + wishdir.x * k;
            playerVelocity.y = playerVelocity.y * speed + wishdir.y * k;
            playerVelocity.z = playerVelocity.z * speed + wishdir.z * k;

            playerVelocity.Normalize();
            moveDirectionNorm = playerVelocity;
        }

        playerVelocity.x *= speed;
        playerVelocity.y = zspeed; // Note this line
        playerVelocity.z *= speed;
    }

    /// <summary>
    /// Crouching
    /// </summary>
    float desiredHeight;
    private void Crouching() {
        if (!detectedCeiling) {
            desiredHeight = crouching ? crouchHeight : standHeight;
        }

        if (_controller.height != desiredHeight) {
            AdjustHeight(desiredHeight);

            playerView.transform.localPosition = new Vector3(0, _controller.height, 0);
        }

        if (detectedCeiling && _controller.height == crouchHeight) {
            AdjustHeight(crouchHeight);
        } else {
            AdjustHeight(desiredHeight);
        }

        if (crouching) {
            if (isMoving) {
                jumpSpeed = jumpSpeedWhenCrouched;
                moveSpeed = crouchSpeed;

                if (!isGrounded) {
                    gravity = forceWhenMidAirCrouching;
                } else {
                    gravity = initalGravity;
                }
            }
        } else {
            jumpSpeed = initalJumpSpeed;
        }
    }

    private void AdjustHeight(float height) {
        float center = height / 2f;

        _controller.height = Mathf.Lerp(_controller.height, height, changeHeightSpeed);
        _controller.center = Vector3.Lerp(_controller.center, new Vector3(0, center, 0), changeHeightSpeed);
    }

    /// <summary>
    /// Called every frame when the engine detects that the player is on the ground
    /// </summary>
    private void GroundMove() {
        Vector3 wishdir;

        // Do not apply friction if the player is queueing up the next jump
        if (!wishJump)
            ApplyFriction(1.0f);
        else
            ApplyFriction(0);

        SetMovementDir();

        wishdir = new Vector3(_cmd.rightMove, 0, _cmd.forwardMove);
        wishdir = transform.TransformDirection(wishdir);
        wishdir.Normalize();
        moveDirectionNorm = wishdir;

        var wishspeed = wishdir.magnitude;
        wishspeed *= moveSpeed;

        Accelerate(wishdir, wishspeed, runAcceleration);

        // Reset the gravity velocity
        playerVelocity.y = -gravity * Time.deltaTime;

        if (wishJump) {
            playerVelocity.y = jumpSpeed;
            wishJump = false;
        }
    }

    /// <summary>
    /// Applies friction to the player, called in both the air and on the ground
    /// </summary>
    private void ApplyFriction(float t) {
        Vector3 vec = playerVelocity; // Equivalent to: VectorCopy();
        float speed;
        float newspeed;
        float control;
        float drop;

        vec.y = 0.0f;
        speed = vec.magnitude;
        drop = 0.0f;

        /* Only if the player is on the ground then apply friction */
        if (_controller.isGrounded) {
            control = speed < runDeacceleration ? runDeacceleration : speed;
            drop = control * friction * Time.deltaTime * t;
        }

        newspeed = speed - drop;
        playerFriction = newspeed;
        if (newspeed < 0)
            newspeed = 0;
        if (speed > 0)
            newspeed /= speed;

        playerVelocity.x *= newspeed;
        playerVelocity.z *= newspeed;
    }
    
    /// <summary>
    /// Accelerate in whatever direction we are moving
    /// </summary>
    /// <param name="wishdir"></param>
    /// <param name="wishspeed"></param>
    /// <param name="accel"></param>
    private void Accelerate(Vector3 wishdir, float wishspeed, float accel) {
        float addspeed;
        float accelspeed;
        float currentspeed;

        currentspeed = Vector3.Dot(playerVelocity, wishdir);
        addspeed = wishspeed - currentspeed;
        if (addspeed <= 0)
            return;
        accelspeed = accel * Time.deltaTime * wishspeed;
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        playerVelocity.x += accelspeed * 0.5f * wishdir.x;
        playerVelocity.z += accelspeed * 0.5f * wishdir.z;
    }
    
    /// <summary>
    /// Sprinting
    /// </summary>
    private void Sprinting() {
        var desiredSpeed = sprinting ? sprintSpeed : walkSpeed;

        moveSpeed = desiredSpeed;
    }

    private void OnGUI() {
        GUI.Label(new Rect(0, 0, 400, 100), "FPS: " + fps, style);
        var ups = _controller.velocity;
        ups.y = 0;
        GUI.Label(new Rect(0, 15, 400, 100), "Speed: " + Mathf.Round(ups.magnitude * 100) / 100 + "ups", style);
        GUI.Label(new Rect(0, 30, 400, 100), "Top Speed: " + Mathf.Round(playerTopVelocity * 100) / 100 + "ups", style);
    }
}