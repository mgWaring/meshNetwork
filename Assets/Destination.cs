using UnityEngine;
using Managers;

public class Destination : MonoBehaviour
{
    [SerializeField] private string niceName = "Destination";
    [SerializeField] private Node myNode;

    public TextMesh text;
    void Start()
    {
        if (myNode == null)
        {
            Debug.LogError($"No Node attached to Destination {niceName}!");
        }
        NetworkStateManager.Instance.AddDestinationNode(niceName, myNode);
    }

    public void SetText(string message)
    {
        Debug.Log($"Setting text: {message}");
        text.text = message;
    }
}