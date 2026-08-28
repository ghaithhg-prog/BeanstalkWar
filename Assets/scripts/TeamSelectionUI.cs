using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class TeamSelectionUI : MonoBehaviour
{
    [Header("Team Selection")]
    public GameObject panel;
    public Button protectorButton;
    public Button destroyerButton;

    [Header("Next Screen")]
    public CharacterSelectionUI characterSelectionUI;

    private bool characterScreenOpened = false;

    void Awake()
    {
        protectorButton.onClick.AddListener(
            () => SelectTeam(
                PlayerMovement.TeamType.Protector
            )
        );

        destroyerButton.onClick.AddListener(
            () => SelectTeam(
                PlayerMovement.TeamType.Destroyer
            )
        );
    }

    void Update()
    {
        if (panel == null)
            return;

        // لم نتصل بالشبكة بعد
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsClient)
        {
            panel.SetActive(false);
            return;
        }

        NetworkObject player =
            NetworkManager.Singleton
                .SpawnManager
                .GetLocalPlayerObject();

        if (player == null)
            return;

        PlayerMovement movement =
            player.GetComponent<PlayerMovement>();

        if (movement == null)
            return;

        // لم يختر الفريق بعد
        if (!movement.hasSelectedTeam.Value)
        {
            panel.SetActive(true);
            return;
        }

        // الفريق تم اختياره
        panel.SetActive(false);

        // افتح اختيار الشخصية مرة واحدة
        if (!characterScreenOpened)
        {
            characterScreenOpened = true;

            if (characterSelectionUI != null)
                characterSelectionUI.Show();
        }
    }

    void SelectTeam(
        PlayerMovement.TeamType selectedTeam)
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkObject player =
            NetworkManager.Singleton
                .SpawnManager
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