using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;



public class Enemy : MonoBehaviour
{
    public enum EnemyState { Patrol, Chase, Attack }
    public Transform target;
    NavMeshAgent agent;
    EnemyState currentState;
    bool facingRight = false;
    Vector2 lastPosition = Vector2.zero;
    public Transform patrolSpot1;
    public Transform patrolSpot2;
    Transform currentPatrolSpot;
   
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        currentState = EnemyState.Patrol;
        lastPosition = transform.position;
        currentPatrolSpot = patrolSpot2;
    }
    
    void Update()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;

            case EnemyState.Chase:
                ChasePlayer();
                break;

            case EnemyState.Attack:
                break;
        }

        if (transform.position.x > lastPosition.x && !facingRight) 
        {
            transform.localScale = new Vector3(-1, 1, 1);            
            facingRight = true;
        }
        else if (transform.position.x < lastPosition.x && facingRight)
        {
            transform.localScale = new Vector3(1, 1, 1);
            facingRight = false;
        }

        lastPosition = transform.position;
    }

    void ChasePlayer()
    {
        agent.SetDestination(target.position);
    }

    void Patrol()
    {
        agent.SetDestination(currentPatrolSpot.position);

        if (Vector3.Distance(transform.position, currentPatrolSpot.position) <= 0f)
        {
            if (currentPatrolSpot == patrolSpot1)
            {
                currentPatrolSpot = patrolSpot2;
            }
            else
            {
                currentPatrolSpot = patrolSpot1;
            }
        } 
       
    }
}
