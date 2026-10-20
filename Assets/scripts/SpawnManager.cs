using UnityEngine;
using UnityEngine.AI;

public class SpawnManager : MonoBehaviour
{
    private static SpawnManager _instance;
    public static SpawnManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SpawnManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("SpawnManager");
                    _instance = obj.AddComponent<SpawnManager>();
                }
            }
            return _instance;
        }
        set => _instance = value;
    }

    [Header("Protector Spawn Points")]
    public Transform protectorSpawn;
    public Transform[] protectorSpawns;

    [Header("Destroyer Spawn Points")]
    public Transform destroyerSpawn;
    public Transform[] destroyerSpawns;

    private int nextProtectorSpawn = 0;
    private int nextDestroyerSpawn = 0;

    void Awake()
    {
        _instance = this;
        AutoFindSpawns();
    }

    public void AutoFindSpawns()
    {
        if (protectorSpawn == null)
        {
            GameObject pObj = GameObject.Find("ProtectorSpawn");
            if (pObj != null) protectorSpawn = pObj.transform;
        }

        if (destroyerSpawn == null)
        {
            GameObject dObj = GameObject.Find("DestroyerSpawn");
            if (dObj != null) destroyerSpawn = dObj.transform;
        }

        // استخراج نقاط التفريخ للأبناء تلقائياً من ProtectorSpawn
        if ((protectorSpawns == null || protectorSpawns.Length == 0) && protectorSpawn != null)
        {
            int childCount = protectorSpawn.childCount;
            if (childCount > 0)
            {
                protectorSpawns = new Transform[childCount];
                for (int c = 0; c < childCount; c++)
                {
                    protectorSpawns[c] = protectorSpawn.GetChild(c);
                }
            }
        }

        // استخراج نقاط التفريخ للأبناء تلقائياً من DestroyerSpawn
        if ((destroyerSpawns == null || destroyerSpawns.Length == 0) && destroyerSpawn != null)
        {
            int childCount = destroyerSpawn.childCount;
            if (childCount > 0)
            {
                destroyerSpawns = new Transform[childCount];
                for (int c = 0; c < childCount; c++)
                {
                    destroyerSpawns[c] = destroyerSpawn.GetChild(c);
                }
            }
        }
    }

    /// <summary>
    /// يحسب موقع تفريخ تكتيكي فريد لكل بوت أو لاعب على سطح NavMesh مباشرة لمنع التكدس والتجمد
    /// </summary>
    public Vector3 GetSpawnPosition(PlayerMovement.TeamType team, int index = -1)
    {
        AutoFindSpawns();

        Transform[] spawns = (team == PlayerMovement.TeamType.Protector) ? protectorSpawns : destroyerSpawns;
        Transform singleSpawn = (team == PlayerMovement.TeamType.Protector) ? protectorSpawn : destroyerSpawn;

        Vector3 basePos = (team == PlayerMovement.TeamType.Protector)
            ? (singleSpawn != null ? singleSpawn.position : new Vector3(-15f, 0.5f, 0f))
            : (singleSpawn != null ? singleSpawn.position : new Vector3(15f, 0.5f, 0f));

        if (spawns != null && spawns.Length > 1)
        {
            int slot = (index >= 0)
                ? (index % spawns.Length)
                : (team == PlayerMovement.TeamType.Protector ? (nextProtectorSpawn++ % spawns.Length) : (nextDestroyerSpawn++ % spawns.Length));

            if (spawns[slot] != null)
            {
                basePos = spawns[slot].position;
            }
        }
        else if (spawns != null && spawns.Length == 1 && spawns[0] != null)
        {
            basePos = spawns[0].position;
        }

        // توزيع تكتيكي فريد واسع لكل وحدة (Index > 0) لمنع تداخل اللاعبين والبوتات فوق بعضهم نهائياً
        if (index > 0)
        {
            float angle = (index * 72f) * Mathf.Deg2Rad;
            float radius = 3.2f + (index * 0.45f);
            basePos += new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        // معايرة الموقع بدقة فائقة على NavMesh لضمان حركة البوتات الفورية
        if (NavMesh.SamplePosition(basePos, out NavMeshHit hit, 8f, NavMesh.AllAreas))
        {
            return hit.position + Vector3.up * 0.15f;
        }

        return basePos + Vector3.up * 0.5f;
    }

    public Transform GetSpawnPoint(PlayerMovement.TeamType team)
    {
        AutoFindSpawns();

        Transform[] spawns = (team == PlayerMovement.TeamType.Protector) ? protectorSpawns : destroyerSpawns;
        Transform singleSpawn = (team == PlayerMovement.TeamType.Protector) ? protectorSpawn : destroyerSpawn;

        if (spawns != null && spawns.Length > 0)
        {
            int index = (team == PlayerMovement.TeamType.Protector)
                ? (nextProtectorSpawn++ % spawns.Length)
                : (nextDestroyerSpawn++ % spawns.Length);

            if (spawns[index] != null)
                return spawns[index];
        }

        if (singleSpawn != null)
            return singleSpawn;

        // نقطة نجاة افتراضية بعيداً عن مركز الشجرة
        GameObject fallback = new GameObject("Fallback_" + team);
        fallback.transform.position = (team == PlayerMovement.TeamType.Destroyer) 
            ? new Vector3(15f, 1f, 0f) 
            : new Vector3(-15f, 1f, 0f);

        return fallback.transform;
    }
}