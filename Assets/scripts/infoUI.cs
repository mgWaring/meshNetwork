using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Managers;

public class infoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageCount;
    [SerializeField] private TextMeshProUGUI deleteCount;
    [SerializeField] private TextMeshProUGUI uniqueMessageCount;
    [SerializeField] private TextMeshProUGUI deliveriesCount;

    void Start()
    {
        NetworkStateManager.Instance.onChangeMessageCount += UpdateMessageCount;
        NetworkStateManager.Instance.onChangeDeleteCount += UpdateDeleteCount;
        NetworkStateManager.Instance.onChangeDeliveriesCount += UpdateDeliveriesCount;
        NetworkStateManager.Instance.onChangeUniqueMessageCount += UpdateUniqueMessageCount; 
    }
    void OnDestroy()
    {
        NetworkStateManager.Instance.onChangeMessageCount -= UpdateMessageCount;
        NetworkStateManager.Instance.onChangeDeleteCount -= UpdateDeleteCount;
        NetworkStateManager.Instance.onChangeDeliveriesCount -= UpdateDeliveriesCount;
        NetworkStateManager.Instance.onChangeUniqueMessageCount -= UpdateUniqueMessageCount;
    }

    public void UpdateMessageCount()
    {
        messageCount.text = $"{NetworkStateManager.Instance.messageCount}";
    }
    public void UpdateDeleteCount()
    {
        deleteCount.text = $"{NetworkStateManager.Instance.deleteCount}";
    }
    public void UpdateDeliveriesCount()
    {
        deliveriesCount.text = $"{NetworkStateManager.Instance.deliveriesCount}";
    }
    public void UpdateUniqueMessageCount()
    {
        uniqueMessageCount.text = $"{NetworkStateManager.Instance.uniqueMessageCount}";
    }
}
