using UnityEngine;
using TMPro;

public class UIConnector : MonoBehaviour
{
    void Start()
    {
        // ✅ ربط RespawnText تلقائياً
        Health health = GetComponent<Health>();
        if (health != null && health.respawnText == null)
        {
            GameObject respawnObj = GameObject.Find("RespawnText");
            if (respawnObj != null)
                health.respawnText = respawnObj.GetComponent<TextMeshProUGUI>();
        }
    }
}