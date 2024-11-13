using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public enum EnemyState { Patrol, Chase, Attack }
    private NavMeshAgent agent;
    private EnemyState currentState;
    private bool facingRight = false;
    private Vector2 lastPosition = Vector2.zero;
    private Transform currentPatrolSpot;
    public bool playerDetected;
    public bool wallDetected;
    [SerializeField] private Transform target;
    [SerializeField] private float changeDirectionDistance = 0.9f;
    [SerializeField] private Transform patrolSpot1;
    [SerializeField] private Transform patrolSpot2;
    [SerializeField] private float rayDistance;
    [SerializeField] private LayerMask enemyLayer;
    private Vector2 raycastDirection;

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

        //if (!wallDetected && playerDetected)
        //{
        //    Debug.Log("Changing state to chase");
        //    currentState = EnemyState.Chase;
        //}
        //else if (wallDetected)
        //{
        //    Debug.Log("Changing state to patrol");
        //    currentState = EnemyState.Patrol;
        //}

        // Dirección hacia el jugador
        raycastDirection = ((Vector2)target.position - lastPosition).normalized;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, raycastDirection, rayDistance, ~enemyLayer);        
    }

    void ChasePlayer()
    {
        agent.SetDestination(target.position);
    }

    void Patrol()
    {
        agent.SetDestination(currentPatrolSpot.position);

        if (Vector3.Distance(transform.position, currentPatrolSpot.position) <= 0.9f)
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

    void OnDrawGizmos()
    {
        // Dibuja el gizmo del raycast
        Gizmos.color = Color.red;        
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)raycastDirection * rayDistance);    
    }
}
