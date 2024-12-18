using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public enum EnemyState { Patrol, Chase, ExploreHeard, Waiting, WarningHeard, PlayerSeenAndLost }

    private NavMeshAgent agent; // Referencia al NavMeshAgent
    private SpriteRenderer spriteRenderer; // Referencia al SpriteRenderer
    private EnemyState currentState; // Estado actual del enemigo
    private bool facingRight = true; // Determina si el enemigo está mirando hacia la derecha
    private Vector2 lastPosition = Vector2.zero; // Última posición para tener referencia de hacia qué dirección se dirige el enemigo
    private Transform currentPatrolSpot; // Destino actual de patrulla


    [SerializeField] private float speed = 1f; // Velocidad inicial del enemigo
    [SerializeField] private Transform player; // Referencia a la posición del jugador
    [SerializeField] private float destinationReachedDistance = 0.9f; // Distancia que determina cuando se ha llegado al destino
    [SerializeField] private Transform patrolSpot1; // Punto de patrulla 1
    [SerializeField] private Transform patrolSpot2; // Punto de patrulla 2
    [SerializeField] private float viewRange = 7f; // Rango de visión
    [SerializeField] private LayerMask enemyLayer; // Referencia al layer del enemigo para evitar que colisione con el Raycast de visión
    [SerializeField] private float hearingRadius = 4f; // Radio de escucha
    [SerializeField] private LayerMask excludeHearingLayers; // Capas que no detecta la escucha
    [SerializeField] private GameObject questionMark; // Símbolo de interrogación que se muestra cuando el enemigo ha escuchado al jugador moverse
    [SerializeField] private float backViewRangeDivider = 3f; // El rango de visión trasera será igual al rango de visión frontal dividida entre este valora 
    [SerializeField] private float searchRadius = 8f; // Radio de busqueda
    [SerializeField] private float searchDuration = 10f; // Tiempo durante el cual el enemigo buscará al jugador
    [SerializeField] private float searchDirectionChangeDuration = 2f; // Tiempo que tardará en cambiar de dirección cuando busca al jugador
    private Vector2 viewDirection; // Dirección hacia la que se proyecta el Raycast de visión
    private Vector2 playerTempPosition = Vector2.zero; // Posición en la que se ha escuchado al jugador
    private float tempViewRange; // Rango de visión temporal
    private Coroutine heardCoroutine; // Referencia a la corrutina que se ejecuta cuando se ha escuchado al jugador moverse
    private Vector2 lastPlayerSeenPosition;


    void Start()
    {
        // Inicializa los componentes
        agent = GetComponent<NavMeshAgent>();
        spriteRenderer = GetComponent<SpriteRenderer>();
       
        // Configura el Agent para que no se gire
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        
        // Establece el estado inicial y la última posición del enemigo
        currentState = EnemyState.Patrol;
        currentPatrolSpot = patrolSpot2;
        lastPosition = transform.position;

        // Se suscribe al evento del jugador cuando se mueve OnPlayerMoving
        Player.OnPlayerMoving += OnPlayerHeard;
    }
    
    void Update()
    {
        FlipHorizontally();

        CheckPlayerSeen();


        // Establece la última posición del enemigo
        lastPosition = transform.position;

        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;

            case EnemyState.Chase:
              ChasePlayer(lastPlayerSeenPosition);
               break;

            case EnemyState.ExploreHeard:
                ExplorePositionHeard(playerTempPosition);
                break;

            case EnemyState.WarningHeard:                
                heardCoroutine = StartCoroutine(WarningHeard());
                break;

            case EnemyState.PlayerSeenAndLost:
                PlayerSeenAndLost();
                break;
        }
    }

    // Método para manejar el comportamiento del enemigo cuando persigue al jugador
    void ChasePlayer(Vector2 lastPosition)
    {
        // Establece como destino la última posición donde ha visto al jugador
        agent.SetDestination(lastPosition);

        // Duplica la velocidad del enemigo
        agent.speed = speed * 2;

        // Si el interrogante está activo, se desactiva
        if (questionMark.activeSelf)
        {
            questionMark.SetActive(false);
        }

        // Si ha llegado a la posición donde ha visto al jugador, cambia el estado a PlayerSeenAndLost
        if (Vector3.Distance(transform.position, lastPosition) <= destinationReachedDistance)
        {
            currentState = EnemyState.PlayerSeenAndLost;
        }
    }

    // Método para manejar el comportamiento del enemigo cuando patrulla
    void Patrol()
    {
        // Establece como destino el punto actual al que tiene que ir
        agent.SetDestination(currentPatrolSpot.position);

        // Establece la velocidad del enemigo
        agent.speed = speed;

        // Si ha llegado al destino, cambia el punto de destino
        if (Vector3.Distance(transform.position, currentPatrolSpot.position) <= destinationReachedDistance)
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
        if ((player.position.x < transform.position.x && facingRight) || (player.position.x > transform.position.x && !facingRight) )
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
                    
                    // Detiene la corrutina de escucha si se está ejecutando
                    if (heardCoroutine != null)
                    {
                        StopCoroutine(heardCoroutine);
                    }
                    currentState = EnemyState.Chase;
                }                
            }
        }
        return playerSeen;
    }
    // Método que está suscrito al evento Player.OnPlayerMoving, que se emite cuando el jugador se mueve 
    void OnPlayerHeard(Vector2 playerPosition)
    {
        // Calcula la dirección y la distancia
        Vector2 direction = playerPosition - (Vector2)transform.position;
        float distance = direction.magnitude;

        // Si el jugador está dentro del rango de escucha
        if (distance <= hearingRadius)
        {
            // Emite un CircleCast desde la posición del enemigo hacia la posición del jugador
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, hearingRadius, direction, 0f, ~excludeHearingLayers);

            // Si escucha al jugador
            if (hit.collider != null)
            {
                Debug.Log("Player heard at: " + playerPosition);
                playerTempPosition = playerPosition;

                // Si está buscando al jugador, cancela la búsqueda y explora el punto escuchado
                if (currentState == EnemyState.PlayerSeenAndLost)
                {
                    Debug.Log("Cancelling search due to noise.");
                    CancelInvoke(nameof(SearchForPlayer));
                    CancelInvoke(nameof(StopSearch));
                    currentState = EnemyState.WarningHeard;
                }
                // Si el enemigo está patrullando, cambia el estado a WarningHeard
                else if (currentState == EnemyState.Patrol)
                {
                    Debug.Log("Changing state to Warning.");
                    currentState = EnemyState.WarningHeard;
                }
            }
        }
    }


    // Corrutina que controla el comportamiento del enemigo cuando escucha moverse al jugador  
    IEnumerator WarningHeard()
    {
        // Cambia el estado a Waiting para que esté en un estado neutro
        currentState = EnemyState.Waiting;

        // Establece como destino la posición del enemigo para que no se mueva
        agent.SetDestination(transform.position);   
        
        // Activa el signo de interrogación
        questionMark.SetActive(true);

        // Espera medio segundo
        yield return new WaitForSeconds(0.5f);

        // Gira el sprite 2 veces
        for (int i = 0; i < 2; i++)
        {
            spriteRenderer.flipX = !spriteRenderer.flipX;
            // Espera medio segundo
            yield return new WaitForSeconds(0.5f);
        }
        // Desativa el signo de interrogación
        questionMark.SetActive(false);

        // Cambia el estado a PlayerHeard
        currentState = EnemyState.ExploreHeard;
    }
    // Método para que el enemigo vaya a explorar el lugar desde donde ha oído moverse al jugador
    void ExplorePositionHeard(Vector2 tempTarget)
    {       
        // Establece como destino la posición donde se ha oído al jugador
        agent.SetDestination(tempTarget);

        // Si ha llegado al destino
        if (Vector3.Distance(transform.position, tempTarget) <= destinationReachedDistance)
        {
            // Cambia el estado a Patrol
            currentState = EnemyState.Patrol;
        }
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
        Debug.Log("Player not found, returning to Patrol state.");
        currentState = EnemyState.Patrol;
    }
    // Método que se ejecuta cuando el enemigo es destruido
    void OnDestroy()
    {
        // Elimina la suscipción al evento Player.OnPlayerMoving
        Player.OnPlayerMoving -= OnPlayerHeard;
    }

    // Método para dibujar los Gizmos
    void OnDrawGizmos()
    {
        // Dibuja el Raycast de la visión
        Gizmos.color = Color.red;        
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)viewDirection * tempViewRange);

        // Dibuja un círculo que representa al rango de escucha
        Gizmos.color = Color.green;   
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
    }
}
