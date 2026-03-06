using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieWalk : StateMachineBehaviour
{
    float timer = 0;
    List<Transform> waypoints = new List<Transform>();
    NavMeshAgent agent;
    Transform player;
    //[SerializeField] float chaseArea = 10000000000000;
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponent<NavMeshAgent>();
        timer = 0;
        player = GameObject.FindGameObjectWithTag("Player").transform;
        GameObject go = GameObject.FindGameObjectWithTag("Waypoints");
        foreach (Transform t in go.transform)
        {
            waypoints.Add(t);
        }
        agent.SetDestination(waypoints[Random.Range(0, waypoints.Count)].position);

    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        timer += Time.deltaTime;
        float distance = Vector3.Distance(player.position, animator.transform.position);        
        if (agent.remainingDistance < agent.stoppingDistance)
        {            
            agent.SetDestination(waypoints[Random.Range(0, waypoints.Count)].position);
        }        
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent.SetDestination(agent.transform.position);
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
