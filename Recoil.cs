using UnityEngine;

public class Recoil : MonoBehaviour {

    // Reference
    [SerializeField] private ProjectileGun projectileGun;

    // Bools
    private bool isAiming;

    // Rotations
    private Vector3 currentRotation;
    private Vector3 targetRotation;

    private void Update() {

        isAiming = projectileGun.aiming;

        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, projectileGun.returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, projectileGun.snappiness * Time.fixedDeltaTime);
        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    public void RecoilFire() {
        if (isAiming) targetRotation += new Vector3(projectileGun.aimRecoilX, Random.Range(-projectileGun.aimRecoilY, projectileGun.aimRecoilY), Random.Range(-projectileGun.aimRecoilZ, projectileGun.aimRecoilZ));
        else targetRotation += new Vector3(projectileGun.recoilX, Random.Range(-projectileGun.recoilY, projectileGun.recoilY), Random.Range(-projectileGun.recoilZ, projectileGun.recoilZ));
    }

}
