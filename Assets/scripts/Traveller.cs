using UnityEngine;
using System.Collections.Generic;

public class Traveller : MonoBehaviour
{
    [SerializeField] private Transform destination;

    [SerializeField] private UnityEngine.AI.NavMeshAgent agent;

    [SerializeField] private Transform[] teammates;

    [SerializeField] private Transform team;

    [SerializeField] private Transform leader;

    [SerializeField] private bool IsLeader;

    [SerializeField] private Vector2 offset = new Vector2(1, 1);

    public void Start()
    {
        team = gameObject.GetComponentInParent<Team>().transform;
        List<Transform> _teammates = new List<Transform>();
        foreach (Transform teammate in team)
        {
            Traveller traveller = teammate.GetComponentInChildren<Traveller>();
            if (traveller != null) continue;
            if (traveller.IsLeader)
            {
                leader = teammate;
                continue;
            }
            _teammates.Add(teammate);
        }
        teammates = _teammates.ToArray();
    }

    public void BeginTravel(Transform _destination)
    {
        destination = _destination;
        agent.SetDestination(destination.position);
    }

    public void SetDestination(Vector3 position)
    {
        agent.SetDestination(position);
    }

    public void HaltTravel()
    {
        destination = null;
        agent.ResetPath();
    }

    public void Update()
    {
        if (destination == null) return;
        if (leader == null) return;
        if(Vector3.Distance(transform.position, destination.position) < 1)
        {
            HaltTravel();
            foreach (Transform teammate in teammates)
            {
                Traveller teammateAgent = teammate.GetComponent<Traveller>();
                if (teammateAgent == null) continue;
                teammateAgent.HaltTravel();
            }
            return;
        }

        int iterator = 0;
        int factor = 1;
        foreach (Transform teammate in teammates)
        {
            iterator++;

            Traveller teammateAgent = teammate.GetComponent<Traveller>();
            if (teammateAgent == null) continue;
            Vector3 targetLocalPosition = leader.localPosition + new Vector3(offset.x * iterator, 0, offset.y * iterator);
            Vector3 targetWorldPosition = leader.TransformPoint(targetLocalPosition);
            teammateAgent.SetDestination(targetWorldPosition);
            factor *= -1;
        }
    }

    public void StopTravel()
    {
        destination = null;
        agent.ResetPath();
    }
}