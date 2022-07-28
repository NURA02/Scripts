using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// To-Do List:
/// 1. Have detect radius enlarge after detecting player (done)
/// 2. Death animation/code
/// 3. Enemy shoot 
/// 4. Audio(footsteps, audio, etc. )
/// </summary>

public enum EnemyState {
    PATROL,
    CHASE,
    ATTACK
}

public class EnemyController : MonoBehaviour {

    // Assignables
    private EnemyAnimator enemyAnimator;
    private NavMeshAgent navAgent;

    private EnemyState enemyState;

    // Values
    public float walkSpeed = 0.5f;
    public float run_Speed = 4f;

    public float chaseDistance = 7f;
    private float currentChaseDistance;
    public float attackDistance = 1.8f;
    public float chaseAfterAttackDistance = 2f;

    public float patrolRadiusMin = 20f, patrolRadiusMax = 60f;
    public float patrolForThisTime = 15f;
    private float patrolTimer;

    public float waitBeforeAttack = 2f;
    private float attackTimer;

    public bool playerDetected, bulletDetected;
    public float detectRadius = 5f;

    public bool detectedPlayerInFront;
    public float maxDistanceForBoxCast;

    Collider m_Collider;
    RaycastHit m_Hit;

    private Transform target;
    public LayerMask playerLayer, bulletLayer;

    [Header("Shooting")]
    public bool useGun;
    public float fireRate = 1f;
    public float fireCountdown = 0f;
    public float spread;
    public float shootForce = 5f;

    public Transform attackPoint;
    public GameObject bulletPrefab;

    Vector3 startRot;

    void Awake() {
        enemyAnimator = GetComponent<EnemyAnimator>();
        navAgent = GetComponent<NavMeshAgent>();

        target = GameObject.FindWithTag(Tags.PLAYER).transform;
    }

    void Start() {

        maxDistanceForBoxCast = 300.0f;
        m_Collider = GetComponent<Collider>();

        enemyState = EnemyState.PATROL;
        patrolTimer = patrolForThisTime;
        attackDistance = detectRadius;
        // attack right away
        attackTimer = waitBeforeAttack;

        // Memorize value of chase distance so we can use it again
        currentChaseDistance = chaseDistance;

        startRot = transform.localEulerAngles;
    }

    void Update() {

        if (enemyState == EnemyState.PATROL)
            Patrol();

        if (enemyState == EnemyState.CHASE)
            Chase();

        if (enemyState == EnemyState.ATTACK)
            Attack();

    }

    private void FixedUpdate() {
        detectedPlayerInFront = Physics.BoxCast(m_Collider.bounds.center, transform.localScale, transform.forward, out m_Hit, transform.rotation, maxDistanceForBoxCast);
        if (detectedPlayerInFront) {
            if (m_Hit.collider.name == "Player") {
                enemyState = EnemyState.CHASE;
            }
        }

    }

    void LateUpdate() {
        Vector3 rot = transform.localEulerAngles;
        rot.x = startRot.x; // assign back to original x rotation
        transform.eulerAngles = rot;
    }

    void Patrol() {

        navAgent.isStopped = false;
        navAgent.speed = walkSpeed;

        patrolTimer += Time.deltaTime;

        if (patrolTimer > patrolForThisTime) {
            SetNewRandomDestination();

            patrolTimer = 0f;
        }

        if (navAgent.velocity.sqrMagnitude > 0) {
            enemyAnimator.Walk(true);
        } else {
            enemyAnimator.Walk(false);
        }

        playerDetected = Physics.CheckSphere(transform.position, detectRadius, playerLayer);
        if (playerDetected) {
            Debug.Log("Detected player");
        }

        bulletDetected = Physics.CheckSphere(transform.position, detectRadius, bulletLayer);
        if (bulletDetected) {

            // play spotted audio

            enemyState = EnemyState.CHASE;

            Debug.Log("Detected bullet");
        }

        // Test distance between player and enemy
        if (Vector3.Distance(transform.position, target.position) <= chaseDistance) {

            enemyAnimator.Walk(false);

            enemyState = EnemyState.CHASE;

            // play spotted audio

        }

    }

    void Chase() {

        // Enable agent to move again
        navAgent.isStopped = false;
        navAgent.speed = run_Speed;

        // Set new destination as player's position
        navAgent.SetDestination(target.position);

        if (navAgent.velocity.sqrMagnitude > 0) {
            enemyAnimator.Run(true);
        } else {
            enemyAnimator.Run(false);
        }

        if (!detectedPlayerInFront) {
            if (Vector3.Distance(transform.position, target.position) > chaseDistance) {

                // Cancel animations
                enemyAnimator.Run(false);

                enemyAnimator.Walk(true);

                enemyState = EnemyState.PATROL;
            }
        }

        if (Vector3.Distance(transform.position, target.position) <= attackDistance) {

            // Stop animations
            enemyAnimator.Run(false);
            enemyAnimator.Walk(false);
            enemyState = EnemyState.ATTACK;

            // Reset chase distance to previous
            if (chaseDistance != currentChaseDistance) {
                chaseDistance = currentChaseDistance;
            }

        } else if (Vector3.Distance(transform.position, target.position) <= chaseDistance) {

            // Player run away from enemy
            enemyAnimator.Attack(false);

            // Stop running
            enemyAnimator.Run(true);

            enemyState = EnemyState.CHASE;

            // Reset patrol timer so that the function can calculate the new patrol destination right away
            patrolTimer = patrolForThisTime;

            // Reset chase distance to previous
            if (chaseDistance != currentChaseDistance) {
                chaseDistance = currentChaseDistance;
            }

        }

    }

    void Attack() {

        navAgent.velocity = Vector3.zero;
        navAgent.isStopped = true;

        attackTimer += Time.deltaTime;

        if (attackTimer > waitBeforeAttack) {

            enemyAnimator.Attack(true);

            attackTimer = 0f;

            // play attack sound

        }

        if (useGun) {
            if (fireCountdown <= 0f) {
                Shoot();
                fireCountdown = 1f / fireRate;
            }
        }

        fireCountdown -= Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) <= attackDistance) {
            transform.LookAt(target);
        }

        playerDetected = Physics.CheckSphere(transform.position, detectRadius, playerLayer);
        if (playerDetected) {
            Debug.Log("Detected player");
        }

        if (Vector3.Distance(transform.position, target.position) > attackDistance + chaseAfterAttackDistance) {
            enemyState = EnemyState.CHASE;
        }

    }

    void SetNewRandomDestination() {

        float randRadius = Random.Range(patrolRadiusMin, patrolRadiusMax);

        Vector3 randDir = Random.insideUnitSphere * randRadius;
        randDir += transform.position;

        NavMeshHit navHit;

        NavMesh.SamplePosition(randDir, out navHit, randRadius, -1);

        navAgent.SetDestination(navHit.position);

    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.blue;
        
        // Check if there has been a hit
        if (detectedPlayerInFront) {

            // Draw a ray from GameObjects position toward the hit
            Gizmos.DrawRay(transform.position, transform.forward * maxDistanceForBoxCast);

            // Draw a cube that extends to where the hit exists
            Gizmos.DrawWireCube(transform.position + transform.forward * maxDistanceForBoxCast, transform.localScale);

        } else { // If there hasn't been a hit, draw ray at the max distance

            Gizmos.DrawRay(transform.position, transform.forward * maxDistanceForBoxCast);

            Gizmos.DrawWireCube(transform.position + transform.forward * maxDistanceForBoxCast, transform.localScale);
        }
    }

    void Shoot() {

        Vector3 targetPoint;

        targetPoint = target.position;

        //Calculate direction from attackPoint to targetPoint
        Vector3 directionWithoutSpread = targetPoint - attackPoint.position;

        //Calculate spread
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);

        //Calculate new direction with spread
        Vector3 directionWithSpread = directionWithoutSpread + new Vector3(x, y, 0); //Just add spread to last direction

        GameObject currentBullet = Instantiate(bulletPrefab, attackPoint.position, Quaternion.identity);

        //Rotate bullet to shoot direction
        currentBullet.transform.forward = directionWithSpread.normalized;

        //Add forces to bullet
        currentBullet.GetComponent<Rigidbody>().AddForce(directionWithSpread.normalized * shootForce, ForceMode.Impulse);
    }

}
