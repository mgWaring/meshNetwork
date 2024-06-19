using System;
using System.Collections.Generic;
using UnityEngine;
using Utility;
using TMPro;
using System.Transactions;

namespace Managers
{
    public class NetworkStateManager : LonelyMonoBehaviour<NetworkStateManager>
    {
        public float tickRate = 1.0f;

        public float nodeConnectionRange = 10.0f;

        public float messageTTL = 10.0f;

        public float deathNoteTTL = 10.0f;

        public float TransmissionTime = 1.0f;

        public int maxHops = 1;



        public TMP_Dropdown destinationDropdown;
        public TMP_Dropdown senderDropdown;

        public TMP_InputField messageInput;
        public List<Node> nodes { get; private set; } = new();

        private Dictionary<string, Node> destinations = new();
        private List<Sender> senders = new();
        private HashSet<(Guid, Guid)> drawnPairs = new HashSet<(Guid, Guid)>();

        [SerializeField]
        private Material LineMaterial;
        private List<LineRenderer> lineRenderers = new();


        void Start()
        {
            destinationDropdown.ClearOptions();
            senderDropdown.ClearOptions();

            List<string> options = new(destinations.Keys);

            destinationDropdown.AddOptions(options);
            senderDropdown.AddOptions(senders.ConvertAll(x => x.niceName));
        }

        void Update()
        {
            DrawLines(nodes);
        }
        public void AddDestinationNode(string name, Node node)
        {
            destinations.Add(name, node);
        }
        public void AddNode(Node node)
        {
            nodes.Add(node);
        }

        public void AddSender(Sender sender)
        {
            senders.Add(sender);
        }

        public Dictionary<string, Node> ListDestinations()
        {
            return destinations;
        }

        public Guid GetDestinationUID()
        {
            return destinations[destinationDropdown.options[destinationDropdown.value].text].nodeId;
        }

        public string GetMessage()
        {
            return messageInput.text;
        }

        public void TriggerMessage()
        {
            Debug.Log("Triggering message");
            senders[senderDropdown.value].SendMessage(GetDestinationUID(), GetMessage());
        }

        public void DrawLines(List<Node> nodes)
        {
            drawnPairs.Clear();
            foreach (var node in nodes)
            {
                foreach (var peer in node.Peers)
                {
                    DrawLineIfNotDrawn(node, peer);
                }
            }
            //instantiation is expensive, so lets keep our pool of renderers and just reset the lines
            if (lineRenderers.Count > drawnPairs.Count)
            {
                for (int i = drawnPairs.Count; i < lineRenderers.Count; i++)
                {
                    lineRenderers[i].SetPosition(0, Vector3.zero);
                    lineRenderers[i].SetPosition(1, Vector3.zero);
                }
            }
        }

        private void DrawLineIfNotDrawn(Node node1, Node node2)
        {
            Guid id1 = node1.nodeId;
            Guid id2 = node2.nodeId;

            // Ensure the pair is sorted consistently
            var pair = id1.CompareTo(id2) < 0 ? (id1, id2) : (id2, id1);

            if (!drawnPairs.Contains(pair))
            {
                drawnPairs.Add(pair);
                if (drawnPairs.Count > lineRenderers.Count)
                {
                    lineRenderers.Add(GenerateRenderer(drawnPairs.Count-1));
                }

                DrawLine(node1.transform.position, node2.transform.position, lineRenderers[drawnPairs.Count-1]);
            }
        }

        private LineRenderer GenerateRenderer(int id)
        {
            GameObject renderWrapper = new GameObject($"LineRenderer{id}");
            LineRenderer newRenderer = renderWrapper.AddComponent<LineRenderer>();

            newRenderer.material = LineMaterial;
            newRenderer.startColor = Color.green;
            newRenderer.endColor = Color.green;
            newRenderer.startWidth = 0.1f;
            newRenderer.endWidth = 0.1f;

            return newRenderer;
        }

        private void DrawLine(Vector3 start, Vector3 end, LineRenderer lineRenderer)
        {
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }
    }
}