using UnityEngine;
using Unity.Netcode;

public class BotManager : MonoBehaviour
{
    public static BotManager Instance;

    [Header("Bot")]
    public GameObject botPrefab;

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

        if (botPrefab == null)
        {
            Debug.LogError(
                "BotManager: Bot Prefab is missing."
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

        // اللاعب الحقيقي + 4 Bots = 5
        for (int i = 0; i < 4; i++)
        {
            SpawnBot(humanTeam);
        }

        // الفريق الآخر = 5 Bots
        for (int i = 0; i < 5; i++)
        {
            SpawnBot(enemyTeam);
        }

        Debug.Log(
            "Offline bots created: 4 allies + 5 enemies."
        );
    }

    void SpawnBot(
        PlayerMovement.TeamType team)
    {
        Transform spawnPoint = null;

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

        BotController controller =
            botObject.GetComponent<BotController>();

        if (controller != null)
            controller.team = team;

        CharacterCombat combat =
            botObject.GetComponent<CharacterCombat>();

        if (combat != null)
            combat.team = team;

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
    }
}