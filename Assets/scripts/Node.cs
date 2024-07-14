using System;
using System.Collections.Generic;
using UnityEngine;
using Managers;

public class Node : MonoBehaviour
{
    public string niceName = "Node";
    public Guid nodeId { get; private set; } = Guid.NewGuid();
    public Transform messageMarker;
    public Transform deadLetterMarker;
    public GameObject transmitAnimPrefab;
    public GameObject deleteAnimPrefab;
    public List<Node> Peers { get; private set; } = new();
    public List<Node> BlockedPeers { get; private set; } = new();

    public Node[] arrayPeers;
    public Node[] arrayBlockedPeers;

    private List<Message> messages = new();
    private Dictionary<Guid, float> deadMessages = new();
    private bool isTransmitting = false;
    private Node transmissionTarget;
    private Message inFlightMessage;
    private float transmissionProgress = 0.0f;

    private Tuple<Guid, float> inFlightDeleteNotice;

    // Start is called before the first frame update
    void Awake()
    {
    }

    void Start()
    {
        NetworkStateManager.Instance.AddNode(this);
    }

    void Update()
    {
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

    void DoTransmit()
    {
        if (transmissionTarget == null)
        {
            isTransmitting = false;
            transmissionProgress = 0.0f;
            transmissionTarget = null;
            return;
        }

        if (!Peers.Contains(transmissionTarget))
        {
            isTransmitting = false;
            transmissionProgress = 0.0f;
            transmissionTarget = null;
            return;
        }

        transmissionProgress += Time.deltaTime;
        if (transmissionProgress < NetworkStateManager.Instance.TransmissionTime) return;

        if (inFlightDeleteNotice != null)
        {
            transmissionTarget.ReceiveDeleteNotice(inFlightDeleteNotice.Item1, inFlightDeleteNotice.Item2, this);
        }
        else
        {
            transmissionTarget.ReceiveMessage(inFlightMessage, this);
        }

        isTransmitting = false;
        transmissionProgress = 0.0f;
        transmissionTarget = null;
        inFlightDeleteNotice = null;
    }

    private void FindTransmissionTarget()
    {
        bool foundMessageTarget = SetMessageTarget(Peers);
        if (foundMessageTarget)
        {
            Instantiate(transmitAnimPrefab, transform.position, Quaternion.identity);
            TransmitAnim anim = transmitAnimPrefab.GetComponent<TransmitAnim>();
            anim?.SetDestination(transmissionTarget.transform.position);
            isTransmitting = true;
            return;
        }
        bool foundDeadMessageTarget = SetDeadMessageTarget(Peers);
        if (foundDeadMessageTarget)
        {
            Instantiate(deleteAnimPrefab, transform.position, Quaternion.identity);
            TransmitAnim anim = deleteAnimPrefab.GetComponent<TransmitAnim>();
            anim?.SetDestination(transmissionTarget.transform.position);
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
        Debug.Log($"Node {niceName} ReceiveMessage {message.body} from {sender.niceName}");
        message.recentHops++;

        if (message.destination == this.nodeId)
        {
            sender.ReceiveDeleteNotice(message.uID, Time.time + NetworkStateManager.Instance.deathNoteTTL, this);
            Destination destination = GetComponentInParent<Destination>();
            if (destination != null)
            {
                Debug.Log($"Node {niceName} is a destination node setting text {message.body}");
                destination.SetText(message.body);
            }
        }

        messages.Add(message);
    }

    public void ReceiveMessage(Message message)
    {
        Debug.Log($"Node {niceName} had a sender give them a message {message.body}");
        messages.Add(message);
    }

    public void ReceiveDeleteNotice(Guid id, float ttl, Node sender)
    {
        Debug.Log($"Node {niceName} ReceiveDeleteNotice {id} from {sender.niceName}");
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
                    Debug.Log($"Node {niceName} sending an animation blip to {peer.niceName}");

                    inFlightMessage = message;
                    transmissionTarget = peer;
                    return true;
                }
                else
                {
                    Debug.Log($"Node {niceName} does not need to send a message to {peer.niceName}");
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
                    Debug.Log($"Node {niceName} sending a delete animation to {peer.niceName}");

                    inFlightDeleteNotice = new(entry.Key, entry.Value);
                    transmissionTarget = peer;
                    return true;
                }
                else
                {
                    Debug.Log($"Node {niceName} does not need to send a dead message to {peer.niceName}");
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
                    Debug.Log($"Node {niceName} removing mesage listed in dead messages {messages[i].body}");
                    toDelete.Add(i);
                }
            }
        }
        //now remove the messages
        foreach (int index in toDelete)
        {
            messages.RemoveAt(index);
        }
    }

    private void IdentifyExpiredMessages()
    {
        for (int i = 0; i < messages.Count; i++)
        {
            if (Time.time >= messages[i].ttl && !deadMessages.ContainsKey(messages[i].uID))
            {
                Debug.Log($"Node {niceName} adding expired message to the queue {messages[i].body}");
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
                Debug.Log($"Node {niceName} removing dead message {entry.Key}");
                toDelete.Add(entry.Key);
            }
        }
        foreach (Guid key in toDelete)
        {
            deadMessages.Remove(key);
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

        arrayPeers = peers.ToArray();
        arrayBlockedPeers = blockedPeers.ToArray();
    }

    private bool CanSeeNode(Node node)
    {
        Debug.LogWarning($"Node {niceName} checking if it can see {node.niceName}");
        return CanSeeFrom(transform.position, node, Vector3.Distance(transform.position, node.transform.position));
    }

    private bool CanSeeFrom(Vector3 origin, Node target, float maxDistance = 10f)
    {
        if(maxDistance < 0.1f) return false;
        RaycastHit hit;
        //get direction from origin to target
        Vector3 direction = target.transform.position - origin;
        if (Physics.Raycast(origin, direction, out hit, maxDistance))
        {
            Node hitNode = hit.collider.gameObject.GetComponentInChildren<Node>();
            // hit has no node, it's a blocker
            if (hitNode == null) return false;

            Debug.LogWarning($"Node {niceName} hit {hitNode.niceName} when trying to see {target.niceName}");
            if (hitNode == target) return true;

            Debug.LogWarning($"Node {niceName} is ignoring {hitNode.niceName} when trying to see {target.niceName}");
            return false;
            // return CanSeeFrom(hit.transform.position, target, Vector3.Distance(hit.transform.position, target.transform.position));
        }
        return false;
    }
}
