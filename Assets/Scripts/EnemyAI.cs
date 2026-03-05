using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState {
        Idle,
        Walk,
        Chase,
        Scream
    //Attack, 
    //BeingAttacked, 
    //Defeated
    }

    [Header("AI configuration")]
    public EnemyState currentState;
    public float chaseRange = 10f;
    public float idleDuration = 3f;
    public float screamDuration = 2f;

    [Header("Waypoints system")]
    public List<Transform> waypoints; 

    private NavMeshAgent agent;
    private Animator anim;
    private Transform player;
    private float idleTimer;
    private float screamTimer;
    private bool hasScreamed = false;
    private float health = 100f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        GoToRandomWaypoint();

        ChangeState(EnemyState.Walk);
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
            case EnemyState.Idle: Debug.Log("hhh" + currentState.ToString()); HandleIdle(); break;
            case EnemyState.Walk: Debug.Log("hhh" + currentState.ToString()); HandleWalk(); break;
            case EnemyState.Chase: Debug.Log("hhh" + currentState.ToString()); HandleChase(); break;
            case EnemyState.Scream: Debug.Log("hhh" + currentState.ToString()); HandleScream(); break;
        }
    }

    public void ChangeState(EnemyState newState)
    {
        if (currentState == newState && newState != EnemyState.Walk) return;

        currentState = newState;
                
        anim.Play(currentState.ToString());
    }
    

    void HandleIdle()
    {
        CheckNearPlayer();
        
        idleTimer += Time.deltaTime;
        if (idleTimer >= idleDuration)
        {
            idleTimer = 0;
            ChangeState(EnemyState.Walk);
            GoToRandomWaypoint();
        }
    }
    public float reachThreshold = 2f;
    void HandleWalk()
    {
        CheckNearPlayer();
        
        if (!agent.pathPending && agent.remainingDistance <= 2f)
        {            
            ChangeState(EnemyState.Idle); 
        }
    }

    void HandleScream()
    {
        agent.isStopped = true;
        Vector3 direction = player.position - transform.position;
        direction.y = 0; 
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
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
        
        if (Vector3.Distance(transform.position, player.position) > chaseRange * 1.5f)
        {
            ChangeState(EnemyState.Walk);
        }
    }

    // --- ham ho tro ---

    void CheckNearPlayer()
    {
        if (Vector3.Distance(transform.position, player.position) < chaseRange)
        {
            if (!hasScreamed) ChangeState(EnemyState.Scream);
            else ChangeState(EnemyState.Chase);
        }
    }

    void GoToRandomWaypoint()
    {
        if (waypoints.Count == 0) return;
        int index = Random.Range(0, waypoints.Count);
        agent.SetDestination(waypoints[index].position);
    }
}