using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.AI;

public class GuardEnemy : MonoBehaviour
{
    public enum EnemyState { Guard, Waiting, Chase, Attack, PlayerSeenAndLost }

    private NavMeshAgent agent; // Referencia al NavMeshAgent
    private SpriteRenderer spriteRenderer; // Referencia al SpriteRenderer
    private EnemyState currentState; // Estado actual del enemigo
    private bool facingRight = true; // Determina si el enemigo está mirando hacia la derecha
    private Vector2 lastPosition = Vector2.zero; // Última posición para tener referencia de hacia qué dirección se dirige el enemigo
    [SerializeField] private GameObject attackPrefab;
    [SerializeField] private float coolDownTime;

    [SerializeField] private float speed = 1f; // Velocidad inicial del enemigo
    [SerializeField] private Transform player; // Referencia a la posición del jugador
    [SerializeField] private float destinationReachedDistance = 0.68f; // Distancia que determina cuando se ha llegado al destino
    [SerializeField] private float viewRange = 7f; // Rango de visión
    [SerializeField] private LayerMask enemyLayer; // Referencia al layer del enemigo para evitar que colisione con el Raycast de visión
    [SerializeField] private float attackRange = 4f; //Rango de ataque
    [SerializeField] private LayerMask excludeAttackLayers; // Capas que no detecta el ataque
   
    [SerializeField] private float backViewRangeDivider = 3f; // El rango de visión trasera será igual al rango de visión frontal dividida entre este valora 
    [SerializeField] private float searchRadius = 8f; // Radio de busqueda
    [SerializeField] private float searchDuration = 10f; // Tiempo durante el cual el enemigo buscará al jugador
    [SerializeField] private float searchDirectionChangeDuration = 2f; // Tiempo que tardará en cambiar de dirección cuando busca al jugador

    private Vector2 playerTempPosition = Vector2.zero; // Posición en la que se ha detectado al jugador
    private Vector2 viewDirection; // Dirección hacia la que se proyecta el Raycast de visión
    private float tempViewRange; // Rango de visión temporal
    private Vector2 lastPlayerSeenPosition;
    private Vector2 initialPosition;
    private Coroutine attackCoroutine; // Referencia a la corrutina de ataque
    private bool canAttack = true;


    void Start()
    {
        // Inicializa los componentes
        agent = GetComponent<NavMeshAgent>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Configura el Agent para que no se gire
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        // Establece el estado inicial, la posición inicial y la última posición del enemigo
        currentState = EnemyState.Guard;
        initialPosition = transform.position;
        lastPosition = transform.position;
    }

    void Update()
    {
        FlipHorizontally();  
        CheckPlayerInAttackRange();           

        // Establece la última posición del enemigo
        lastPosition = transform.position;

        switch (currentState)
        {
            case EnemyState.Guard:
                Guard();
                break;

            case EnemyState.Attack:
                if (canAttack)
                {
                    Attack();
                }
                break;

            case EnemyState.Chase:
                ChasePlayer(lastPlayerSeenPosition);
                break;

            case EnemyState.PlayerSeenAndLost:
                PlayerSeenAndLost();
                break;
        }
    }

    // Método para manejar el comportamiento del enemigo cuando está de guardia
    void Guard()
    {
        CheckPlayerSeen();
        // Establece como destino la posición inicial del enemigo
        agent.SetDestination(initialPosition);

        // Si ha llegado al destino, gira el sprite
        if (Vector3.Distance(transform.position, initialPosition) <= destinationReachedDistance)
        {
            if (spriteRenderer.flipX)
            {                
                spriteRenderer.flipX = false;
            }
        }
    }

    // Método para manejar el comportamiento del enemigo cuando ataca
    void Attack()
    {
        attackCoroutine = StartCoroutine(AttackEnum());
    }

    IEnumerator AttackEnum()
    {
        canAttack = false;
        currentState = EnemyState.Waiting;
        if (player != null)
        {            
            Instantiate(attackPrefab, player.transform.position, Quaternion.identity);
            yield return new WaitForSeconds(coolDownTime);
            
            if (CheckPlayerInAttackRange() == false)
            {
                if (CheckPlayerSeen())
                {
                    currentState = EnemyState.Chase;
                }
                else
                {
                    currentState = EnemyState.Guard;
                }
            }
            canAttack = true;
        }  
   
    }

    bool CheckPlayerInAttackRange()
    {
        bool inAttackRange = false;
        // Calcula la dirección y la distancia
        Vector2 direction = player.position - (Vector3)transform.position;
        float distance = direction.magnitude;

        // Si el jugador está dentro del rango de ataque
        if (distance <= attackRange)
        {
            // Emite un CircleCast desde la posición del enemigo hacia la posición del jugador
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, attackRange, direction, 0f, ~excludeAttackLayers);

            // Si detecta al jugador
            if (hit.collider != null)
            {
                inAttackRange = true;
                Debug.Log("Player in attack range at: " + player.position);
                playerTempPosition = player.position;

                // Si está buscando al jugador, cancela la búsqueda y ataca
                if (currentState == EnemyState.PlayerSeenAndLost)
                {
                    Debug.Log("Cancelling search due to player detection.");
                    CancelInvoke(nameof(SearchForPlayer));
                    CancelInvoke(nameof(StopSearch));                    
                }
                currentState = EnemyState.Attack;
            }
        }
        return inAttackRange;
    }


    // Método para manejar el comportamiento del enemigo cuando persigue al jugador
    void ChasePlayer(Vector2 lastPosition)
    {
        // Establece como destino la última posición donde ha visto al jugador
        agent.SetDestination(lastPosition);

        // Duplica la velocidad del enemigo
        agent.speed = speed * 2;

        // Si ha llegado a la posición donde ha visto al jugador, cambia el estado a PlayerSeenAndLost
        if (Vector3.Distance(transform.position, lastPosition) <= destinationReachedDistance)
        {
            currentState = EnemyState.PlayerSeenAndLost;
        }
    }

   
    // Método que rota el Sprite en el eje X, dependiendo de la dirección a la que se dirige
    void FlipHorizontally()
    {
        // Si se dirige a la derecha y no está mirando hacia la derecha, establece que está mirando hacia la derecha
        if (transform.position.x > lastPosition.x && !facingRight)
        {
            spriteRenderer.flipX = false;
            facingRight = true;
        }
        // Si se dirige a la izquierda y está mirando hacia la derecha, establece que no está mirando hacia la derecha
        else if (transform.position.x < lastPosition.x && facingRight)
        {
            spriteRenderer.flipX = true;
            facingRight = false;
        }
    }

    // Método para manejar la visión del enemigo
    bool CheckPlayerSeen()
    {
        // Booleano que devuelve el método
        bool playerSeen = false;

        // Dirección hacia el jugador
        viewDirection = ((Vector2)player.position - lastPosition).normalized;

        // Si está mirando hacia la dirección opuesta al jugador, reduce la distancia de visión
        if ((player.position.x < transform.position.x && facingRight) || (player.position.x > transform.position.x && !facingRight))
        {
            tempViewRange = viewRange / backViewRangeDivider;
        }
        else
        {
            tempViewRange = viewRange;
        }
        // Emite el raycast que simula la visión del enemigo, excluyendo las colisiones en la capa del enemigo
        RaycastHit2D hit = Physics2D.Raycast(transform.position, viewDirection, tempViewRange, ~enemyLayer);

        // Si hay colisión
        if (hit)
        {
            // Si puede ver al jugador
            if (hit.collider.gameObject.CompareTag("Player"))
            {
                // Devuelve True
                Debug.Log("Player seen!");
                playerSeen = true;
                lastPlayerSeenPosition = player.position;

                // Si no está en estado Chase, cambia el estado a Chase
                if (currentState != EnemyState.Chase)
                {
                    Debug.Log("Changing state to Chase.");
                    currentState = EnemyState.Chase;
                }
            }
        }
        return playerSeen;
    } 

    void PlayerSeenAndLost()
    {
        // Establece la velocidad del enemigo
        agent.speed = speed;

        // Si no está ejecutándose ya una búsqueda, inicia una
        if (!IsInvoking(nameof(SearchForPlayer)))
        {
            Debug.Log("Player lost, starting search...");
            // Comienza a buscar
            InvokeRepeating(nameof(SearchForPlayer), 0f, searchDirectionChangeDuration);
            // Cancela la búsqueda cuando se cumple el tiempo de búsqueda
            Invoke(nameof(StopSearch), searchDuration);
        }
    }

    void SearchForPlayer()
    {
        Debug.Log("Searching for player...");

        // Genera un punto aleatorio dentro del radio de búsqueda
        Vector2 randomDirection = Random.insideUnitCircle * searchRadius;
        Vector2 searchPoint = (Vector2)lastPlayerSeenPosition + randomDirection;

        // Establece como destino el punto de búsqueda
        agent.SetDestination(searchPoint);

        // Si el jugador entra en el rango de visión mientras busca, cambia a estado Chase
        if (CheckPlayerSeen())
        {
            CancelInvoke(nameof(SearchForPlayer));
            CancelInvoke(nameof(StopSearch));
            Debug.Log("Player found during search, switching to Chase state.");
            currentState = EnemyState.Chase;
        }
    }

    void StopSearch()
    {
        // Cancela la búsqueda y vuelve al estado de patrulla
        CancelInvoke(nameof(SearchForPlayer));
        Debug.Log("Player not found, returning to Guard state.");
        currentState = EnemyState.Guard;
    }
   
    // Método para dibujar los Gizmos
    void OnDrawGizmos()
    {
        // Dibuja el Raycast de la visión
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)viewDirection * tempViewRange);

        // Dibuja un círculo que representa al rango de escucha
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
