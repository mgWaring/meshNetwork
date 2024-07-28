using UnityEngine;
using System;
using Managers;

public class Sender : MonoBehaviour
{
    public Node myNode; 

    public string niceName = "Sender";

    void Awake()
    {
        NetworkStateManager.Instance.AddSender(this);
    }
    void Start()
    { 
        if (myNode == null)
        {
            Debug.LogError($"No Node attached to Sender {niceName}!");
        }
    }

    public void SendMessage(Guid destinationID, string body)
    {
        Message message = new()
        {
            destination = destinationID,
            uID = Guid.NewGuid(),
            body = body,
            ttl = Time.time + NetworkStateManager.Instance.messageTTL
        };
        myNode.ReceiveMessage(message);
        NetworkStateManager.Instance.UpdateUniqueMessageCount(NetworkStateManager.Instance.uniqueMessageCount + 1);
    }
}