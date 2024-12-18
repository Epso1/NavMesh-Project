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
    private bool facingRight = true; // Determina si el enemigo est� mirando hacia la derecha
    private Vector2 lastPosition = Vector2.zero; // �ltima posici�n para tener referencia de hacia qu� direcci�n se dirige el enemigo
    [SerializeField] private GameObject attackPrefab;
    [SerializeField] private float coolDownTime;

    [SerializeField] private float speed = 1f; // Velocidad inicial del enemigo
    [SerializeField] private Transform player; // Referencia a la posici�n del jugador
    [SerializeField] private float destinationReachedDistance = 0.68f; // Distancia que determina cuando se ha llegado al destino
    [SerializeField] private float viewRange = 7f; // Rango de visi�n
    [SerializeField] private LayerMask enemyLayer; // Referencia al layer del enemigo para evitar que colisione con el Raycast de visi�n
    [SerializeField] private float attackRange = 4f; //Rango de ataque
    [SerializeField] private LayerMask excludeAttackLayers; // Capas que no detecta el ataque
   
    [SerializeField] private float backViewRangeDivider = 3f; // El rango de visi�n trasera ser� igual al rango de visi�n frontal dividida entre este valora 
    [SerializeField] private float searchRadius = 8f; // Radio de busqueda
    [SerializeField] private float searchDuration = 10f; // Tiempo durante el cual el enemigo buscar� al jugador
    [SerializeField] private float searchDirectionChangeDuration = 2f; // Tiempo que tardar� en cambiar de direcci�n cuando busca al jugador

    private Vector2 playerTempPosition = Vector2.zero; // Posici�n en la que se ha detectado al jugador
    private Vector2 viewDirection; // Direcci�n hacia la que se proyecta el Raycast de visi�n
    private float tempViewRange; // Rango de visi�n temporal
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

        // Establece el estado inicial, la posici�n inicial y la �ltima posici�n del enemigo
        currentState = EnemyState.Guard;
        initialPosition = transform.position;
        lastPosition = transform.position;
    }

    void Update()
    {
        FlipHorizontally();  
        CheckPlayerInAttackRange();           

        // Establece la �ltima posici�n del enemigo
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

    // M�todo para manejar el comportamiento del enemigo cuando est� de guardia
    void Guard()
    {
        CheckPlayerSeen();
        // Establece como destino la posici�n inicial del enemigo
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

    // M�todo para manejar el comportamiento del enemigo cuando ataca
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
        // Calcula la direcci�n y la distancia
        Vector2 direction = player.position - (Vector3)transform.position;
        float distance = direction.magnitude;

        // Si el jugador est� dentro del rango de ataque
        if (distance <= attackRange)
        {
            // Emite un CircleCast desde la posici�n del enemigo hacia la posici�n del jugador
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, attackRange, direction, 0f, ~excludeAttackLayers);

            // Si detecta al jugador
            if (hit.collider != null)
            {
                inAttackRange = true;
                Debug.Log("Player in attack range at: " + player.position);
                playerTempPosition = player.position;

                // Si est� buscando al jugador, cancela la b�squeda y ataca
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


    // M�todo para manejar el comportamiento del enemigo cuando persigue al jugador
    void ChasePlayer(Vector2 lastPosition)
    {
        // Establece como destino la �ltima posici�n donde ha visto al jugador
        agent.SetDestination(lastPosition);

        // Duplica la velocidad del enemigo
        agent.speed = speed * 2;

        // Si ha llegado a la posici�n donde ha visto al jugador, cambia el estado a PlayerSeenAndLost
        if (Vector3.Distance(transform.position, lastPosition) <= destinationReachedDistance)
        {
            currentState = EnemyState.PlayerSeenAndLost;
        }
    }

   
    // M�todo que rota el Sprite en el eje X, dependiendo de la direcci�n a la que se dirige
    void FlipHorizontally()
    {
        // Si se dirige a la derecha y no est� mirando hacia la derecha, establece que est� mirando hacia la derecha
        if (transform.position.x > lastPosition.x && !facingRight)
        {
            spriteRenderer.flipX = false;
            facingRight = true;
        }
        // Si se dirige a la izquierda y est� mirando hacia la derecha, establece que no est� mirando hacia la derecha
        else if (transform.position.x < lastPosition.x && facingRight)
        {
            spriteRenderer.flipX = true;
            facingRight = false;
        }
    }

    // M�todo para manejar la visi�n del enemigo
    bool CheckPlayerSeen()
    {
        // Booleano que devuelve el m�todo
        bool playerSeen = false;

        // Direcci�n hacia el jugador
        viewDirection = ((Vector2)player.position - lastPosition).normalized;

        // Si est� mirando hacia la direcci�n opuesta al jugador, reduce la distancia de visi�n
        if ((player.position.x < transform.position.x && facingRight) || (player.position.x > transform.position.x && !facingRight))
        {
            tempViewRange = viewRange / backViewRangeDivider;
        }
        else
        {
            tempViewRange = viewRange;
        }
        // Emite el raycast que simula la visi�n del enemigo, excluyendo las colisiones en la capa del enemigo
        RaycastHit2D hit = Physics2D.Raycast(transform.position, viewDirection, tempViewRange, ~enemyLayer);

        // Si hay colisi�n
        if (hit)
        {
            // Si puede ver al jugador
            if (hit.collider.gameObject.CompareTag("Player"))
            {
                // Devuelve True
                Debug.Log("Player seen!");
                playerSeen = true;
                lastPlayerSeenPosition = player.position;

                // Si no est� en estado Chase, cambia el estado a Chase
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

        // Si no est� ejecut�ndose ya una b�squeda, inicia una
        if (!IsInvoking(nameof(SearchForPlayer)))
        {
            Debug.Log("Player lost, starting search...");
            // Comienza a buscar
            InvokeRepeating(nameof(SearchForPlayer), 0f, searchDirectionChangeDuration);
            // Cancela la b�squeda cuando se cumple el tiempo de b�squeda
            Invoke(nameof(StopSearch), searchDuration);
        }
    }

    void SearchForPlayer()
    {
        Debug.Log("Searching for player...");

        // Genera un punto aleatorio dentro del radio de b�squeda
        Vector2 randomDirection = Random.insideUnitCircle * searchRadius;
        Vector2 searchPoint = (Vector2)lastPlayerSeenPosition + randomDirection;

        // Establece como destino el punto de b�squeda
        agent.SetDestination(searchPoint);

        // Si el jugador entra en el rango de visi�n mientras busca, cambia a estado Chase
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
        // Cancela la b�squeda y vuelve al estado de patrulla
        CancelInvoke(nameof(SearchForPlayer));
        Debug.Log("Player not found, returning to Guard state.");
        currentState = EnemyState.Guard;
    }
   
    // M�todo para dibujar los Gizmos
    void OnDrawGizmos()
    {
        // Dibuja el Raycast de la visi�n
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)viewDirection * tempViewRange);

        // Dibuja un c�rculo que representa al rango de escucha
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
