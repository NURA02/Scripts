using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GravityManipulator : MonoBehaviour {

    [Header("References")]
    private PlayerMovement playerMovement;
    [SerializeField] private Slider slider;

    [Header("Jetpack Stats")]
    [SerializeField] private KeyCode floatKey = KeyCode.Space;
    [SerializeField] private float floatSpeed = 10;
    [SerializeField] private float cooldownTime = 0.6f;
    [SerializeField] private bool canFloat;
    [SerializeField] private bool floating;

    private void Start() {
        playerMovement = GetComponent<PlayerMovement>();

        slider.value = 0f;

        canFloat = true;
    }

    private void Update() {
        // Input
        if (Input.GetKey(floatKey)) Float();

        // Cooldown
        if (startCooldown && playerMovement.isGrounded) {
            // Start cooldown
            slider.value -= (Time.deltaTime * cooldownTime);
            if (slider.value <= slider.minValue) {
                canFloat = true;
                slider.value = 0f;
            }
        }

        if (slider.value == 0f) {
            slider.value = 0f;
        }
    }

    bool startCooldown;
    private void Float() {
        if (canFloat) {
            playerMovement.playerVelocity = new Vector3(playerMovement.playerVelocity.x, floatSpeed, playerMovement.playerVelocity.z);
            // Start timer
            slider.value += Time.deltaTime;
            if (slider.value >= slider.maxValue) {
                canFloat = false;
                startCooldown = true;
            }
        }
    }
}
