using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System;

[System.Serializable]
public class GenerationData
{
    public int generationNumber;
    public float averageFitness;
    public float maxFitness;
    public float minFitness;
    public int hunterCount;
    public int preyCount;
    public float averageHunterFitness;
    public float averagePreyFitness;
    public float averageHunterSize;
    public float averageHunterSpeed;
    public float averageHunterStealth;
    public float averagePreySize;
    public float averagePreySpeed;
    public float averagePreyCamouflage;
    public int totalSuccessfulHunts;
    public int totalSuccessfulEscapes;
    public float averageHunterEnergy;
    public float averagePreyEnergy;
    public float averageHunterLifespan;
    public float averagePreyLifespan;
    public float simulationTime;
    public DateTime timestamp;
}

[System.Serializable]
public class AgentData
{
    public int generationNumber;
    public string agentId;
    public bool isHunter;
    public float size;
    public float speed;
    public float stealth;
    public float camouflage;
    public float finalFitness;
    public float finalEnergy;
    public float lifespan;
    public int successfulHunts;
    public int successfulEscapes;
    public float averageDistanceToOpponents;
    public bool survivedGeneration;
    public string parentId1;
    public string parentId2;
    public bool isElite;
}

public class DataCollector : MonoBehaviour
{
    // Singleton instance
    public static DataCollector Instance { get; private set; }
    
    // Data storage
    private List<GenerationData> generationsData = new List<GenerationData>();
    private List<AgentData> agentsData = new List<AgentData>();
    
    // Configuration
    public bool enableDataCollection = true;
    public bool saveAfterEachGeneration = true;
    public string dataFolderPath = "SimulationData";
    public string simulationId;
    
    // References
    private Manager simulationManager;
    
    // Tracking variables
    private Dictionary<GameObject, string> agentIdMap = new Dictionary<GameObject, string>();
    private int nextAgentId = 0;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Generate unique simulation ID if not set
            if (string.IsNullOrEmpty(simulationId))
            {
                simulationId = "Sim_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            }
            
            // Create data directory if it doesn't exist
            if (!Directory.Exists(dataFolderPath))
            {
                Directory.CreateDirectory(dataFolderPath);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Find the simulation manager - use FindFirstObjectByType instead of FindObjectOfType
        simulationManager = FindFirstObjectByType<Manager>();
        if (simulationManager == null)
        {
            Debug.LogError("DataCollector: No Manager found in the scene!");
            enableDataCollection = false;
        }
        else
        {
            Debug.Log("DataCollector: Successfully connected to Manager.");
        }
    }
    
    // Register a new agent for tracking
    public string RegisterAgent(GameObject agent, AgentController controller, bool isElite = false, string parent1 = "", string parent2 = "")
    {
        if (!enableDataCollection) return "";
        
        string agentId = nextAgentId.ToString();
        nextAgentId++;
        
        agentIdMap[agent] = agentId;
        
        // Create initial agent data
        AgentData data = new AgentData
        {
            generationNumber = simulationManager.GetGeneration(), // Use a getter method instead of direct access
            agentId = agentId,
            isHunter = controller.genes.isHunter,
            size = controller.genes.size,
            speed = controller.genes.speed,
            stealth = controller.genes.stealth,
            camouflage = controller.genes.camouflage,
            finalFitness = 0,
            finalEnergy = controller.energy,
            lifespan = 0,
            successfulHunts = 0,
            successfulEscapes = 0,
            averageDistanceToOpponents = 0,
            survivedGeneration = true,
            parentId1 = parent1,
            parentId2 = parent2,
            isElite = isElite
        };
        
        agentsData.Add(data);
        return agentId;
    }
    
    // Update agent data during simulation
    public void UpdateAgentData(GameObject agent, float fitness, float energy, float lifespan, 
                               int hunts, int escapes, float avgDistance, bool survived)
    {
        if (!enableDataCollection) return;
        if (!agentIdMap.ContainsKey(agent)) return;
        
        string agentId = agentIdMap[agent];
        AgentData data = agentsData.FirstOrDefault(a => a.agentId == agentId && 
                                                      a.generationNumber == simulationManager.GetGeneration());
        
        if (data != null)
        {
            data.finalFitness = fitness;
            data.finalEnergy = energy;
            data.lifespan = lifespan;
            data.successfulHunts = hunts;
            data.successfulEscapes = escapes;
            data.averageDistanceToOpponents = avgDistance;
            data.survivedGeneration = survived;
        }
    }
    
    // Record data for the entire generation
    public void RecordGenerationData(List<AgentFitness> fitnessData, float simulationTime)
    {
        if (!enableDataCollection) return;
        
        var hunterFitness = fitnessData.Where(f => f.genes.isHunter).ToList();
        var preyFitness = fitnessData.Where(f => !f.genes.isHunter).ToList();
        
        GenerationData data = new GenerationData
        {
            generationNumber = simulationManager.GetGeneration(),
            averageFitness = fitnessData.Count > 0 ? fitnessData.Average(f => f.fitnessScore) : 0,
            maxFitness = fitnessData.Count > 0 ? fitnessData.Max(f => f.fitnessScore) : 0,
            minFitness = fitnessData.Count > 0 ? fitnessData.Min(f => f.fitnessScore) : 0,
            hunterCount = hunterFitness.Count,
            preyCount = preyFitness.Count,
            averageHunterFitness = hunterFitness.Count > 0 ? hunterFitness.Average(f => f.fitnessScore) : 0,
            averagePreyFitness = preyFitness.Count > 0 ? preyFitness.Average(f => f.fitnessScore) : 0,
            averageHunterSize = hunterFitness.Count > 0 ? hunterFitness.Average(f => f.genes.size) : 0,
            averageHunterSpeed = hunterFitness.Count > 0 ? hunterFitness.Average(f => f.genes.speed) : 0,
            averageHunterStealth = hunterFitness.Count > 0 ? hunterFitness.Average(f => f.genes.stealth) : 0,
            averagePreySize = preyFitness.Count > 0 ? preyFitness.Average(f => f.genes.size) : 0,
            averagePreySpeed = preyFitness.Count > 0 ? preyFitness.Average(f => f.genes.speed) : 0,
            averagePreyCamouflage = preyFitness.Count > 0 ? preyFitness.Average(f => f.genes.camouflage) : 0,
            totalSuccessfulHunts = hunterFitness.Sum(f => f.successfulHunts),
            totalSuccessfulEscapes = preyFitness.Sum(f => f.successfulEscapes),
            averageHunterEnergy = hunterFitness.Count > 0 ? hunterFitness.Average(f => f.energyLevel) : 0,
            averagePreyEnergy = preyFitness.Count > 0 ? preyFitness.Average(f => f.energyLevel) : 0,
            averageHunterLifespan = hunterFitness.Count > 0 ? hunterFitness.Average(f => f.lifespan) : 0,
            averagePreyLifespan = preyFitness.Count > 0 ? preyFitness.Average(f => f.lifespan) : 0,
            simulationTime = simulationTime,
            timestamp = DateTime.Now
        };
        
        generationsData.Add(data);
        
        if (saveAfterEachGeneration)
        {
            SaveData();
        }
    }
    
    // Save all collected data to CSV files
    public void SaveData()
    {
        if (!enableDataCollection) return;
        
        string generationsFilePath = Path.Combine(dataFolderPath, $"{simulationId}_generations.csv");
        string agentsFilePath = Path.Combine(dataFolderPath, $"{simulationId}_agents.csv");
        
        // Save generations data
        using (StreamWriter writer = new StreamWriter(generationsFilePath, false))
        {
            // Write header
            writer.WriteLine("GenerationNumber,AverageFitness,MaxFitness,MinFitness,HunterCount,PreyCount," +
                           "AvgHunterFitness,AvgPreyFitness,AvgHunterSize,AvgHunterSpeed,AvgHunterStealth," +
                           "AvgPreySize,AvgPreySpeed,AvgPreyCamouflage,TotalHunts,TotalEscapes," +
                           "AvgHunterEnergy,AvgPreyEnergy,AvgHunterLifespan,AvgPreyLifespan,SimulationTime,Timestamp");
            
            // Write data rows
            foreach (var gen in generationsData)
            {
                writer.WriteLine($"{gen.generationNumber},{gen.averageFitness},{gen.maxFitness},{gen.minFitness}," +
                               $"{gen.hunterCount},{gen.preyCount},{gen.averageHunterFitness},{gen.averagePreyFitness}," +
                               $"{gen.averageHunterSize},{gen.averageHunterSpeed},{gen.averageHunterStealth}," +
                               $"{gen.averagePreySize},{gen.averagePreySpeed},{gen.averagePreyCamouflage}," +
                               $"{gen.totalSuccessfulHunts},{gen.totalSuccessfulEscapes},{gen.averageHunterEnergy}," +
                               $"{gen.averagePreyEnergy},{gen.averageHunterLifespan},{gen.averagePreyLifespan}," +
                               $"{gen.simulationTime},{gen.timestamp}");
            }
        }
        
        // Save agents data
        using (StreamWriter writer = new StreamWriter(agentsFilePath, false))
        {
            // Write header
            writer.WriteLine("GenerationNumber,AgentId,IsHunter,Size,Speed,Stealth,Camouflage,Fitness,Energy," +
                           "Lifespan,Hunts,Escapes,AvgDistance,Survived,ParentId1,ParentId2,IsElite");
            
            // Write data rows
            foreach (var agent in agentsData)
            {
                writer.WriteLine($"{agent.generationNumber},{agent.agentId},{agent.isHunter},{agent.size}," +
                               $"{agent.speed},{agent.stealth},{agent.camouflage},{agent.finalFitness}," +
                               $"{agent.finalEnergy},{agent.lifespan},{agent.successfulHunts},{agent.successfulEscapes}," +
                               $"{agent.averageDistanceToOpponents},{agent.survivedGeneration},{agent.parentId1}," +
                               $"{agent.parentId2},{agent.isElite}");
            }
        }
        
        Debug.Log($"DataCollector: Data saved to {dataFolderPath}");
    }
    
    // Calculate additional statistics
// In DataCollector.cs, modify the CalculateStatistics method:

// Calculate additional statistics
public Dictionary<string, object> CalculateStatistics()
{
    Dictionary<string, object> stats = new Dictionary<string, object>();
    
    if (generationsData.Count == 0) return stats;
    
    // Evolution rate (fitness increase per generation)
    var fitnessProgression = generationsData.Select(g => g.averageFitness).ToArray();
    if (fitnessProgression.Length > 1)
    {
        float evolutionRate = (fitnessProgression.Last() - fitnessProgression.First()) / (fitnessProgression.Length - 1);
        stats["EvolutionRate"] = evolutionRate;
    }
    
    // Population stability
    var hunterCounts = generationsData.Select(g => (float)g.hunterCount).ToArray(); // Convert to float[]
    var preyCounts = generationsData.Select(g => (float)g.preyCount).ToArray(); // Convert to float[]
    if (hunterCounts.Length > 5)
    {
        float hunterStability = CalculateStability(hunterCounts);
        float preyStability = CalculateStability(preyCounts);
        stats["HunterStability"] = hunterStability;
        stats["PreyStability"] = preyStability;
    }
    
    // Success rates
    var huntRates = generationsData.Select(g => g.hunterCount > 0 ? (float)g.totalSuccessfulHunts / g.hunterCount : 0).ToArray();
    var escapeRates = generationsData.Select(g => g.preyCount > 0 ? (float)g.totalSuccessfulEscapes / g.preyCount : 0).ToArray();
    stats["AverageHuntRate"] = huntRates.Average();
    stats["AverageEscapeRate"] = escapeRates.Average();
    stats["FinalHuntRate"] = huntRates.Last();
    stats["FinalEscapeRate"] = escapeRates.Last();
    
    // Gene evolution trends
    stats["HunterSizeEvolution"] = CalculateEvolutionTrend(generationsData.Select(g => g.averageHunterSize).ToArray());
    stats["HunterSpeedEvolution"] = CalculateEvolutionTrend(generationsData.Select(g => g.averageHunterSpeed).ToArray());
    stats["HunterStealthEvolution"] = CalculateEvolutionTrend(generationsData.Select(g => g.averageHunterStealth).ToArray());
    stats["PreySizeEvolution"] = CalculateEvolutionTrend(generationsData.Select(g => g.averagePreySize).ToArray());
    stats["PreySpeedEvolution"] = CalculateEvolutionTrend(generationsData.Select(g => g.averagePreySpeed).ToArray());
    stats["PreyCamouflageEvolution"] = CalculateEvolutionTrend(generationsData.Select(g => g.averagePreyCamouflage).ToArray());
    
    // Energy efficiency
    var hunterEnergyEfficiency = generationsData.Select(g => g.averageHunterFitness / (g.averageHunterEnergy > 0 ? g.averageHunterEnergy : 1)).ToArray();
    var preyEnergyEfficiency = generationsData.Select(g => g.averagePreyFitness / (g.averagePreyEnergy > 0 ? g.averagePreyEnergy : 1)).ToArray();
    stats["FinalHunterEnergyEfficiency"] = hunterEnergyEfficiency.Last();
    stats["FinalPreyEnergyEfficiency"] = preyEnergyEfficiency.Last();
    
    return stats;
}

    // Helper method to calculate stability (lower value = more stable)
    private float CalculateStability(float[] values)
    {
        if (values.Length <= 1) return 0;
        
        float sum = 0;
        for (int i = 1; i < values.Length; i++)
        {
            sum += Mathf.Abs(values[i] - values[i-1]);
        }
        
        return sum / (values.Length - 1);
    }
    
    // Helper method to calculate evolution trend (positive = increasing, negative = decreasing)
    private float CalculateEvolutionTrend(float[] values)
    {
        if (values.Length <= 1) return 0;
        
        // Simple linear regression
        float sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        int n = values.Length;
        
        for (int i = 0; i < n; i++)
        {
            sumX += i;
            sumY += values[i];
            sumXY += i * values[i];
            sumX2 += i * i;
        }
        
        float slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        return slope;
    }
    
    // Generate a summary report
    public string GenerateSummaryReport()
    {
        if (generationsData.Count == 0) return "No data available.";
        
        var stats = CalculateStatistics();
        StringBuilder report = new StringBuilder();
        
        report.AppendLine($"=== Simulation Summary: {simulationId} ===");
        report.AppendLine($"Total Generations: {generationsData.Count}");
        report.AppendLine($"Initial Population: {generationsData[0].hunterCount + generationsData[0].preyCount} " +
                         $"({generationsData[0].hunterCount} hunters, {generationsData[0].preyCount} prey)");
        report.AppendLine($"Final Population: {generationsData.Last().hunterCount + generationsData.Last().preyCount} " +
                         $"({generationsData.Last().hunterCount} hunters, {generationsData.Last().preyCount} prey)");
        report.AppendLine();
        
        report.AppendLine("=== Fitness Evolution ===");
        report.AppendLine($"Initial Average Fitness: {generationsData[0].averageFitness:F2}");
        report.AppendLine($"Final Average Fitness: {generationsData.Last().averageFitness:F2}");
        if (stats.ContainsKey("EvolutionRate"))
            report.AppendLine($"Evolution Rate: {stats["EvolutionRate"]:F2} fitness/generation");
        report.AppendLine();
        
        report.AppendLine("=== Hunter Evolution ===");
        report.AppendLine($"Size: {generationsData[0].averageHunterSize:F2} → {generationsData.Last().averageHunterSize:F2} " +
                         $"(Trend: {stats["HunterSizeEvolution"]:F3})");
        report.AppendLine($"Speed: {generationsData[0].averageHunterSpeed:F2} → {generationsData.Last().averageHunterSpeed:F2} " +
                         $"(Trend: {stats["HunterSpeedEvolution"]:F3})");
        report.AppendLine($"Stealth: {generationsData[0].averageHunterStealth:F2} → {generationsData.Last().averageHunterStealth:F2} " +
                         $"(Trend: {stats["HunterStealthEvolution"]:F3})");
        report.AppendLine($"Hunt Success Rate: {stats["AverageHuntRate"]:F2} → {stats["FinalHuntRate"]:F2}");
        report.AppendLine();
        
        report.AppendLine("=== Prey Evolution ===");
        report.AppendLine($"Size: {generationsData[0].averagePreySize:F2} → {generationsData.Last().averagePreySize:F2} " +
                         $"(Trend: {stats["PreySizeEvolution"]:F3})");
        report.AppendLine($"Speed: {generationsData[0].averagePreySpeed:F2} → {generationsData.Last().averagePreySpeed:F2} " +
                         $"(Trend: {stats["PreySpeedEvolution"]:F3})");
        report.AppendLine($"Camouflage: {generationsData[0].averagePreyCamouflage:F2} → {generationsData.Last().averagePreyCamouflage:F2} " +
                         $"(Trend: {stats["PreyCamouflageEvolution"]:F3})");
        report.AppendLine($"Escape Success Rate: {stats["AverageEscapeRate"]:F2} → {stats["FinalEscapeRate"]:F2}");
        report.AppendLine();
        
        report.AppendLine("=== Energy Efficiency ===");
        report.AppendLine($"Hunter Energy Efficiency: {stats["FinalHunterEnergyEfficiency"]:F2}");
        report.AppendLine($"Prey Energy Efficiency: {stats["FinalPreyEnergyEfficiency"]:F2}");
        report.AppendLine();
        
        report.AppendLine("=== Population Stability ===");
        if (stats.ContainsKey("HunterStability") && stats.ContainsKey("PreyStability"))
        {
            report.AppendLine($"Hunter Population Stability: {stats["HunterStability"]:F2}");
            report.AppendLine($"Prey Population Stability: {stats["PreyStability"]:F2}");
        }
        
        return report.ToString();
    }
}
