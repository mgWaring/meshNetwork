using System;
using System.Collections.Generic;
using UnityEngine;
using Managers;

public class Node : MonoBehaviour
{
    public string[] arrayMessages; //debug
    public string niceName = "Node";
    public Guid nodeId { get; private set; } = Guid.NewGuid();
    public Transform messageMarker;
    public Transform deadLetterMarker;
    public List<Node> Peers { get; private set; } = new();
    public List<Node> BlockedPeers { get; private set; } = new();
    [SerializeField] Material tansmitMaterial;
    [SerializeField] Material deleteMaterial;
    private List<Message> messages = new();
    private Dictionary<Guid, float> deadMessages = new();
    private bool isTransmitting = false;
    private Node transmissionTarget;
    private Message inFlightMessage;
    private float transmissionProgress = 0.0f;
    private Tuple<Guid, float> inFlightDeleteNotice;
    private LineRenderer lineRenderer;
    private Vector3 initialPosition;

    // Start is called before the first frame update
    void Awake()
    {
    }

    void Start()
    {
        initialPosition = transform.position;
        NetworkStateManager.Instance.AddNode(this);
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        // debug \/
        arrayMessages = new string[messages.Count];
        foreach (Message message in messages)
        {
            arrayMessages[messages.IndexOf(message)] = message.ToString();
        }


        if (messageMarker != null)
            messageMarker.localScale = new Vector3(0.2f, (messages.Count * 0.1f + 0.1f), 0.2f);
        if (deadLetterMarker != null)
            deadLetterMarker.localScale = new Vector3(0.2f, (messages.Count * 0.1f + 0.1f), 0.2f);

        // constantly look for peers
        FindPeers();
        if (!isTransmitting) FindTransmissionTarget();
        OrganiseMessages();
        DoTransmit();
    }

    void OrganiseMessages()
    {
        IdentifyExpiredMessages();
        RemoveMessagesListedInDeadMessages();
        RemoveExpiredDeadMessages();
    }

    void OnDestroy()
    {
    }

    public void Reset(){
        messages.Clear();
        deadMessages.Clear();
        ResetTransmitState();
        gameObject.transform.parent.transform.position = initialPosition;
    }

    void ResetTransmitState()
    {
        isTransmitting = false;
        transmissionProgress = 0.0f;
        transmissionTarget = null;
        inFlightDeleteNotice = null;
        lineRenderer.SetPositions(new Vector3[2]);
    }

    void DoTransmit()
    {
        if (transmissionTarget == null || !Peers.Contains(transmissionTarget))
        {
            ResetTransmitState();
            return;
        }

        transmissionProgress += Time.deltaTime;

        if (transmissionProgress < NetworkStateManager.Instance.TransmissionTime)
        {
            DisplayTransmissionMarker();
            return;
        }

        //transmission is complete
        if (inFlightDeleteNotice != null)
        {
            NetworkStateManager.Instance.UpdateDeleteCount(NetworkStateManager.Instance.deleteCount - 1);
            transmissionTarget.ReceiveDeleteNotice(inFlightDeleteNotice.Item1, inFlightDeleteNotice.Item2, this);
        }
        else
        {
            NetworkStateManager.Instance.UpdateMessageCount(NetworkStateManager.Instance.messageCount - 1);
            transmissionTarget.ReceiveMessage(inFlightMessage, this);
        }

        ResetTransmitState();
    }

    private void DisplayTransmissionMarker()
    {
        float distance = Vector3.Distance(transform.position, transmissionTarget.transform.position);
        // work out how far through trasmitting we are
        float progress = transmissionProgress / NetworkStateManager.Instance.TransmissionTime;
        //position a line of length distance * progress at the origin, towards the target
        Vector3[] positions = new Vector3[2];
        positions[0] = transform.position;
        positions[1] = Vector3.Lerp(transform.position, transmissionTarget.transform.position, progress);
        lineRenderer.SetPositions(positions);
    }

    private void FindTransmissionTarget()
    {
        bool foundMessageTarget = SetMessageTarget(Peers);
        if (foundMessageTarget)
        {
            NetworkStateManager.Instance.UpdateMessageCount(NetworkStateManager.Instance.messageCount + 1);
            lineRenderer.material = tansmitMaterial;
            isTransmitting = true;
            return;
        }
        bool foundDeadMessageTarget = SetDeadMessageTarget(Peers);
        if (foundDeadMessageTarget)
        {
            NetworkStateManager.Instance.UpdateDeleteCount(NetworkStateManager.Instance.deleteCount + 1);
            lineRenderer.material = deleteMaterial;
            isTransmitting = true;
        }
    }

    public bool NeedMessage(Guid uID)
    {
        foreach (Message message in messages)
        {
            if (message.uID == uID)
            {
                return false;
            }
        }
        return true;
    }
    public bool NeedDeadMessage(Guid uID)
    {
        foreach (KeyValuePair<Guid, float> entry in deadMessages)
        {
            if (entry.Key == uID)
            {
                return false;
            }
        }
        return true;
    }
    public void ReceiveMessage(Message message, Node sender)
    {
        message.recentHops++;

        if (message.destination == this.nodeId)
        {
            sender.ReceiveDeleteNotice(message.uID, Time.time + NetworkStateManager.Instance.deathNoteTTL, this);
            Destination destination = GetComponentInParent<Destination>();
            if (destination != null)
            {
                destination.SetText(message.body);
            }
        }

        messages.Add(message);
    }

    public void ReceiveMessage(Message message)
    {
        if(messages.Contains(message))
        {
            return;
        }
        messages.Add(message);
    }

    public void ReceiveDeleteNotice(Guid id, float ttl, Node sender)
    {
        if(deadMessages.ContainsKey(id))
        {
            return;
        }
        deadMessages.Add(id, ttl);
    }

    private bool SetMessageTarget(List<Node> peers)
    {
        foreach (Node peer in peers)
        {
            foreach (Message message in messages)
            {
                if (peer.NeedMessage(message.uID))
                {
                    inFlightMessage = message;
                    transmissionTarget = peer;
                    return true;
                }
            }
        }

        return false;
    }

    private bool SetDeadMessageTarget(List<Node> peers)
    {
        foreach (Node peer in peers)
        {
            foreach (KeyValuePair<Guid, float> entry in deadMessages)
            {
                if (peer.NeedDeadMessage(entry.Key))
                {
                    inFlightDeleteNotice = new(entry.Key, entry.Value);
                    transmissionTarget = peer;
                    return true;
                }
            }
        }

        return false;
    }

    private void RemoveMessagesListedInDeadMessages()
    {
        List<int> toDelete = new();
        //find the messages to delete
        foreach (KeyValuePair<Guid, float> entry in deadMessages)
        {
            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].uID == entry.Key)
                {
                    toDelete.Add(i);
                }
            }
        }
        //now remove the messages
        foreach (int index in toDelete)
        {
            if(index < 0 || index >= messages.Count)
            {
                continue;
            }
            messages.RemoveAt(index);
            NetworkStateManager.Instance.UpdateMessageCount(NetworkStateManager.Instance.messageCount - 1);
        }
    }

    private void IdentifyExpiredMessages()
    {
        for (int i = 0; i < messages.Count; i++)
        {
            if (Time.time >= messages[i].ttl && !deadMessages.ContainsKey(messages[i].uID))
            {
                deadMessages.Add(messages[i].uID, Time.time + NetworkStateManager.Instance.deathNoteTTL);
            }
        }
    }

    private void RemoveExpiredDeadMessages()
    {
        List<Guid> toDelete = new();
        foreach (KeyValuePair<Guid, float> entry in deadMessages)
        {
            if (Time.time >= entry.Value)
            {
                toDelete.Add(entry.Key);
            }
        }
        foreach (Guid key in toDelete)
        {
            deadMessages.Remove(key);
            NetworkStateManager.Instance.UpdateDeleteCount(NetworkStateManager.Instance.deleteCount - 1);
        }
    }

    private void FindPeers()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            NetworkStateManager.Instance.nodeConnectionRange,
            1 << 6
        );
        List<Node> peers = new();
        List<Node> blockedPeers = new();

        foreach (Collider collider in colliders)
        {
            if (collider.isTrigger) continue;
            if (collider.gameObject != gameObject)
            {
                Node node = collider.gameObject.GetComponent<Node>();
                if (node != null && CanSeeNode(node))
                {
                    peers.Add(node);
                    continue;
                }
                blockedPeers.Add(node);
            }
        }

        Peers = peers;
        BlockedPeers = blockedPeers;
    }

    private bool CanSeeNode(Node node)
    {
        return CanSeeFrom(transform.position, node, Vector3.Distance(transform.position, node.transform.position));
    }

    private bool CanSeeFrom(Vector3 origin, Node target, float maxDistance = 10f)
    {
        if (maxDistance < 0.1f) return false;
        RaycastHit hit;
        //get direction from origin to target
        Vector3 direction = target.transform.position - origin;
        if (Physics.Raycast(origin, direction, out hit, maxDistance))
        {
            Node hitNode = hit.collider.gameObject.GetComponentInChildren<Node>();
            // hit has no node, it's a blocker
            if (hitNode == null) return false;

            //hit is the target, woohoo
            if (hitNode == target) return true;

            //hit is another node, so we ignore it and carry on checking
            return true;
        }
        return false;
    }
}
