using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour {

    // Assignables
    private CharacterController controller;
    public Camera fpsCam;

    [Header("Player Stats")]
    public float moveSpeed = 12f;
    public float gravity = -9.81f;
    public float originalSpeed;

    // Sprinting
    [Header("Sprinting")]
    public float sprintSpeed = 16f;
    bool isSprinting;
    public Slider sprintSlider;
    bool canSprint;

    // Jumping
    [Header("Jumping")]
    public float jumpHeight = 3f;
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    bool isGrounded;
    bool canJump;
    public float jumpSpeed;

    Vector3 velocity;
    Vector3 pos;

    private void Awake() {
        originalSpeed = moveSpeed;
        canJump = true;
    }

    private void Start() {
        controller = GetComponent<CharacterController>();
    }

    void Update() {

        MyInput();
        Move();
        Sprint();

        pos = transform.position;

    }

    void MyInput() {

        // Sprinting
        if (Input.GetKeyDown(KeyCode.LeftShift) && canSprint)
            isSprinting = true;

        if (Input.GetKeyUp(KeyCode.LeftShift))
            isSprinting = false;
    }

    void Move() {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0) {
            velocity.y = -1f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * moveSpeed * Time.deltaTime);

        // Jumping
        if (sprintSlider.value == 0f) {
            canJump = false;
        }

        if (sprintSlider.value > 0f) {
            canJump = true;
        }

        if (Input.GetButtonDown("Jump") && isGrounded && canJump) {
            velocity.y = Mathf.Sqrt(jumpHeight * jumpSpeed * -2f * gravity);
            sprintSlider.value -= 50f;
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }
    
    void Sprint() {

        if (isSprinting) {
            moveSpeed = sprintSpeed;
            sprintSlider.value--;        
        }

        if (!isSprinting) {
            moveSpeed = originalSpeed;
            sprintSlider.value++;        }

        if (sprintSlider.value == 0f) {
            moveSpeed = originalSpeed;
        }

        Vector3 currentPos = transform.position;
        if (currentPos != pos) {
            canSprint = true;
        } else {
            canSprint = false;
        }

    }
}
