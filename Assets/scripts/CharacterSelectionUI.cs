using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class CharacterSelectionUI : MonoBehaviour
{
    [Header("Data")]
    public CharacterDatabase database;

    [Header("Character Grid")]
    public GameObject panel;
    public Transform content;
    public CharacterCardUI cardPrefab;

    [Header("Selected Character Info")]
    public TextMeshProUGUI selectedNameText;
    public TextMeshProUGUI selectedRoleText;
    public TextMeshProUGUI selectedStatsText;
    public Button confirmButton;

    private bool generated = false;
    private CharacterData selectedCharacter;

    // =========================
    // Awake
    // =========================

    void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(
                ConfirmSelection
            );

            confirmButton.interactable = false;
        }
    }

    // =========================
    // Update
    // =========================

    void Update()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsClient)
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

        // السيرفر وافق على اختيار الشخصية
        if (movement.hasSelectedCharacter.Value)
        {
            Hide();
        }
    }

    // =========================
    // Show / Hide
    // =========================

    public void Show()
    {
        if (panel == null)
            return;

        panel.SetActive(true);

        if (!generated)
        {
            GenerateCards();
            generated = true;
        }

        ClearSelection();
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    // =========================
    // Generate Characters
    // =========================

    void GenerateCards()
    {
        if (database == null ||
            content == null ||
            cardPrefab == null)
        {
            Debug.LogError(
                "CharacterSelectionUI references are missing."
            );

            return;
        }

        foreach (
            CharacterData character
            in database.characters)
        {
            if (character == null)
                continue;

            CharacterCardUI card =
                Instantiate(
                    cardPrefab,
                    content
                );

            card.Setup(
                character,
                0,
                OnCharacterSelected
            );
        }
    }

    // =========================
    // Select Character
    // =========================

    void OnCharacterSelected(
        int characterID)
    {
        CharacterData data =
            database.GetCharacter(
                characterID
            );

        if (data == null)
            return;

        selectedCharacter = data;

        if (selectedNameText != null)
        {
            selectedNameText.text =
                data.characterName;
        }

        if (selectedRoleText != null)
        {
            selectedRoleText.text =
                "Role: " +
                data.role;
        }

        if (selectedStatsText != null)
        {
            selectedStatsText.text =
                "Health: " +
                data.maxHealth +

                "\nDamage: " +
                data.attackDamage +

                "\nSpeed: " +
                data.moveSpeed +

                "\nAttack Range: " +
                data.attackRange +

                "\nAttack Cooldown: " +
                data.attackCooldown;
        }

        if (confirmButton != null)
        {
            confirmButton.interactable =
                true;
        }
    }

    // =========================
    // Confirm
    // =========================

    void ConfirmSelection()
    {
        if (selectedCharacter == null)
            return;

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

        Debug.Log(
            "Confirm character: " +
            selectedCharacter.characterName +
            " ID: " +
            selectedCharacter.characterID
        );

        movement.RequestCharacter(
            selectedCharacter.characterID
        );

        // لا نخفي الشاشة هنا.
        // ننتظر موافقة السيرفر.
    }

    // =========================
    // Clear
    // =========================

    void ClearSelection()
    {
        selectedCharacter = null;

        if (selectedNameText != null)
        {
            selectedNameText.text =
                "Select a Character";
        }

        if (selectedRoleText != null)
        {
            selectedRoleText.text = "";
        }

        if (selectedStatsText != null)
        {
            selectedStatsText.text = "";
        }

        if (confirmButton != null)
        {
            confirmButton.interactable =
                false;
        }
    }
}