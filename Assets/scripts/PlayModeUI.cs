using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayModeUI : MonoBehaviour
{
    public enum PlayMode
    {
        None,
        Online,
        Offline
    }

    public static PlayMode SelectedMode =
        PlayMode.None;

    public GameObject panel;
    public Button onlineButton;
    public Button offlineButton;

    void Awake()
    {
        onlineButton.onClick.AddListener(
            SelectOnline
        );

        offlineButton.onClick.AddListener(
            SelectOffline
        );
    }

    void SelectOnline()
    {
        SelectedMode = PlayMode.Online;

        Debug.Log("ONLINE mode selected");

        panel.SetActive(false);
    }

   void SelectOffline()
{
    SelectedMode = PlayMode.Offline;

    Debug.Log("OFFLINE mode selected");

    panel.SetActive(false);

    if (Unity.Netcode.NetworkManager.Singleton != null)
    {
        Unity.Netcode.NetworkManager.Singleton.StartHost();
    }
}
}