using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class BotManager : MonoBehaviour
{
    public static BotManager Instance;

    [Header("Bot")]
    public GameObject botPrefab;

    [Header("Characters")]
    public CharacterDatabase characterDatabase;

    private bool offlineBotsCreated = false;

    void Awake()
    {
        Instance = this;
    }

    // =========================
    // Offline 5v5
    // =========================

    public void CreateOfflineBots(
        PlayerMovement humanPlayer)
    {
        if (offlineBotsCreated)
            return;

        if (humanPlayer == null)
            return;

        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsServer)
            return;

        if (botPrefab == null ||
            characterDatabase == null)
        {
            Debug.LogError(
                "BotManager references are missing."
            );

            return;
        }

        offlineBotsCreated = true;

        PlayerMovement.TeamType humanTeam =
            humanPlayer.networkTeam.Value;

        PlayerMovement.TeamType enemyTeam =
            humanTeam ==
            PlayerMovement.TeamType.Protector
                ? PlayerMovement.TeamType.Destroyer
                : PlayerMovement.TeamType.Protector;

        // =========================
        // فريق اللاعب
        // =========================

        List<CharacterData> humanComposition =
            new List<CharacterData>();

        CharacterData humanCharacter =
            characterDatabase.GetCharacter(
                humanPlayer.selectedCharacterID.Value
            );

        if (humanCharacter != null)
        {
            humanComposition.Add(
                humanCharacter
            );
        }

        // اللاعب الحقيقي + 4 Bots
        for (int i = 0; i < 4; i++)
        {
            CharacterData selected =
                ChooseBestCharacter(
                    humanComposition
                );

            if (selected == null)
                break;

            humanComposition.Add(
                selected
            );

            SpawnBot(
                humanTeam,
                selected
            );
        }

        // =========================
        // الفريق الخصم
        // =========================

        List<CharacterData> enemyComposition =
            new List<CharacterData>();

        // 5 Bots
        for (int i = 0; i < 5; i++)
        {
            CharacterData selected =
                ChooseBestCharacter(
                    enemyComposition
                );

            if (selected == null)
                break;

            enemyComposition.Add(
                selected
            );

            SpawnBot(
                enemyTeam,
                selected
            );
        }

        // =========================
        // Debug
        // =========================

        Debug.Log(
            "Offline smart 5v5 teams created."
        );

        PrintComposition(
            "Human Team",
            humanComposition
        );

        PrintComposition(
            "Enemy Team",
            enemyComposition
        );

        // =========================
        // Start Match Countdown
        // =========================

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance
                .RequestStartGame();
        }
        else
        {
            Debug.LogError(
                "GameStateManager Instance not found."
            );
        }
    }

    // =========================
    // Smart Character Selection
    // =========================

    CharacterData ChooseBestCharacter(
        List<CharacterData> currentTeam)
    {
        // أولاً:
        // نضمن Tank + Support + Control

        CharacterData.CharacterRole[] requiredRoles =
        {
            CharacterData.CharacterRole.Tank,
            CharacterData.CharacterRole.Support,
            CharacterData.CharacterRole.Control
        };

        foreach (
            CharacterData.CharacterRole role
            in requiredRoles)
        {
            if (!HasRole(
                    currentTeam,
                    role))
            {
                CharacterData candidate =
                    FindAvailableCharacter(
                        role,
                        currentTeam
                    );

                if (candidate != null)
                    return candidate;
            }
        }

        // =========================
        // بعد اكتمال الأدوار الأساسية
        // =========================

        // نعطي الأولوية للهجوم
        // حتى لا تتكرر Tanks وSupports
        // بلا حاجة.

        CharacterData.CharacterRole[] fillPriorities =
        {
            CharacterData.CharacterRole.Damage,
            CharacterData.CharacterRole.Specialist,
            CharacterData.CharacterRole.Control,
            CharacterData.CharacterRole.Support,
            CharacterData.CharacterRole.Tank
        };

        foreach (
            CharacterData.CharacterRole role
            in fillPriorities)
        {
            CharacterData candidate =
                FindAvailableCharacter(
                    role,
                    currentTeam
                );

            if (candidate != null)
                return candidate;
        }

        return null;
    }

    // =========================
    // Has Role
    // =========================

    bool HasRole(
        List<CharacterData> team,
        CharacterData.CharacterRole role)
    {
        foreach (
            CharacterData character
            in team)
        {
            if (character != null &&
                character.role == role)
            {
                return true;
            }
        }

        return false;
    }

    // =========================
    // Find Available Character
    // =========================

    CharacterData FindAvailableCharacter(
        CharacterData.CharacterRole role,
        List<CharacterData> currentTeam)
    {
        List<CharacterData> candidates =
            new List<CharacterData>();

        foreach (
            CharacterData character
            in characterDatabase.characters)
        {
            if (character == null)
                continue;

            if (character.role != role)
                continue;

            int count =
                CountCharacter(
                    currentTeam,
                    character.characterID
                );

            // احترام Max Per Team
            if (count >=
                character.maxPerTeam)
            {
                continue;
            }

            candidates.Add(
                character
            );
        }

        if (candidates.Count == 0)
            return null;

        // اختيار عشوائي من الشخصيات
        // المتاحة داخل الدور المطلوب.

        return candidates[
            Random.Range(
                0,
                candidates.Count
            )
        ];
    }

    // =========================
    // Count Character
    // =========================

    int CountCharacter(
        List<CharacterData> team,
        int characterID)
    {
        int count = 0;

        foreach (
            CharacterData character
            in team)
        {
            if (character != null &&
                character.characterID ==
                characterID)
            {
                count++;
            }
        }

        return count;
    }

    // =========================
    // Spawn Bot
    // =========================

    void SpawnBot(
        PlayerMovement.TeamType team,
        CharacterData character)
    {
        Transform spawnPoint =
            null;

        if (SpawnManager.Instance != null)
        {
            spawnPoint =
                SpawnManager.Instance
                    .GetSpawnPoint(team);
        }

        Vector3 position =
            spawnPoint != null
                ? spawnPoint.position
                : Vector3.zero;

        Quaternion rotation =
            spawnPoint != null
                ? spawnPoint.rotation
                : Quaternion.identity;

        GameObject botObject =
            Instantiate(
                botPrefab,
                position,
                rotation
            );

        // =========================
        // Bot Controller
        // =========================

        BotController controller =
            botObject.GetComponent<BotController>();

        if (controller != null)
        {
            controller.team =
                team;
        }

        // =========================
        // Combat
        // =========================

        CharacterCombat combat =
            botObject.GetComponent<CharacterCombat>();

        if (combat != null)
        {
            combat.team =
                team;

            combat.characterData =
                character;

            combat.attackDamage =
                character.attackDamage;

            combat.attackCooldown =
                character.attackCooldown;

            combat.attackRange =
                character.attackRange;

            combat.attackType =
                character.attackType;
        }

        // =========================
        // Health
        // =========================

        Health health =
            botObject.GetComponent<Health>();

        if (health != null)
        {
            health.maxHealth =
                character.maxHealth;

            health.currentHealth =
                character.maxHealth;
        }

        // =========================
        // Network
        // =========================

        NetworkObject networkObject =
            botObject.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            networkObject.Spawn();
        }
        else
        {
            Debug.LogError(
                "Bot prefab has no NetworkObject."
            );
        }

        // =========================
        // Debug Bot
        // =========================

        Debug.Log(
            "Spawned Bot | Team: " +
            team +
            " | Character: " +
            character.characterName +
            " | Role: " +
            character.role
        );
    }

    // =========================
    // Debug Composition
    // =========================

    void PrintComposition(
        string title,
        List<CharacterData> team)
    {
        string result =
            title + ": ";

        foreach (
            CharacterData character
            in team)
        {
            result +=
                "[" +
                character.characterName +
                " - " +
                character.role +
                "] ";
        }

        Debug.Log(result);
    }
}