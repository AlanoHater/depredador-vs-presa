using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

public class AgentGenes 
{
    public bool isHunter;
    public float size;
    public float speed;
    public float stealth;    // Para cazadores: capacidad para acercarse sin ser detectado
    public float camouflage; // Para presas: capacidad para pasar desapercibido
}

public class AgentController : MonoBehaviour
{
    //Data collector
    public string agentId;
    private Vector3 lastPosition;
    private float totalDistanceTraveled = 0;
    private float birthTime;
    private List<float> distanceToOpponentsHistory = new List<float>();

    // Genes y atributos básicos
    public AgentGenes genes;
    public float energy = 100f;
    public float lowEnergyThreshold = 40f;
    public float searchRadius = 30f;
    
    // Referencias de componentes
    [HideInInspector] public NavMeshAgent agent;
    private Renderer agentRenderer;
    
    // Materiales para visualización
    public Material HunterMaterial;
    public Material PreyMaterial;
    
    // Movimiento aleatorio
    private Vector3 randomDirection;
    private float changeDirectionTime = 2f;
    private float timeSinceLastChange = 0f;
    
    // Variables para control de caza
    private float huntCooldown = 0f;
    private float huntTimer = 0f;
    public float maxHuntTime = 6f;
    public float huntCooldownTime = 3f;
    public float captureDistance = 4f;
    
    // Variables para escape
    public float escapeDistance = 5f;
    public float safeEscapeDistance = 1f;
    private Vector3 nearestHunterPosition;
    private bool hunterNearby = false;
    
    // Contadores para fitness
    [HideInInspector] public int successfulHunts = 0;
    [HideInInspector] public int successfulEscapes = 0;

    void Start()
    {
        // Obtener componentes
        agent = GetComponent<NavMeshAgent>();
        agentRenderer = GetComponent<Renderer>();

        InitializeGenes();
        ApplyGeneticAttributes();
        ChangeDirection();
        
        birthTime = Time.time;
        lastPosition = transform.position;
    }

    void InitializeGenes()
    {
        // Si genes es null, asignar valores predeterminados
        if (genes == null)
        {
            genes = new AgentGenes();
            genes.isHunter = (Random.value > 0.5f);
            genes.size = Random.Range(0.5f, 3f);
            genes.speed = Random.Range(1f, 5f);

            // Asignar valores predeterminados para stealth y camouflage
            if (genes.isHunter)
            {
                genes.stealth = Random.Range(2f, 5f);
                genes.camouflage = Random.Range(0.5f, 2f);
            }
            else
            {
                genes.stealth = Random.Range(0.5f, 2f);
                genes.camouflage = Random.Range(2f, 5f);
            }
        }
    }

    void ApplyGeneticAttributes()
    {
        // Aplicar color según si es cazador o presa
        agentRenderer.material = genes.isHunter ? HunterMaterial : PreyMaterial;

        // Asignar atributos genéticos
        agent.speed = genes.speed;
        transform.localScale = Vector3.one * genes.size;
    }

    void Update()
    {
        // Disminuir el cooldown de caza
        if (huntCooldown > 0)
            huntCooldown -= Time.deltaTime;

        // Si la energía llega a 0 o menos, el agente muere
        if (energy <= 0)
        {
            Die();
            return;
        }

        // Lógica de comportamiento
        UpdateBehavior();

        // Calcular distancia recorrida
        totalDistanceTraveled += Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
        
        // Registrar distancia a oponentes cada segundo
        if (Time.frameCount % 60 == 0)
        {
            if (genes.isHunter)
            {
                float distance = FindClosestPreyDistance();
                if (distance < 100) // Valor válido
                    distanceToOpponentsHistory.Add(distance);
            }
            else
            {
                float distance = FindClosestHunterDistance();
                if (distance < 100) // Valor válido
                    distanceToOpponentsHistory.Add(distance);
            }
        }
        
    }

    void UpdateBehavior()
    {
        // Si es una presa, buscar cazadores cercanos
        if (!genes.isHunter)
        {
            CheckForNearbyHunters();
            
            // Escapar si hay un cazador cerca
            if (hunterNearby && Vector3.Distance(transform.position, nearestHunterPosition) < escapeDistance)
            {
                //Debug.Log($"{name} está huyendo de un cazador.");
                Escape(nearestHunterPosition);
                return;
            }
            
            // Buscar comida si la energía es baja
            if (energy <= lowEnergyThreshold)
            {
                //Debug.Log($"{name} está buscando comida debido a baja energía.");
                SearchFood();
                return;
            }
        }
        // Si es cazador y el cooldown ha terminado, intentar cazar
        else if (huntCooldown <= 0)
        {
            //Debug.Log($"{name} está cazando.");
            Hunt();
            return;
        }
        
        // Si no hay comportamiento específico, moverse aleatoriamente
        Move();
    }

    void CheckForNearbyHunters()
    {
        GameObject[] allAgents = GameObject.FindGameObjectsWithTag("Agente");
        GameObject nearestHunter = null;
        float closestDistance = Mathf.Infinity;
        
        foreach (GameObject agentObj in allAgents)
        {
            if (agentObj == this.gameObject)
                continue;
                
            AgentController otherAgent = agentObj.GetComponent<AgentController>();
            if (otherAgent != null && otherAgent.genes != null && otherAgent.genes.isHunter)
            {
                float distance = Vector3.Distance(transform.position, agentObj.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestHunter = agentObj;
                }
            }
        }
        
        hunterNearby = nearestHunter != null && closestDistance < searchRadius;
        if (hunterNearby)
        {
            nearestHunterPosition = nearestHunter.transform.position;
        }
    }

    void Move()
    {
        if (agent.enabled)
        {
            agent.ResetPath();
        }
        
        transform.position += randomDirection * agent.speed * Time.deltaTime;
        timeSinceLastChange += Time.deltaTime;
        
        if (timeSinceLastChange >= changeDirectionTime)
        {
            ChangeDirection();
            energy -= 3; // Reducción de energía al cambiar de dirección
        }
    }

    void SearchFood()
    {
        Collider[] foodColliders = Physics.OverlapSphere(transform.position, searchRadius);
        GameObject closestFood = null;
        float closestDistance = Mathf.Infinity;
        
        foreach (Collider collider in foodColliders)
        {
            if (collider.CompareTag("Food"))
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestFood = collider.gameObject;
                }
            }
        }
        
        if (closestFood != null)
        {
            agent.SetDestination(closestFood.transform.position);
            energy -= 1 * Time.deltaTime;
        }
        else
        {
            Move(); // Si no hay comida, moverse aleatoriamente
        }
    }
    
    void Hunt()
    {
        // Reiniciar el temporizador cuando se inicia la caza
        if (huntTimer <= 0f)
            huntTimer = maxHuntTime;

        GameObject[] allAgents = GameObject.FindGameObjectsWithTag("Agente");
        GameObject closestPrey = null;
        float closestDistance = Mathf.Infinity;
        
        // Encontrar la presa más cercana que sea más pequeña
        foreach (GameObject agentObj in allAgents)
        {
            if (agentObj == this.gameObject)
                continue;
                
            AgentController otherAgent = agentObj.GetComponent<AgentController>();
            if (otherAgent != null && otherAgent.genes != null && !otherAgent.genes.isHunter && otherAgent.genes.size < this.genes.size)
            {
                float distance = Vector3.Distance(transform.position, agentObj.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPrey = agentObj;
                }
            }
        }
        
        if (closestPrey != null) 
        {
            AgentController preyController = closestPrey.GetComponent<AgentController>();
            float hunterPower = genes.stealth + genes.speed + genes.size;
            float preyDefense = preyController.genes.camouflage + preyController.genes.speed + preyController.genes.size;

            agent.SetDestination(closestPrey.transform.position);
            energy -= 3 * Time.deltaTime;
            huntTimer -= Time.deltaTime;

            // Capturar presa si está lo suficientemente cerca y el cazador es más fuerte
            if (closestDistance <= captureDistance && hunterPower > preyDefense)
            {
                float energyGained = preyController.energy * 0.4f;
                energy += energyGained;
                Debug.Log($"{name} ha cazado a {closestPrey.name} y ganó {energyGained} energía.");
                
                // Incrementar contador de cazas exitosas
                successfulHunts++;
                
                preyController.Die();
                huntCooldown = huntCooldownTime;
                huntTimer = 0f;
            }
            // Abandonar la caza si se agota el tiempo o la energía
            else if (huntTimer <= 0f || energy < lowEnergyThreshold)
            {
                Debug.Log($"{name} abandona la caza debido al tiempo o baja energía.");
                huntCooldown = huntCooldownTime;
                huntTimer = 0f;
                Move();
            }
        }
        else
        {
            Move(); // Si no hay presas adecuadas, moverse aleatoriamente
        }
    }
        
    void Escape(Vector3 hunterPosition)
    {
        // Calcular la dirección opuesta al cazador
        Vector3 escapeDirection = (transform.position - hunterPosition).normalized;
        Vector3 escapeTarget = transform.position + escapeDirection * escapeDistance;

        // Moverse hacia la dirección de escape si no está ya en camino
        if (Vector3.Distance(transform.position, escapeTarget) > 1f)
        {
            agent.SetDestination(escapeTarget);
        }

        energy -= 3 * Time.deltaTime; // Mayor gasto de energía al escapar
        
        // Verificar si ha escapado con éxito
        float currentDistance = Vector3.Distance(transform.position, hunterPosition);
        if (currentDistance >= safeEscapeDistance)
        {
            // Recuperar energía al escapar con éxito
            energy += (energy * 0.1f);
            Debug.Log($"{name} ha logrado escapar y deja de huir.");
            
            // Incrementar contador de escapes exitosos
            successfulEscapes++;
            
            ChangeDirection();
        }
    }

    public void Eat(float foodEnergy)
    {
        energy += foodEnergy;
    }
    
    void ChangeDirection()
    {
        randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        timeSinceLastChange = 0f;
    }

    public void Die()
    {
        Debug.Log($"{name} ha muerto.");
        Destroy(gameObject);
    }

    public void RegisterSuccessfulHunt()
    {
        successfulHunts++;
    }

    public void RegisterSuccessfulEscape()
    {
        successfulEscapes++;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Food"))
        {
            Eat(10f);
            Destroy(collision.gameObject);
        }
    }

    private float FindClosestPreyDistance()
    {
        float closestDistance = 100f;
        GameObject[] allAgents = GameObject.FindGameObjectsWithTag("Agente");
        
        foreach (GameObject agent in allAgents)
        {
            if (agent == gameObject) continue;
            
            AgentController agentCtrl = agent.GetComponent<AgentController>();
            if (agentCtrl != null && !agentCtrl.genes.isHunter)
            {
                float distance = Vector3.Distance(transform.position, agent.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }
            }
        }
        
        return closestDistance;
    }

    private float FindClosestHunterDistance()
    {
        float closestDistance = 100f;
        GameObject[] allAgents = GameObject.FindGameObjectsWithTag("Agente");
        
        foreach (GameObject agent in allAgents)
        {
            if (agent == gameObject) continue;
            
            AgentController agentCtrl = agent.GetComponent<AgentController>();
            if (agentCtrl != null && agentCtrl.genes.isHunter)
            {
                float distance = Vector3.Distance(transform.position, agent.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }
            }
        }
        
        return closestDistance;
    }

    // Añadir método para obtener estadísticas del agente
    public Dictionary<string, float> GetAgentStats()
    {
        Dictionary<string, float> stats = new Dictionary<string, float>();
        
        stats["Lifespan"] = Time.time - birthTime;
        stats["TotalDistanceTraveled"] = totalDistanceTraveled;
        stats["AverageSpeed"] = totalDistanceTraveled / (Time.time - birthTime);
        stats["CurrentEnergy"] = energy;
        
        if (distanceToOpponentsHistory.Count > 0)
        {
            stats["AverageDistanceToOpponents"] = distanceToOpponentsHistory.Average(x => (float)x);
            stats["MinDistanceToOpponents"] = distanceToOpponentsHistory.Min();
        }
        
        return stats;
    }
}
