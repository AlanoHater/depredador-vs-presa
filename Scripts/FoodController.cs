using UnityEngine;

public class FoodController : MonoBehaviour
{
    public float energy = 10f; // Amount of energy the food provides
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agente"))
        {
            AgentController agente = other.GetComponent<AgentController>();
            if (agente != null && gameObject != null )
            {
                agente.Eat(energy); // Call the Eat method on the agent
                Destroy(gameObject); // Destroy the food object after being eaten
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
