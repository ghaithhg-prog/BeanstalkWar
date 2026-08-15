using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public Transform protectorSpawn;
    public Transform destroyerSpawn;

    public static SpawnManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public Transform GetSpawnPoint(PlayerMovement.TeamType team)
    {
        if (team == PlayerMovement.TeamType.Protector)
            return protectorSpawn;
        else
            return destroyerSpawn;
    }
}