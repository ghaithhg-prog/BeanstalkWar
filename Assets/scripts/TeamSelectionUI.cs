using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class TeamSelectionUI : MonoBehaviour
{
    public GameObject panel;
    public Button protectorButton;
    public Button destroyerButton;

    void Awake()
    {
        protectorButton.onClick.AddListener(
            () => SelectTeam(PlayerMovement.TeamType.Protector)
        );

        destroyerButton.onClick.AddListener(
            () => SelectTeam(PlayerMovement.TeamType.Destroyer)
        );
    }

    void Update()
    {
        if (panel == null)
            return;

        // لا نظهر الاختيار قبل الاتصال
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsClient)
        {
            panel.SetActive(false);
            return;
        }

        NetworkObject player =
            NetworkManager.Singleton.SpawnManager
                .GetLocalPlayerObject();

        if (player == null)
            return;

        PlayerMovement movement =
            player.GetComponent<PlayerMovement>();

        if (movement == null)
            return;

        // يظهر فقط إلى أن يختار اللاعب فريقه
        panel.SetActive(
            !movement.hasSelectedTeam.Value
        );
    }

    void SelectTeam(PlayerMovement.TeamType selectedTeam)
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkObject player =
            NetworkManager.Singleton.SpawnManager
                .GetLocalPlayerObject();

        if (player == null)
            return;

        PlayerMovement movement =
            player.GetComponent<PlayerMovement>();

        if (movement == null)
            return;

        movement.RequestTeam(selectedTeam);
    }
}