using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    // Private references
    private Rigidbody _rb;

    [Header("Camera")]
    [SerializeField, Tooltip("Player's camera")] private Transform playerView;
    [SerializeField] private float xMouseSens;
    [SerializeField] private float yMouseSens;
    float rotX = 0f, rotY = 0f;

    [Header("Movement")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float crouchSpeed;
    private float moveSpeed;
    [SerializeField] private float counterMovement = 2f;
    [SerializeField] private float counterStrafe = 0.175f;
    [SerializeField] private float initiaCounterStrafe;
    [SerializeField] private float threshold = 0.01f;
    // Jumping
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private Transform checkGroundPos;
    [SerializeField] private float jumpForce;
    [SerializeField] private float detectGroundRadius;
    [SerializeField] private bool grounded;
    bool _readyToJump;
    // Crouching
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;
    bool _jumping, _crouching, _sprinting;
    float x, y;


    private void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerScale = transform.localScale;

        moveSpeed = walkSpeed;
        initiaCounterStrafe = counterStrafe;

        _readyToJump = true;

        _rb = GetComponent<Rigidbody>();
    }

    private void Update() {
        MyInput();
        MouseLook();
    }

    private void FixedUpdate() {
        Movement();
    }

    private void MyInput() {
        // Camera rotation
        rotX -= Input.GetAxisRaw("Mouse Y") * xMouseSens * 0.02f;
        rotY += Input.GetAxisRaw("Mouse X") * yMouseSens * 0.02f;

        // Movement
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        _jumping = Input.GetKey(KeyCode.Space);
        _crouching = Input.GetKey(KeyCode.LeftControl);
        _sprinting = Input.GetKey(KeyCode.LeftShift);

        // Crouching
        if (_crouching)
            StartCrouch();
        else
            StopCrouch();

        // Sprinting
        if (_sprinting)
            StartSprint();
        else
            StopSprint();
    }

    private void MouseLook() {
        // Clamp the x rotation
        if (rotX < -90)
            rotX = -90;
        else if (rotX > 90)
            rotX = 90;

        this.transform.rotation = Quaternion.Euler(0, rotY, 0); // Rotates the collider
        playerView.rotation = Quaternion.Euler(rotX, rotY, 0);
    }

    private void Movement() {
        // Extra gravity onto the player
        _rb.AddForce(Vector3.down * Time.deltaTime * 10f);

        // Check if we're grounded
        if (Physics.Raycast(checkGroundPos.position, Vector3.down, detectGroundRadius, whatIsGround)) grounded = true;
        else grounded = false;

        if (_readyToJump && _jumping) Jump();

        // Get actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();

        // Counteract so there is no sloppy movement
        CounterMovement(x, y, mag);

        // Multipliers
        float multiplier = 1f, multiplierV = 1f;

        if (!grounded) {
            multiplier = 0.2f;
            multiplierV = 0.2f;
        }

        // Apply forces in the direction we want to move our player
        _rb.AddForce(gameObject.transform.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
        _rb.AddForce(gameObject.transform.right * x * moveSpeed * Time.deltaTime * multiplier);
    }

    private void Jump() {
        if (grounded) {
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void StartCrouch() {
        transform.localScale = crouchScale;
        if (grounded) _rb.AddForce(Vector3.down * 2f, ForceMode.Impulse);
        moveSpeed = crouchSpeed;
    }
    
    private void StopCrouch() {
        transform.localScale = playerScale;
        moveSpeed = walkSpeed;
    }

    private void StartSprint() {
        moveSpeed = sprintSpeed;
        counterStrafe = 0.15f;
    }

    private void StopSprint() {
        moveSpeed = walkSpeed;
        counterStrafe = initiaCounterStrafe;
    }

    private void CounterMovement(float x, float y, Vector2 mag) {
        if (!grounded || _jumping) return;

        //Counter movement
        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0)) {
            _rb.AddForce(moveSpeed * gameObject.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0)) {
            _rb.AddForce(moveSpeed * gameObject.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }

        // Counter strafe (have zero strafing to make movement more realistic)
        if (x == 1f && y == 1f || x == -1f && y == 1f || x == 1f && y == -1f || x == -1f && y == -1f) {
            _rb.AddForce(moveSpeed * gameObject.transform.right * Time.deltaTime * -mag.x * counterStrafe);
            _rb.AddForce(moveSpeed * gameObject.transform.forward * Time.deltaTime * -mag.y * counterStrafe);
        }
    }

    public Vector2 FindVelRelativeToLook() {
        float lookAngle = gameObject.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(_rb.velocity.x, _rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitude = _rb.velocity.magnitude;
        float xMag = magnitude * Mathf.Cos(v * Mathf.Deg2Rad);
        float yMag = magnitude * Mathf.Cos(u * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(checkGroundPos.position, detectGroundRadius);
    }
}
