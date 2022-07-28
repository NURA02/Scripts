using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {
    public bool allowProjectileRicochet;

    private void OnTriggerEnter(Collider other) {
        if (!allowProjectileRicochet) {
            Destroy(gameObject, 0.001f);
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if (!allowProjectileRicochet) {
            Destroy(gameObject, 0.001f);
        }
    }
}