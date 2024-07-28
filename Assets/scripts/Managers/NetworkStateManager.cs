using System;
using System.Collections.Generic;
using UnityEngine;
using Utility;
using TMPro;

namespace Managers
{
    public class NetworkStateManager : LonelyMonoBehaviour<NetworkStateManager>
    {
        public int messageCount { get; private set; } = 0;
        public int deleteCount { get; private set; } = 0;
        public int uniqueMessageCount { get; private set; } = 0;
        public int deliveriesCount { get; private set; } = 0;
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
        private HashSet<(Guid, Guid)> connectedPairs = new HashSet<(Guid, Guid)>();
        private HashSet<(Guid, Guid)> blockedPairs = new HashSet<(Guid, Guid)>();

        [SerializeField] private GameObject Teams;
        [SerializeField] private Material ConnectionMaterial;
        [SerializeField] private Material BlockageMaterial;
        [SerializeField] private Transform[] goals;

        private List<LineRenderer> connectionRenderers = new();
        private List<LineRenderer> blockageRenderers = new();

        public event Action onChangeMessageCount; 
        public event Action onChangeDeleteCount; 
        public event Action onChangeUniqueMessageCount;
        public event Action onChangeDeliveriesCount;

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
        public void UpdateMessageCount(int count)
        {
            messageCount = count;
            onChangeMessageCount?.Invoke();
        }
        public void UpdateDeleteCount(int count)
        {
            deleteCount = count;
            onChangeDeleteCount?.Invoke();
        }

        public void UpdateUniqueMessageCount(int count)
        {
            uniqueMessageCount = count;
            onChangeUniqueMessageCount?.Invoke();
        }
        public void UpdateDeliveriesCount(int count)
        {
            deliveriesCount = count;
            onChangeDeliveriesCount?.Invoke();
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
            senders[senderDropdown.value].SendMessage(GetDestinationUID(), GetMessage());
            BeginTravel();
        }

        private void BeginTravel()
        {
            int teamIndex = 0;
            foreach (Transform team in Teams.transform)
            {
                foreach (Transform child in team)
                {
                    Traveller traveller = child.GetComponentInChildren<Traveller>();
                    Node tnode = child.GetComponentInChildren<Node>();
                    if (traveller != null)
                    {
                        traveller.BeginTravel(goals[teamIndex]);
                    }
                }
                teamIndex++;
            }
        }

        private void HaltTravel()
        {
            foreach (Transform team in Teams.transform)
            {
                foreach (Transform child in team)
                {
                    Traveller traveller = child.GetComponentInChildren<Traveller>();
                    if (traveller != null)
                    {
                        traveller.HaltTravel();
                    }
                }
            }
        }

        public void DrawLines(List<Node> nodes)
        {
            connectedPairs.Clear();
            blockedPairs.Clear();
            foreach (Node node in nodes)
            {
                foreach (Node peer in node.Peers)
                {
                    DrawLineIfNotDrawn(node, peer, connectedPairs, connectionRenderers, "connected");
                }
                foreach (Node peer in node.BlockedPeers)
                {
                    DrawLineIfNotDrawn(node, peer, blockedPairs, blockageRenderers, "blocked");
                }
            }
            ResetRenderers(connectionRenderers, connectedPairs);
            ResetRenderers(blockageRenderers, blockedPairs);
        }

        private void ResetRenderers(List<LineRenderer> renderers, HashSet<(Guid, Guid)> pairs)
        {
            //instantiation is expensive, so lets keep our pool of renderers and just reset the lines
            if (renderers.Count > pairs.Count)
            {
                for (int i = pairs.Count; i < renderers.Count; i++)
                {
                    renderers[i].SetPosition(0, Vector3.zero);
                    renderers[i].SetPosition(1, Vector3.zero);
                }
            }
        }

        private void DrawLineIfNotDrawn(
            Node node1,
            Node node2,
            HashSet<(Guid, Guid)> drawnPairs,
            List<LineRenderer>
            lineRenderers,
            string type
        )
        {
            Guid id1 = node1.nodeId;
            Guid id2 = node2.nodeId;

            // Ensure the pair is sorted consistently
            (Guid, Guid) pair = id1.CompareTo(id2) < 0 ? (id1, id2) : (id2, id1);

            if (!drawnPairs.Contains(pair))
            {
                drawnPairs.Add(pair);
                if (drawnPairs.Count > lineRenderers.Count)
                {
                    lineRenderers.Add(GenerateRenderer(drawnPairs.Count - 1, type));
                }

                DrawLine(node1.transform.position, node2.transform.position, lineRenderers[drawnPairs.Count - 1]);
            }
        }

        private LineRenderer GenerateRenderer(int id, string type)
        {
            GameObject renderWrapper = new GameObject($"LineRenderer{id}");
            LineRenderer newRenderer = renderWrapper.AddComponent<LineRenderer>();

            Color color = type == "connected" ? Color.green : Color.red;
            newRenderer.material = "connected" == type ? ConnectionMaterial : BlockageMaterial;
            newRenderer.startColor = color;
            newRenderer.endColor = color;
            newRenderer.startWidth = 0.1f;
            newRenderer.endWidth = 0.1f;

            return newRenderer;
        }

        private void DrawLine(Vector3 start, Vector3 end, LineRenderer lineRenderer)
        {
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }

        public void ResetScenario()
        {
            HaltTravel();
            foreach (Node node in nodes)
            {
                node.Reset();
            }
            UpdateDeleteCount(0);
            UpdateMessageCount(0);
            UpdateUniqueMessageCount(0);
            UpdateDeliveriesCount(0);
        }
    }
}