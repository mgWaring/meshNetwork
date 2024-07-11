using UnityEngine;

public class Traveller : MonoBehaviour
{
    [SerializeField] private Transform destination;

    [SerializeField] private UnityEngine.AI.NavMeshAgent agent;

    public void BeginTravel(Transform _destination)
    {
        Debug.LogWarning($"Travelling to {_destination.position}");
        destination = _destination;
        agent.SetDestination(destination.position);
    }

    public void StopTravel()
    {
        agent.ResetPath();
    }
}