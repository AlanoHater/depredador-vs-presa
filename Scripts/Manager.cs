using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class AgentFitness
{
    public AgentGenes genes;
    public float fitnessScore;
    public float lifespan;
    public float energyLevel;
    public float preyDistanceScore;  // Para presas: mayor distancia a cazadores = mejor
    public float hunterDistanceScore; // Para cazadores: menor distancia a presas = mejor
    public int successfulHunts;      // Para cazadores: número de presas cazadas
    public int successfulEscapes;    // Para presas: número de escapes exitosos
    public string agentId;           // ID del agente asignado por el DataCollector
}

public class Manager : MonoBehaviour
{
    private DataCollector dataCollector;
    private float simulationStartTime;

    [Header("Prefabs")]
    public GameObject AgentePrefab;
    public GameObject FoodPrefab;

    [Header("Simulation Settings")]
    public int initialAgents = 5;
    public int initialFood = 30;
    public float generationTime = 10f;

    [Header("Evolution Settings")]
    [Range(0.0f, 1.0f)]
    public float mutationRate = 0.1f;
    [Range(0.0f, 1.0f)]
    public float mutationStrength = 0.2f;
    [Range(0.0f, 1.0f)]
    public float elitePercentage = 0.2f;

    [Header("Spawn Boundaries")]
    private const float SPAWN_X = 35f;
    private const float SPAWN_Z = 18f;

    [Header("UI")]
    public TMP_Text generationText;
    public TMP_Text hunterStatsText;
    public TMP_Text preyStatsText;
    public TMP_Text fitnessStatsText;
    public Toggle showStatsToggle;
    public GameObject statsPanel;


    // Lists to track entities
    private List<GameObject> population = new List<GameObject>();
    private List<GameObject> foodList = new List<GameObject>();
    private List<AgentFitness> fitnessData = new List<AgentFitness>();

    // Generation tracking
    private int generation = 0;
    private float generationCooldown;
    private float generationStartTime;

    // Flag to track if initialization was successful
    private bool simulationInitialized = false;

    void Start()
    {
        CreateUIIfMissing();
        InitializeSimulation();
        // Inicializar DataCollector
        dataCollector = FindFirstObjectByType<DataCollector>();
        if (dataCollector == null)
        {
            GameObject dataCollectorObj = new GameObject("DataCollector");
            dataCollector = dataCollectorObj.AddComponent<DataCollector>();
        }
        
        simulationStartTime = Time.realtimeSinceStartup;
    }

    void CreateUIIfMissing()
    {
        // Si no hay Canvas, crear uno
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Buscar panel existente o crear uno nuevo
        statsPanel = GameObject.Find("StatsPanel");
        if (statsPanel == null)
        {
            statsPanel = new GameObject("StatsPanel");
            statsPanel.transform.SetParent(canvas.transform, false);
            var panelRect = statsPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0.7f);
            panelRect.anchorMax = new Vector2(0.3f, 1);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Añadir componente Image para que sea visible
            var panelImage = statsPanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.5f); // Semi-transparente
        }

        // Buscar o crear textos (usando TextMeshProUGUI)
        generationText = GameObject.Find("GenerationText")?.GetComponent<TMP_Text>();
        if (generationText == null)
        {
            GameObject genTextObj = CreateTextObject("GenerationText", statsPanel.transform);
            generationText = genTextObj.GetComponent<TMP_Text>();
        }

        hunterStatsText = GameObject.Find("HunterStatsText")?.GetComponent<TMP_Text>();
        if (hunterStatsText == null)
        {
            GameObject hunterTextObj = CreateTextObject("HunterStatsText", statsPanel.transform);
            hunterStatsText = hunterTextObj.GetComponent<TMP_Text>();
            hunterTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
        }

        preyStatsText = GameObject.Find("PreyStatsText")?.GetComponent<TMP_Text>();
        if (preyStatsText == null)
        {
            GameObject preyTextObj = CreateTextObject("PreyStatsText", statsPanel.transform);
            preyStatsText = preyTextObj.GetComponent<TMP_Text>();
            preyTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -150);
        }

        fitnessStatsText = GameObject.Find("FitnessStatsText")?.GetComponent<TMP_Text>();
        if (fitnessStatsText == null)
        {
            GameObject fitnessTextObj = CreateTextObject("FitnessStatsText", statsPanel.transform);
            fitnessStatsText = fitnessTextObj.GetComponent<TMP_Text>();
            fitnessTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -250);
        }

        // Buscar o crear toggle
        showStatsToggle = GameObject.Find("ShowStatsToggle")?.GetComponent<Toggle>();
        if (showStatsToggle == null)
        {
            GameObject toggleObj = new GameObject("ShowStatsToggle");
            toggleObj.transform.SetParent(canvas.transform, false);
            showStatsToggle = toggleObj.AddComponent<Toggle>();

            // Crear elementos visuales del toggle
            GameObject background = new GameObject("Background");
            background.transform.SetParent(toggleObj.transform, false);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = Color.white;

            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(background.transform, false);
            Image checkImage = checkmark.AddComponent<Image>();
            checkImage.color = Color.green;

            // Configurar el toggle
            showStatsToggle.targetGraphic = bgImage;
            showStatsToggle.graphic = checkImage;
            showStatsToggle.isOn = true;

            // Posicionar el toggle
            var toggleRect = toggleObj.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0, 1);
            toggleRect.anchorMax = new Vector2(0, 1);
            toggleRect.pivot = new Vector2(0, 1);
            toggleRect.anchoredPosition = new Vector2(10, -10);
            toggleRect.sizeDelta = new Vector2(20, 20);

            // Configurar rectTransforms de los elementos del toggle
            var bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            var checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.sizeDelta = Vector2.zero;
        }
    }

    GameObject CreateTextObject(string name, Transform parent)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        // Usar TextMeshProUGUI en lugar de Text
        TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 14;
        text.color = Color.white;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = new Vector2(0, 30);

        return textObj;
    }

    private float CalculateAverageGene(bool isHunter, System.Func<AgentGenes, float> geneSelector)
    {
        var agents = population
            .Where(a => a != null)
            .Select(a => a.GetComponent<AgentController>())
            .Where(a => a != null && a.genes != null && a.genes.isHunter == isHunter);

        if (!agents.Any())
            return 0;

        return agents.Average(a => geneSelector(a.genes));
    }

    void UpdateUI()
{
    if (!showStatsToggle.isOn)
    {
        statsPanel.SetActive(false);
        return;
    }
    
    statsPanel.SetActive(true);
    generationText.text = $"Generación: {generation}";
    
    // Calcular estadísticas de cazadores
    int hunterCount = population.Count(a => a.GetComponent<AgentController>()?.genes.isHunter ?? false);
    float avgHunterSize = CalculateAverageGene(true, g => g.size);
    float avgHunterSpeed = CalculateAverageGene(true, g => g.speed);
    
    hunterStatsText.text = $"Cazadores: {hunterCount}\n" +
                        $"Tamaño: {avgHunterSize:F2}\n" +
                        $"Velocidad: {avgHunterSpeed:F2}\n" +
                        $"Sigilo: {CalculateAverageGene(true, g => g.stealth):F2}";
    
    // Calcular estadísticas de presas
    int preyCount = population.Count - hunterCount;
    float avgPreySize = CalculateAverageGene(false, g => g.size);
    float avgPreySpeed = CalculateAverageGene(false, g => g.speed);
    
    preyStatsText.text = $"Presas: {preyCount}\n" +
                        $"Tamaño: {avgPreySize:F2}\n" +
                        $"Velocidad: {avgPreySpeed:F2}\n" +
                        $"Camuflaje: {CalculateAverageGene(false, g => g.camouflage):F2}";

    // Calcular y mostrar estadísticas de fitness en la consola
    if (fitnessData.Count > 0)
    {
        // Calcular estadísticas generales de fitness
        float avgFitness = fitnessData.Average(f => f.fitnessScore);
        float maxFitness = fitnessData.Max(f => f.fitnessScore);
        float minFitness = fitnessData.Min(f => f.fitnessScore);

        // Estadísticas separadas para cazadores y presas
        var hunterFitness = fitnessData.Where(f => f.genes.isHunter);
        var preyFitness = fitnessData.Where(f => !f.genes.isHunter);

        float avgHunterFitness = hunterFitness.Any() ? hunterFitness.Average(f => f.fitnessScore) : 0;
        float avgPreyFitness = preyFitness.Any() ? preyFitness.Average(f => f.fitnessScore) : 0;

        // Construir mensaje para la consola
        string fitnessStats = $"\n=== Estadísticas de Fitness (Generación {generation}) ===\n" +
                            $"Promedio General: {avgFitness:F1}\n" +
                            $"Más Alto: {maxFitness:F1}\n" +
                            $"Más Bajo: {minFitness:F1}\n" +
                            $"\nCazadores:\n" +
                            $"Promedio: {avgHunterFitness:F1}\n" +
                            $"Mejor: {(hunterFitness.Any() ? hunterFitness.Max(f => f.fitnessScore) : 0):F1}\n" +
                            $"\nPresas:\n" +
                            $"Promedio: {avgPreyFitness:F1}\n" +
                            $"Mejor: {(preyFitness.Any() ? preyFitness.Max(f => f.fitnessScore) : 0):F1}\n" +
                            "=====================================";

        // Mostrar en la consola solo cuando cambie la generación o cada cierto intervalo
        if (Time.frameCount % 600 == 0) // Actualizar cada 60 frames (aproximadamente 1 segundo a 60 FPS)
        {
            Debug.Log(fitnessStats);
        }
    }
}

    void InitializeSimulation()
{
    LoadPrefabs();

    // Check if prefabs are loaded and initialize simulation
    if (AgentePrefab != null && FoodPrefab != null)
    {
        SpawnAgents();
        SpawnFood();
        generationCooldown = generationTime;
        generationStartTime = Time.time;
        simulationInitialized = true;
        Debug.Log("Simulación inicializada correctamente.");
    }
    else
    {
        Debug.LogError("No se pudieron cargar los prefabs necesarios. Verifica que existen en Resources o asígnalos en el Inspector.");
    }
}

void LoadPrefabs()
{
    // Try to load from Resources only if prefabs are not already assigned in the Inspector
    if (FoodPrefab == null)
    {
        FoodPrefab = LoadPrefabFromResources("FoodPrefab") ??
                    LoadPrefabFromResources("Prefabs/FoodPrefab") ??
                    LoadPrefabFromResources("Food");
    }

    if (AgentePrefab == null)
    {
        AgentePrefab = LoadPrefabFromResources("AgentePrefab") ??
                      LoadPrefabFromResources("Prefabs/AgentePrefab") ??
                      LoadPrefabFromResources("Agente");
    }
}

GameObject LoadPrefabFromResources(string path)
{
    GameObject prefab = Resources.Load<GameObject>(path);
    if (prefab != null)
    {
        Debug.Log($"Prefab cargado desde 'Resources/{path}'");
    }
    return prefab;
}

void SpawnAgents()
{
    CleanupList(population);
    fitnessData.Clear();

    // Si es la primera generación o no hay datos de fitness, crear agentes aleatorios
    if (generation == 0 || fitnessData.Count == 0)
    {
        for (int i = 0; i < initialAgents; i++)
        {
            Vector3 randomPosition = GetRandomSpawnPosition();
            GameObject newAgent = Instantiate(AgentePrefab, randomPosition, Quaternion.identity);
            population.Add(newAgent);

            InitializeAgent(newAgent);
        }
    }
    else
    {
        // Reproducir agentes basados en fitness
        ReproduceAgents();
    }
}

void InitializeAgent(GameObject agent, AgentGenes parentGenes = null, bool isElite = false, string parent1Id = "", string parent2Id = "")
{
    AgentController agentCtrl = agent.GetComponent<AgentController>();
    if (agentCtrl == null) return;

    // Si no hay genes de padre, crear genes aleatorios
    if (parentGenes == null)
    {
        agentCtrl.genes = new AgentGenes
        {
            isHunter = Random.value > 0.5f,
            size = Random.Range(0.5f, 4f),
            speed = Random.Range(1f, 8f)
        };

        // Asignar atributos basados en el rol
        if (agentCtrl.genes.isHunter)
        {
            agentCtrl.genes.stealth = Random.Range(2f, 5f);
            agentCtrl.genes.camouflage = Random.Range(0.5f, 2f);
        }
        else
        {
            agentCtrl.genes.stealth = Random.Range(0.5f, 2f);
            agentCtrl.genes.camouflage = Random.Range(2f, 5f);
        }
    }
    else
    {
        // Copiar genes del padre
        agentCtrl.genes = new AgentGenes
        {
            isHunter = parentGenes.isHunter,
            size = parentGenes.size,
            speed = parentGenes.speed,
            stealth = parentGenes.stealth,
            camouflage = parentGenes.camouflage
        };
    }

    // Aplicar características físicas
    agent.transform.localScale = Vector3.one * agentCtrl.genes.size;
    var navAgent = agent.GetComponent<UnityEngine.AI.NavMeshAgent>();
    if (navAgent != null)
    {
        navAgent.speed = agentCtrl.genes.speed;
    }

    // Inicializar datos de fitness para este agente
    AgentFitness fitness = new AgentFitness
    {
        genes = agentCtrl.genes,
        fitnessScore = 0,
        lifespan = 0,
        energyLevel = agentCtrl.energy,
        preyDistanceScore = 0,
        hunterDistanceScore = 0,
        successfulHunts = 0,
        successfulEscapes = 0
    };

    fitnessData.Add(fitness);

    // Registrar el agente con el DataCollector
    if (dataCollector != null)
    {
        string agentId = dataCollector.RegisterAgent(agent, agentCtrl, isElite, parent1Id, parent2Id);
        agentCtrl.agentId = agentId;
        fitness.agentId = agentId; // Store the agent ID in the fitness data
    }
}


void SpawnFood()
{
    CleanupList(foodList);

    for (int i = 0; i < initialFood; i++)
    {
        Vector3 randomPosition = GetRandomSpawnPosition();
        GameObject newFood = Instantiate(FoodPrefab, randomPosition, Quaternion.identity);
        foodList.Add(newFood);
    }
}

Vector3 GetRandomSpawnPosition()
{
    return new Vector3(
        Random.Range(-SPAWN_X, SPAWN_X),
        0,
        Random.Range(-SPAWN_Z, SPAWN_Z)
    );
}

void CleanupList<T>(List<T> list) where T : Object
{
    list.RemoveAll(item => item == null);
    foreach (var item in list)
    {
        if (item != null)
        {
            Destroy(item);
        }
    }
    list.Clear();
}

void LogFitnessStats()
{
    if (fitnessData.Count == 0) return;

    float avgFitness = fitnessData.Average(f => f.fitnessScore);
    float maxFitness = fitnessData.Max(f => f.fitnessScore);
    float minFitness = fitnessData.Min(f => f.fitnessScore);

    var hunterFitness = fitnessData.Where(f => f.genes.isHunter);
    var preyFitness = fitnessData.Where(f => !f.genes.isHunter);

    float avgHunterFitness = hunterFitness.Any() ? hunterFitness.Average(f => f.fitnessScore) : 0;
    float avgPreyFitness = preyFitness.Any() ? preyFitness.Average(f => f.fitnessScore) : 0;

    string stats = $"\n=== Estadísticas de Fitness (Generación {generation}) ===\n" +
                  $"Población Total: {population.Count}\n" +
                  $"Promedio General: {avgFitness:F1}\n" +
                  $"Más Alto: {maxFitness:F1}\n" +
                  $"Más Bajo: {minFitness:F1}\n" +
                  $"\nCazadores ({hunterFitness.Count()}):\n" +
                  $"Promedio: {avgHunterFitness:F1}\n" +
                  $"Mejor: {(hunterFitness.Any() ? hunterFitness.Max(f => f.fitnessScore) : 0):F1}\n" +
                  $"\nPresas ({preyFitness.Count()}):\n" +
                  $"Promedio: {avgPreyFitness:F1}\n" +
                  $"Mejor: {(preyFitness.Any() ? preyFitness.Max(f => f.fitnessScore) : 0):F1}\n" +
                  "=====================================";

    Debug.Log(stats);
}

void Update()
{
    // Si la simulación no se inicializó correctamente, intentar inicializarla de nuevo
    if (!simulationInitialized)
    {
        LoadPrefabs();
        if (AgentePrefab != null && FoodPrefab != null)
        {
            InitializeSimulation();
        }
        return;
    }

    // Limpiar agentes nulos de la lista de población
    population.RemoveAll(agent => agent == null);

    // Actualizar datos de fitness durante la simulación
    UpdateFitnessData();

    // Actualizar UI
    UpdateUI();
    
    if (Time.frameCount % 600 == 0)
    {
        LogFitnessStats();
    }

    // Actualizar el temporizador de generación
    generationCooldown -= Time.deltaTime;

    // Si el temporizador llega a 0, avanzamos a la siguiente generación
    if (generationCooldown <= 0f)
    {
        StartNewGeneration();
    }
}

void UpdateFitnessData()
{
    // Actualizar datos de fitness para cada agente vivo
    for (int i = 0; i < population.Count; i++)
    {
        if (i >= fitnessData.Count) continue; // Protección contra índices fuera de rango
        
        GameObject agent = population[i];
        if (agent == null) continue;
        
        AgentController agentCtrl = agent.GetComponent<AgentController>();
        if (agentCtrl == null) continue;
        
        // Actualizar datos básicos
        fitnessData[i].energyLevel = agentCtrl.energy; // Energía actual del agente
        fitnessData[i].lifespan = Time.time - generationStartTime; // Tiempo desde que nació el agente
        
        // Actualizar contadores de caza/escape desde el controlador del agente
        fitnessData[i].successfulHunts = agentCtrl.successfulHunts;
        fitnessData[i].successfulEscapes = agentCtrl.successfulEscapes;
        
        // Calcular distancias a cazadores/presas
        if (!agentCtrl.genes.isHunter)
        {
            // Para presas: calcular distancia al cazador más cercano
            float closestHunterDistance = FindClosestHunterDistance(agent);
            fitnessData[i].preyDistanceScore += closestHunterDistance * Time.deltaTime; // Acumular por tiempo
        }
        else
        {
            // Para cazadores: calcular distancia a la presa más cercana
            float closestPreyDistance = FindClosestPreyDistance(agent);
            fitnessData[i].hunterDistanceScore += (100 - closestPreyDistance) * Time.deltaTime; // Acumular por tiempo
        }
        
        // Calcular fitness en tiempo real
        CalculateAgentFitness(fitnessData[i]);
    }
}

void CalculateAgentFitness(AgentFitness data)
{
    // Calcular el costo energético basado en tamaño y velocidad
    float sizeCost = (data.genes.size - 0.5f) * 0.1f; // 0.1 puntos por cada 1.0 de tamaño por encima del mínimo
    float speedCost = (data.genes.speed - 1f) * 0.1f; // 0.1 puntos por cada 1.0 de velocidad por encima del mínimo
    float energyCost = (sizeCost + speedCost) * Time.deltaTime; // Aplicar el costo por tiempo

    // Base de fitness: energía (con costo) y tiempo de vida
    float baseFitness = (data.energyLevel - energyCost) * 0.5f + data.lifespan * 10f;
    
    // Fitness específico por rol
    if (data.genes.isHunter)
    {
        // Para cazadores: priorizar caza exitosa y cercanía a presas
        float huntingScore = data.successfulHunts * 50f;    // Mayor número de cacerías exitosas = mejor
        float proximityScore = data.hunterDistanceScore * 0.2f; // Menor distancia a presas = mejor
        data.fitnessScore = baseFitness + huntingScore + proximityScore;
    }
    else
    {
        // Para presas: priorizar escapes exitosos y distancia a cazadores
        float escapeScore = data.successfulEscapes * 50f;   // Mayor número de escapes exitosos = mejor
        float safetyScore = data.preyDistanceScore * 0.2f; // Mayor distancia a cazadores = mejor
        data.fitnessScore = baseFitness + escapeScore + safetyScore;
    }
    
    // Asegurar que el fitness no sea negativo
    data.fitnessScore = Mathf.Max(0, data.fitnessScore);

}

float FindClosestHunterDistance(GameObject prey)
{
    float closestDistance = 100f; // Valor alto por defecto

    foreach (GameObject agent in population)
    {
        if (agent == prey) continue;

        AgentController agentCtrl = agent.GetComponent<AgentController>();
        if (agentCtrl != null && agentCtrl.genes.isHunter)
        {
            float distance = Vector3.Distance(prey.transform.position, agent.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
            }
        }
    }

    return closestDistance;
}

float FindClosestPreyDistance(GameObject hunter)
{
    float closestDistance = 100f; // Valor alto por defecto

    foreach (GameObject agent in population)
    {
        if (agent == hunter) continue;

        AgentController agentCtrl = agent.GetComponent<AgentController>();
        if (agentCtrl != null && !agentCtrl.genes.isHunter)
        {
            float distance = Vector3.Distance(hunter.transform.position, agent.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
            }
        }
    }

    return closestDistance;
}

void CalculateFitness()
{
    // El cálculo individual ya se hace en UpdateFitnessData
    // Aquí solo registramos los mejores para depuración
    
    var bestHunter = fitnessData.Where(f => f.genes.isHunter).OrderByDescending(f => f.fitnessScore).FirstOrDefault();
    var bestPrey = fitnessData.Where(f => !f.genes.isHunter).OrderByDescending(f => f.fitnessScore).FirstOrDefault();
    
    if (bestHunter != null)
        Debug.Log($"Mejor cazador - Fitness: {bestHunter.fitnessScore:F1}, Caza exitosa: {bestHunter.successfulHunts}");
    
    if (bestPrey != null)
        Debug.Log($"Mejor presa - Fitness: {bestPrey.fitnessScore:F1}, Escapes: {bestPrey.successfulEscapes}");
}

void ReproduceAgents()
{
    // Calcular fitness final para esta generación
    CalculateFitness();

    // Separar cazadores y presas para reproducción independiente
    List<AgentFitness> hunters = fitnessData.Where(f => f.genes.isHunter).ToList();
    List<AgentFitness> preys = fitnessData.Where(f => !f.genes.isHunter).ToList();

    // Ordenar por fitness (mayor a menor)
    hunters = hunters.OrderByDescending(f => f.fitnessScore).ToList();
    preys = preys.OrderByDescending(f => f.fitnessScore).ToList();

    // Determinar cuántos agentes de cada tipo crear
    int hunterCount = Mathf.Max(2, initialAgents / 2);
    int preyCount = initialAgents - hunterCount;

    // Crear nuevos agentes basados en los mejores de la generación anterior
    CreateOffspring(hunters, hunterCount, true);
    CreateOffspring(preys, preyCount, false);
}

void CreateOffspring(List<AgentFitness> parentPool, int count, bool isHunter)
{
    if (parentPool.Count == 0) 
    {
        // Si no hay padres disponibles, crear agentes aleatorios
        for (int i = 0; i < count; i++)
        {
            Vector3 randomPosition = GetRandomSpawnPosition();
            GameObject newAgent = Instantiate(AgentePrefab, randomPosition, Quaternion.identity);
            population.Add(newAgent);

            InitializeAgent(newAgent);

            // Forzar el tipo de agente si es necesario
            AgentController agentCtrl = newAgent.GetComponent<AgentController>();
            if (agentCtrl != null)
            {
                agentCtrl.genes.isHunter = isHunter; 
            }
        }
        return;
    }

    // Determinar cuántos agentes de élite conservar
    int eliteCount = Mathf.Max(1, Mathf.FloorToInt(count * elitePercentage)); 

    // Crear agentes de élite (copias directas de los mejores)
    for (int i = 0; i < eliteCount && i < parentPool.Count; i++) 
    {
        Vector3 randomPosition = GetRandomSpawnPosition();
        GameObject newAgent = Instantiate(AgentePrefab, randomPosition, Quaternion.identity); 
        population.Add(newAgent);

        // Usar genes del padre élite
        InitializeAgent(newAgent, parentPool[i].genes, true, parentPool[i].agentId);
    }

    // Crear el resto de agentes mediante reproducción y mutación
    for (int i = eliteCount; i < count; i++)
    {
        // Seleccionar padres usando selección por ruleta
        AgentFitness parent1 = SelectParentByRoulette(parentPool);
        AgentFitness parent2 = SelectParentByRoulette(parentPool);

        // Crear genes del hijo mediante cruce
        AgentGenes childGenes = CrossoverGenes(parent1.genes, parent2.genes);

        // Aplicar mutación
        MutateGenes(childGenes);

        // Asegurar que el tipo de agente sea correcto
        childGenes.isHunter = isHunter;

        // Crear el nuevo agente
        Vector3 randomPosition = GetRandomSpawnPosition();
        GameObject newAgent = Instantiate(AgentePrefab, randomPosition, Quaternion.identity);
        population.Add(newAgent);

        InitializeAgent(newAgent, childGenes, false, parent1.agentId, parent2.agentId);             
    }
}

AgentFitness SelectParentByRoulette(List<AgentFitness> pool)
{
    // Calcular suma total de fitness
    float totalFitness = 0;
    foreach (var agent in pool)
    {
        totalFitness += agent.fitnessScore;
    }

    // Si el fitness total es 0, seleccionar aleatoriamente
    if (totalFitness <= 0)
    {
        return pool[Random.Range(0, pool.Count)];
    }

    // Selección por ruleta
    float randomValue = Random.Range(0, totalFitness);
    float currentSum = 0;

    foreach (var agent in pool)
    {
        currentSum += agent.fitnessScore;
        if (currentSum >= randomValue)
        {
            return agent;
        }
    }

    // Por si acaso, devolver el último
    return pool[pool.Count - 1];
}

AgentGenes CrossoverGenes(AgentGenes parent1, AgentGenes parent2)
{
    // Crear genes del hijo mediante cruce
    AgentGenes childGenes = new AgentGenes
    {
        // El tipo de agente se establece después
        isHunter = parent1.isHunter,

        // Cruce de atributos numéricos
        size = Random.value < 0.5f ? parent1.size : parent2.size,
        speed = Random.value < 0.5f ? parent1.speed : parent2.speed,
        stealth = Random.value < 0.5f ? parent1.stealth : parent2.stealth,
        camouflage = Random.value < 0.5f ? parent1.camouflage : parent2.camouflage 
    };

    return childGenes;
}

void MutateGenes(AgentGenes genes)
{
    // Aplicar mutación a cada gen con probabilidad mutationRate
    if (Random.value < mutationRate)
        genes.size = Mathf.Clamp(genes.size + Random.Range(-mutationStrength, mutationStrength), 0.5f, 3f);

    if (Random.value < mutationRate)
        genes.speed = Mathf.Clamp(genes.speed + Random.Range(-mutationStrength, mutationStrength), 1f, 5f);

    if (Random.value < mutationRate)
        genes.stealth = Mathf.Clamp(genes.stealth + Random.Range(-mutationStrength, mutationStrength),
                                   genes.isHunter ? 1f : 0.5f,
                                   genes.isHunter ? 5f : 2f);

    if (Random.value < mutationRate)
        genes.camouflage = Mathf.Clamp(genes.camouflage + Random.Range(-mutationStrength, mutationStrength),
                                      genes.isHunter ? 0.5f : 1f,
                                      genes.isHunter ? 2f : 5f);
}

void StartNewGeneration()
{
        // Registrar datos de la generación actual antes de iniciar la nueva
    if (dataCollector != null)
    {
        float simulationTime = Time.realtimeSinceStartup - simulationStartTime;
        dataCollector.RecordGenerationData(fitnessData, simulationTime);
    }

    // Verificar que los prefabs estén disponibles antes de iniciar una nueva generación
    if (AgentePrefab != null && FoodPrefab != null)
    {
        // Incrementar contador de generación
        generation++;
        Debug.Log($"Generación {generation} comenzada.");

        // Restaurar comida
        SpawnFood();

        // Crear nueva población basada en la anterior
        SpawnAgents();

        // Reiniciar temporizador
        generationCooldown = generationTime;
        generationStartTime = Time.time;
    }
    else
    {
        Debug.LogWarning("No se puede iniciar una nueva generación porque faltan prefabs.");
        simulationInitialized = false;
    }
}

public int GetGeneration()
{
    return generation;
}


}