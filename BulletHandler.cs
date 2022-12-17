using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletHandler : MonoBehaviour {

    [SerializeField] private bool destroyOnCollision = true;

    private void OnCollisionEnter(Collision collision) {
        if (collision != null) {
            if (destroyOnCollision) {
                Destroy(gameObject, 0.001f);
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other != null) {
            if (destroyOnCollision) {
                Destroy(gameObject, 0.001f);
            }
        }
    }

}
