using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public class BotController : NetworkBehaviour
{
    [Header("Team")]
    public PlayerMovement.TeamType team;

    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Target")]
    public Transform treeTarget;

    [Header("Protector")]
    public float minDefenseRadius = 5f;
    public float maxDefenseRadius = 8f;

    private NavMeshAgent agent;

    // موقع الحماية
    private Vector3 protectorTarget;

    private bool targetInitialized = false;
    private bool agentReady = false;

    // =========================
    // Network Spawn
    // =========================

    public override void OnNetworkSpawn()
    {
        // السيرفر فقط يشغل AI
        enabled = IsServer;

        if (!IsServer)
            return;

        agent =
            GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError(
                "Bot has no NavMeshAgent."
            );

            enabled = false;
            return;
        }

        agent.speed = moveSpeed;

        FindTree();

        PrepareAgent();
    }

    // =========================
    // Prepare Agent
    // =========================

    void PrepareAgent()
    {
        if (agent == null)
            return;

        // نحاول إيجاد أقرب نقطة NavMesh
        // لمكان ظهور الـBot.

        NavMeshHit hit;

        if (NavMesh.SamplePosition(
                transform.position,
                out hit,
                5f,
                NavMesh.AllAreas))
        {
            agent.Warp(
                hit.position
            );

            agentReady = true;

            InitializeTarget();
        }
        else
        {
            Debug.LogWarning(
                "Bot could not find nearby NavMesh."
            );

            agentReady = false;
        }
    }

    // =========================
    // Find Tree
    // =========================

    void FindTree()
    {
        GameObject tree =
            GameObject.FindGameObjectWithTag(
                "Tree"
            );

        if (tree != null)
        {
            treeTarget =
                tree.transform;
        }
        else
        {
            Debug.LogWarning(
                "Bot could not find Tree."
            );
        }
    }

    // =========================
    // Initialize Target
    // =========================

    void InitializeTarget()
    {
        if (treeTarget == null)
            return;

        if (team ==
            PlayerMovement.TeamType.Protector)
        {
            FindProtectorPosition();
        }

        targetInitialized = true;
    }

    // =========================
    // Protector Position
    // =========================

    void FindProtectorPosition()
    {
        if (treeTarget == null)
            return;

        for (int attempt = 0;
             attempt < 10;
             attempt++)
        {
            Vector2 circle =
                Random.insideUnitCircle;

            if (circle.sqrMagnitude < 0.01f)
                continue;

            circle.Normalize();

            float radius =
                Random.Range(
                    minDefenseRadius,
                    maxDefenseRadius
                );

            Vector3 candidate =
                treeTarget.position +
                new Vector3(
                    circle.x,
                    0f,
                    circle.y
                ) * radius;

            NavMeshHit hit;

            if (NavMesh.SamplePosition(
                    candidate,
                    out hit,
                    4f,
                    NavMesh.AllAreas))
            {
                protectorTarget =
                    hit.position;

                return;
            }
        }

        // fallback
        protectorTarget =
            transform.position;
    }

    // =========================
    // Update
    // =========================

    void Update()
    {
        if (!IsServer)
            return;

        if (agent == null)
            return;

        // لا يتحرك قبل GO
        if (GameStateManager.Instance == null ||
            !GameStateManager.Instance.IsPlaying())
        {
            if (agent.isOnNavMesh)
                agent.isStopped = true;

            return;
        }

        if (!agentReady)
        {
            PrepareAgent();

            if (!agentReady)
                return;
        }

        if (!agent.isOnNavMesh)
        {
            agentReady = false;
            return;
        }

        agent.isStopped = false;

        if (treeTarget == null)
        {
            FindTree();

            if (treeTarget == null)
                return;
        }

        if (!targetInitialized)
            InitializeTarget();

        UpdateDestination();
    }

    // =========================
    // Destination
    // =========================

    void UpdateDestination()
    {
        Vector3 destination;

        if (team ==
            PlayerMovement.TeamType.Destroyer)
        {
            // لا نطلب مركز الشجرة مباشرة
            // لأنه قد لا يكون على NavMesh.
            destination =
                FindClosestTreePosition();
        }
        else
        {
            destination =
                protectorTarget;
        }

        if (!agent.pathPending)
        {
            agent.SetDestination(
                destination
            );
        }
    }

    // =========================
    // Closest Tree Position
    // =========================

    Vector3 FindClosestTreePosition()
    {
        if (treeTarget == null)
            return transform.position;

        Vector3 direction =
            transform.position -
            treeTarget.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <
            0.01f)
        {
            direction =
                Vector3.forward;
        }

        direction.Normalize();

        // نقطة قرب الشجرة وليست داخلها
        Vector3 desired =
            treeTarget.position +
            direction * 4f;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(
                desired,
                out hit,
                5f,
                NavMesh.AllAreas))
        {
            return hit.position;
        }

        return transform.position;
    }
}