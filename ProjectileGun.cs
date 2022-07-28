using UnityEngine;
using TMPro;

public class ProjectileGun : MonoBehaviour {

    // Bullet
    [Header("Prefabs")]
    public GameObject bullet;

    // Bullet force
    [Header("Force")]
    public float shootForce;
    public float upwardForce;

    // Aiming
    [Header("Aiming")]
    public Transform AimDownSightsPos;
    public float desiredAimDuration = 3f;
    private float elapsedTime = 2f;
    public Transform defaultGunPos;
    public float aimSensitivity;
    private MouseLook mouseLook;
    public float aimFOV;
    public float zoomInSpeed;
    private float defaultFOV;
    public bool aiming;
    Vector3 currentPos;

    // Gun stats
    [Header("Gun Stats")]
    public float timeBtwnShooting;
    public float spread;
    public float reloadTime;
    public float timeBtwShots;
    public int magazineSize, bulletsPerTap;
    public bool allowButtonHold;

    int bulletsLeft, bulletsShot;

    // Hipfire Recoil
    [Header("Hipfire Recoil")]
    public float recoilX;
    public float recoilY;
    public float recoilZ;

    // ADS Recoil
    [Header("ADS Recoil")]
    public float aimRecoilX;
    public float aimRecoilY;
    public float aimRecoilZ;

    // Settings
    [Header("Settings")]
    public float snappiness;
    public float returnSpeed;

    // Bools
    [Header("Random Bools")]
    bool shooting, readyToShoot, reloading;
    [Space]
    public bool addUpwardForceToBullet;

    // Reference
    [Header("Reference")]
    public Camera fpsCam;
    public Transform attackPoint;
    public Recoil Recoil_Script;
    public AudioSource shootSound;

    // Graphics
    [Header("Graphics")]
    public GameObject muzzleFlash;
    public TextMeshProUGUI ammunitionDisplay;
    public Animator anim;
    public GameObject bulletHoleGraphic;

    // Bug fixing
    public bool allowInvoke = true;

    private void Awake() {
        // Make sure magazine is full
        bulletsLeft = magazineSize;
        readyToShoot = true;

        defaultFOV = fpsCam.fieldOfView;

        mouseLook = GetComponentInParent<MouseLook>();
    }

    private void Update() {
        MyInput();

        // Set ammo display
        if (ammunitionDisplay != null)
            ammunitionDisplay.SetText(bulletsLeft / bulletsPerTap + " / " + magazineSize / bulletsPerTap);
    }

    private void MyInput() {

        // Check if allowed to hold down button and take coressponding input
        if (allowButtonHold) shooting = Input.GetKey(KeyCode.Mouse0);
        else shooting = Input.GetKeyDown(KeyCode.Mouse0);

        // Reloading
        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !reloading) Reload();
        //Reload automatically when trying to shoot without ammo
        if (readyToShoot && shooting && !reloading && bulletsLeft <= 0) Reload();

        // Shooting
        if (readyToShoot && shooting && !reloading && bulletsLeft > 0) {
            //Set bullets to shot to 0
            bulletsShot = 0;

            Shoot();
        }

        if (Input.GetMouseButton(1)) {
            StartAiming();
            aiming = true;
        } else {
            aiming = false;
            StopAiming();
        }
    }

    private void Shoot() {

        anim.SetBool("Shooting", true);
        anim.SetBool("Reload", false);

        readyToShoot = false;

        // Find the exact hit position using a raycast
        Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));    // Just a ray through the middle of screen
        RaycastHit hit;

        // Play audio clip
        shootSound.Play();

        // Check if ray hits something
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit))
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(75); // Just a point far away from player

        // Calculate direction from attackPoint to targetPoint
        Vector3 directionWithoutSpread = targetPoint - attackPoint.position;

        // Calculate spread
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);

        // Calculate new direction with spread
        Vector3 directionWithSpread = directionWithoutSpread + new Vector3(x, y, 0);

        // Instantiate bullet/projectile
        GameObject currentBullet = Instantiate(bullet, attackPoint.position, Quaternion.identity);

        // Rotate bullet to shoot direction
        currentBullet.transform.forward = directionWithSpread.normalized;

        // Add forces to bullet
        currentBullet.GetComponent<Rigidbody>().AddForce(directionWithSpread.normalized * shootForce, ForceMode.Impulse);

        // Add upward force
        if(addUpwardForceToBullet)
            currentBullet.GetComponent<Rigidbody>().AddForce(fpsCam.transform.up * upwardForce, ForceMode.Impulse);

        Destroy(currentBullet, 3f);

        // Recoil
        Recoil_Script.RecoilFire();

        // Instantiate muzzle flash
        if (muzzleFlash != null)
            MuzzleFlash();

        // Instantiate bullet hole graphic
        Instantiate(bulletHoleGraphic, hit.point, Quaternion.Euler(0, 180, 0));

        bulletsLeft--;
        bulletsShot++;

        // Invoke resetShot function (if not already Invoked) 
        if (allowInvoke) {
            Invoke("ResetShot", timeBtwnShooting);
            allowInvoke = false;
        }

        // If more than one bulletPerTap
        if (bulletsShot < bulletsPerTap && bulletsLeft > 0)
            Invoke("Shoot", timeBtwShots);
    }

    private void ResetShot() {
        readyToShoot = true;
        allowInvoke = true;
    }

    private void Reload() {

        anim.SetBool("Reload", true);
        anim.SetBool("Shooting", false);

        reloading = true;
        Invoke("ReloadFinished", reloadTime);
    }

    private void ReloadFinished() {

        anim.SetBool("Reload", false);
        anim.SetBool("Shooting", true);

        bulletsLeft = magazineSize;
        reloading = false;
    }

    private void MuzzleFlash() {

        // Store instanitated bullet into gameObject
        GameObject currentMuzzleFlash = Instantiate(muzzleFlash, attackPoint.position, Quaternion.identity);

        Destroy(currentMuzzleFlash, 2f);

    }

    private void StartAiming() {

        // Play sound

        // Change camera FOV
        fpsCam.fieldOfView = Mathf.Lerp(fpsCam.fieldOfView, aimFOV, zoomInSpeed * Time.deltaTime);

        // Change position
        float percentageComplete = elapsedTime / desiredAimDuration;

        currentPos = transform.position;

        transform.position = Vector3.Lerp(currentPos, AimDownSightsPos.transform.position, Mathf.SmoothStep(0, 1, percentageComplete));

        // Change mouse sensitivity
        mouseLook.mouseSensitivity = aimSensitivity;
    }

    private void StopAiming() {

        // Play sound

        // Set camera FOV back to default
        fpsCam.fieldOfView = Mathf.Lerp(fpsCam.fieldOfView, defaultFOV, zoomInSpeed * Time.deltaTime);

        // Change back to default position
        float percentageComplete = elapsedTime / desiredAimDuration;

        currentPos = transform.position;

        transform.position = Vector3.Lerp(currentPos, defaultGunPos.transform.position, Mathf.SmoothStep(0, 1, percentageComplete));

        // Change mouse sensitivity back to default
        mouseLook.mouseSensitivity = mouseLook.defaultMouseSensitivity;
    }
}