using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour, IDamageable
{
    public enum EnemyState {
        Idle,
        Walk,
        Chase,
        Scream,
        Attack,    
        Defeated
    }

    [Header("AI configuration")]
    public EnemyState currentState;    
    public float idleDuration = 3f;
    public float screamDuration = 2f;
    public float attackRange = 5f;
    public ParticleSystem muzzleFlash;
    public EnemyVision vision;
    public Transform player;
    public float fireRate = 1f;
    private float nextFireTime;
    public LayerMask shootMask;
    public Transform gunMuzzle;

    private PlayerMovement playerMovement;


    [Header("Waypoints system")]
    public List<Transform> waypoints; 

    private NavMeshAgent agent;
    private Animator anim;    
    private float idleTimer;
    private float screamTimer;
    private bool hasScreamed = false;
    private float health = 100f;
    private Transform currentTarget;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();        
        GoToRandomWaypoint();

        ChangeState(EnemyState.Walk);
        if(muzzleFlash != null)
            muzzleFlash.Stop();

        // Set default shoot mask to include player and default layers
        shootMask = LayerMask.GetMask("Player", "World");

        // Get player movement for accurate targeting
        playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement == null && player.parent != null)
        {
            playerMovement = player.parent.GetComponent<PlayerMovement>();
        }
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
        }

        // Add collider if missing
        if (GetComponent<Collider>() == null)
        {
            CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1, 0);
            col.height = 2;
            col.radius = 0.5f;
        }

        // Set layer to Enemy if exists
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            gameObject.layer = enemyLayer;
        }
    }

    void Update()
    {        
        if (health <= 0)
        {
            //ChangeState(EnemyState.Defeated);
            return;
        }        
        
        switch (currentState)
        {
            case EnemyState.Idle:
                Debug.Log("hhh" + currentState.ToString());
                HandleIdle();
                break;
            case EnemyState.Walk: 
                Debug.Log("hhh" + currentState.ToString());
                HandleWalk(); 
                break;
            case EnemyState.Chase: 
                Debug.Log("hhh" + currentState.ToString());
                HandleChase(); 
                break;
            case EnemyState.Scream: 
                Debug.Log("hhh" + currentState.ToString());
                HandleScream(); 
                break;
            case EnemyState.Attack: 
                Debug.Log("hhh" + currentState.ToString());
                HandleAttack(); 
                break;
            case EnemyState.Defeated:
                Debug.Log("hhh" + currentState.ToString());
                HandleDefeated();
                break;
        }
        Debug.Log("See Player: " + vision.canSeePlayer);
    }

    public void ChangeState(EnemyState newState)
    {
        if (currentState == newState && newState != EnemyState.Walk) return;

        currentState = newState;        

        anim.Play(currentState.ToString());
    }

    void Shoot()
    {
        if (player == null || gunMuzzle == null) return;

        // Target the player's body position if available, otherwise use player transform
        Vector3 targetPosition = playerMovement != null && playerMovement.bodyController != null 
            ? playerMovement.bodyController.transform.position 
            : player.position;

        // Aim slightly higher for better hit chance on capsule collider
        targetPosition.y += 1f;

        Vector3 directionToPlayer = (targetPosition - gunMuzzle.position).normalized;

        Debug.DrawRay(gunMuzzle.position, directionToPlayer * 100f, Color.red, 1f);
        RaycastHit hit;

        // Align enemy (optional) and raycast with layer mask like SMG.cs
        transform.rotation = Quaternion.LookRotation(directionToPlayer) * Quaternion.Euler(0f, 30f, 0f);

        if (Physics.Raycast(gunMuzzle.position, directionToPlayer, out hit, 100f, shootMask))
        {
            Debug.Log("ENEMY Hit " + hit.collider.name + " at point " + hit.point + " distance " + hit.distance);
            Debug.DrawRay(gunMuzzle.position, directionToPlayer * hit.distance, Color.red, 1f);

            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log("Hit player!");
                // apply damage through interface if present
                if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(10f); // adjust value as needed
                }
            }
        }
        else
        {
            Debug.Log("Enemy raycast: No hit on layers: " + shootMask.value);
            Debug.DrawRay(gunMuzzle.position, directionToPlayer * 100f, Color.red, 1f);
        }
    }
    


    void HandleIdle()
    {
        CheckNearPlayer();
        
        idleTimer += Time.deltaTime;
        if (idleTimer >= idleDuration)
        {
            idleTimer = 0;
            GoToRandomWaypoint();
            ChangeState(EnemyState.Walk);            
        }
    }
    public float reachThreshold = 2f;
    void HandleWalk()
    {
        CheckNearPlayer();
        
        if (!agent.pathPending && agent.remainingDistance <= 2f && currentTarget.CompareTag("Waypoint"))
        {
            Debug.Log("HEHEHEHEHEHE");
            ChangeState(EnemyState.Idle); 
        }
    }
    void LookAtPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            if(muzzleFlash != null && currentState == EnemyState.Attack)
                transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 40f, 0f);
            else
                transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void HandleScream()
    {
        agent.isStopped = true;
        LookAtPlayer();
        screamTimer += Time.deltaTime;
        if (screamTimer >= screamDuration)
        {
            screamTimer = 0;
            FinishScream();
        }             
    }
    
    void FinishScream()
    {
        hasScreamed = true;
        agent.isStopped = false;
        ChangeState(EnemyState.Chase);
    }

    void HandleChase()
    {
        agent.SetDestination(player.position);
        //agent.speed = agent.speed * 1.5f; // increase speed for chasing

        vision.DetectPlayer(player);
        if (!vision.canSeePlayer)
        {
            ChangeState(EnemyState.Walk);
        }
        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            ChangeState(EnemyState.Attack);
        }
    }

    void HandleAttack()
    {
        if (muzzleFlash != null)
        {            
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
        agent.SetDestination(transform.position);

        LookAtPlayer();
        if (Vector3.Distance(transform.position, player.position) > attackRange * 1.5f)
        {
            ChangeState(EnemyState.Chase);
        }
    }
    void HandleDefeated()
    {
            agent.isStopped = true;
            // Play defeated animation or effects here            
    }

        // --- ham ho tro ---

    void CheckNearPlayer()
    {
        vision.DetectPlayer(player);
        if (vision.canSeePlayer)
        {
            if (!hasScreamed) ChangeState(EnemyState.Scream);
            else ChangeState(EnemyState.Chase);
        }
    }

    void GoToRandomWaypoint()
    {
        if (waypoints.Count == 0) return;
        int index = Random.Range(0, waypoints.Count);
        currentTarget = waypoints[index];
        agent.SetDestination(currentTarget.position);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log($"Enemy took {amount} damage, remaining health: {health}");

        if (health <= 0)
        {
            Debug.Log("Enemy defeated!");
            Destroy(gameObject);
        }
    }
}