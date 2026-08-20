using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    [Header("Protector Spawn Points")]
    public Transform[] protectorSpawns;

    [Header("Destroyer Spawn Points")]
    public Transform[] destroyerSpawns;

    private int nextProtectorSpawn = 0;
    private int nextDestroyerSpawn = 0;

    void Awake()
    {
        Instance = this;
    }

    public Transform GetSpawnPoint(PlayerMovement.TeamType team)
    {
        Transform[] spawns;

        if (team == PlayerMovement.TeamType.Protector)
            spawns = protectorSpawns;
        else
            spawns = destroyerSpawns;

        if (spawns == null || spawns.Length == 0)
        {
            Debug.LogError("No spawn points configured for " + team);
            return null;
        }

        int index;

        if (team == PlayerMovement.TeamType.Protector)
        {
            index = nextProtectorSpawn % spawns.Length;
            nextProtectorSpawn++;
        }
        else
        {
            index = nextDestroyerSpawn % spawns.Length;
            nextDestroyerSpawn++;
        }

        return spawns[index];
    }
}