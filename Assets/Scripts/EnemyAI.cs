using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour, IDamageable
{
    public enum EnemyState
    {
        Idle,
        Walk,
        Chase,
        Scream,
        Attack,
        Defeated
    }

    // ??? AI Config ???????????????????????????????????????????
    [Header("AI Configuration")]
    public EnemyState currentState;
    public float idleDuration = 2f;
    public float screamDuration = 2f;
    public float attackRange = 5f;
    public float chaseSpeedMultiplier = 1.5f;
    public float damageAmount = 10f;
    public float damageCooldown = 2f;
    public AudioClip screamSound;

    [Header("References")]
    public EnemyVision vision;
    public Transform player;

    [Header("Gun (Optional)")]
    public ParticleSystem muzzleFlash;
    public Transform gunMuzzle;
    public float fireRate = 1f;
    public LayerMask shootMask;
    public AudioClip shootSound;    

    // ??? Waypoints ???????????????????????????????????????????
    [Header("Waypoints")]
    public List<Transform> waypoints;

    // ??? Private ?????????????????????????????????????????????
    private NavMeshAgent agent;
    private Animator anim;
    private PlayerMovement playerMovement;
    private AudioSource audioSource;

    private float idleTimer;
    private float screamTimer;
    private float nextFireTime;
    private float health = 100f;
    private float baseSpeed;
    private bool hasScreamed = false;
    private Transform currentWaypoint;

    // ?????????????????????????????????????????????????????????
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        baseSpeed = agent.speed;

        // Tìm PlayerMovement ?? aim chính xác h?n
        playerMovement = player.GetComponentInParent<PlayerMovement>()
                      ?? FindObjectOfType<PlayerMovement>();

        // Ragdoll: t?t c? rigidbody con b?t ??u kinematic
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;

        // ??t layer Enemy n?u t?n t?i
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) gameObject.layer = enemyLayer;

        if (muzzleFlash != null) muzzleFlash.Stop();

        GoToRandomWaypoint();
        ChangeState(EnemyState.Walk);
    }

    // ?????????????????????????????????????????????????????????
    void Update()
    {
        if (currentState == EnemyState.Defeated) return;

        // ?? Vision detect m?t l?n duy nh?t m?i frame ??
        vision.DetectPlayer(player);

        switch (currentState)
        {
            case EnemyState.Idle: HandleIdle(); break;
            case EnemyState.Walk: HandleWalk(); break;
            case EnemyState.Scream: HandleScream(); break;
            case EnemyState.Chase: HandleChase(); break;
            case EnemyState.Attack: HandleAttack(); break;
        }
    }

    // ??? State Machine ????????????????????????????????????????
    void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        anim.Play(currentState.ToString());

        // Reset t?c ?? v? m?c ??nh, Chase s? t? set l?i
        agent.speed = baseSpeed;
    }

    // ??? Handlers ?????????????????????????????????????????????
    void HandleIdle()
    {
        // Luôn s?n sàng phát hi?n player khi idle
        if (ReactToPlayer()) return;

        idleTimer += Time.deltaTime;
        if (idleTimer >= idleDuration)
        {
            idleTimer = 0f;
            GoToRandomWaypoint();
            ChangeState(EnemyState.Walk);
        }
    }

    void HandleWalk()
    {
        // Luôn s?n sàng phát hi?n player khi patrol
        if (ReactToPlayer()) return;

        if (!agent.pathPending && agent.remainingDistance <= 1.5f)
            ChangeState(EnemyState.Idle);
    }

    void HandleScream()
    {
        agent.isStopped = true;
        LookAtPlayer();
        audioSource.PlayOneShot(screamSound);
        screamTimer += Time.deltaTime;
        if (screamTimer >= screamDuration)
        {
            screamTimer = 0f;
            hasScreamed = true;
            agent.isStopped = false;
            ChangeState(EnemyState.Chase);
        }
    }

    void HandleChase()
    {
        agent.speed = baseSpeed * chaseSpeedMultiplier;
        agent.SetDestination(player.position);

        if (!vision.canSeePlayer)
        {
            agent.speed = baseSpeed;
            GoToRandomWaypoint();
            ChangeState(EnemyState.Walk);
            return;
        }

        if (Vector3.Distance(transform.position, player.position) <= attackRange)
            ChangeState(EnemyState.Attack);
    }

    void HandleAttack()
    {
        agent.SetDestination(transform.position); // ??ng yên
        LookAtPlayer();

        // B?n n?u có súng
        if (muzzleFlash != null && gunMuzzle != null)
        {
            Vector3 targetPos = (playerMovement?.bodyController != null)
            ? playerMovement.bodyController.transform.position
            : player.position;

            targetPos.y += 1f; // Aim vào thân ng??i
            Vector3 dir = (targetPos - gunMuzzle.position).normalized;
            transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 30f, 0f);
            if (Time.time >= nextFireTime)
            {
                muzzleFlash.Play();
                Shoot();
                nextFireTime = Time.time + 1f / fireRate;
            }
            else
            {
                muzzleFlash.Stop();
            }
        }
        else
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            float normalizedTime = stateInfo.normalizedTime % 1f; // % 1f ?? loop

            if (normalizedTime >= 0.5f && Time.time >= nextFireTime)
            {
                Debug.Log("Enemy attacks the player with melee!");
                audioSource.PlayOneShot(shootSound);
                player.GetComponent<IDamageable>()?.TakeDamage(damageAmount);
                nextFireTime = Time.time + stateInfo.length; // ch? h?t 1 vòng m?i trigger l?i
            }
        }

        // N?u player ch?y ra xa thì chase l?i
        if (Vector3.Distance(transform.position, player.position) > attackRange * 1.5f)
            ChangeState(EnemyState.Chase);

        // N?u m?t t?m nhìn thì v? patrol
        if (!vision.canSeePlayer)
        {
            GoToRandomWaypoint();
            ChangeState(EnemyState.Walk);
        }
    }

    // ??? Helpers ??????????????????????????????????????????????

    /// <summary>
    /// G?i ? Idle/Walk ?? ph?n ?ng khi th?y player.
    /// Tr? v? true n?u ?ã ??i state (?? d?ng x? lý ti?p).
    /// </summary>
    bool ReactToPlayer()
    {
        if (!vision.canSeePlayer) return false;

        if (!hasScreamed)
            ChangeState(EnemyState.Scream);
        else
            ChangeState(EnemyState.Chase);

        return true;
    }

    void LookAtPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    void GoToRandomWaypoint()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        currentWaypoint = waypoints[Random.Range(0, waypoints.Count)];
        agent.SetDestination(currentWaypoint.position);
    }

    void Shoot()
    {
        audioSource.PlayOneShot(shootSound);
        Vector3 targetPos = (playerMovement?.bodyController != null)
            ? playerMovement.bodyController.transform.position
            : player.position;

        targetPos.y += 1f; // Aim vào thân ng??i
        Vector3 dir = (targetPos - gunMuzzle.position).normalized;
                

        if (Physics.Raycast(gunMuzzle.position, dir, out RaycastHit hit, 100f, shootMask))
        {
            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log("Enemy hit the player!");
                if (hit.collider.TryGetComponent<IDamageable>(out var dmg))
                    dmg.TakeDamage(damageAmount);
            }
        }
    }

    // ??? IDamageable ??????????????????????????????????????????
    public void TakeDamage(float amount)
    {
        if (currentState == EnemyState.Defeated) return;

        health -= amount;

        if (health <= 0f)
        {
            health = 0f;
            ChangeState(EnemyState.Defeated);
            HandleDefeated();
        }
    }

    void HandleDefeated()
    {
        agent.enabled = false;
        anim.enabled = false;

        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            // Log t?t c? collider info c?a bone này
            foreach (Collider col in rb.GetComponentsInChildren<Collider>())
            {
                if (col is CapsuleCollider cap)
                    Debug.Log($"Bone: {rb.name} | Capsule radius={cap.radius} height={cap.height} scale={rb.transform.lossyScale}");
                else if (col is BoxCollider box)
                    Debug.Log($"Bone: {rb.name} | Box size={box.size} scale={rb.transform.lossyScale}");
                else if (col is SphereCollider sph)
                    Debug.Log($"Bone: {rb.name} | Sphere radius={sph.radius} scale={rb.transform.lossyScale}");
            }

            try
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"FAILED bone: {rb.name} | {e.Message}");
            }
        }
    }
}