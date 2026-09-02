using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public class BotController : NetworkBehaviour
{
    public enum BotState
    {
        Defend,
        Advance
    }

    [Header("Team")]
    public PlayerMovement.TeamType team;

    [Header("AI")]
    public BotState currentState;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float acceleration = 18f;
    public float angularSpeed = 360f;
    public float stoppingDistance = 0.8f;

    [Header("Target")]
    public Transform treeTarget;

    [Header("Levels")]
    public Transform[] levelTargets;

    // TopLevel محفوظ لهدف الكريستال لاحقًا
    public Transform topLevelTarget;

    [Header("Protector")]
    public float minDefenseRadius = 5f;
    public float maxDefenseRadius = 8f;

    [Header("Decision")]
    public float decisionInterval = 1f;

    private NavMeshAgent agent;

    private Vector3 currentDestination;
    private bool hasDestination = false;

    private Transform currentTargetLevel;
    private Vector3 protectorTarget;

    private bool agentReady = false;
    private float nextDecisionTime = 0f;

    private float personalOffset;

    // =========================
    // Network Spawn
    // =========================

    public override void OnNetworkSpawn()
    {
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

        personalOffset =
            Random.Range(-0.5f, 0.5f);

        ConfigureAgent();

        FindTree();
        FindLevels();
        PrepareAgent();

        ChooseInitialState();
    }

    // =========================
    // Agent Settings
    // =========================

    void ConfigureAgent()
    {
        agent.speed =
            moveSpeed +
            personalOffset;

        agent.acceleration =
            acceleration;

        agent.angularSpeed =
            angularSpeed;

        agent.stoppingDistance =
            stoppingDistance;

        agent.autoBraking = true;
        agent.autoRepath = true;

        agent.avoidancePriority =
            Random.Range(20, 80);
    }

    // =========================
    // Prepare Agent
    // =========================

    void PrepareAgent()
    {
        if (agent == null)
            return;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(
                transform.position,
                out hit,
                5f,
                NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            agentReady = true;
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
    }

    // =========================
    // Find Levels
    // =========================

    void FindLevels()
    {
        GameObject treeLevels =
            GameObject.Find(
                "TreeLevels"
            );

        if (treeLevels == null)
        {
            Debug.LogWarning(
                "TreeLevels not found."
            );

            return;
        }

        // مهم:
        // هذه القائمة تحتوي المستويات القتالية فقط.
        // TopLevel ليس ضمن Advance.

        levelTargets =
            new Transform[3];

        levelTargets[0] =
            treeLevels.transform.Find(
                "Level_1"
            );

        levelTargets[1] =
            treeLevels.transform.Find(
                "Level_2"
            );

        levelTargets[2] =
            treeLevels.transform.Find(
                "Level_3"
            );

        // نخزنه منفصلًا للكريستال لاحقًا.
        topLevelTarget =
            treeLevels.transform.Find(
                "TopLevel"
            );
    }

    // =========================
    // Initial State
    // =========================

    void ChooseInitialState()
    {
        if (team ==
            PlayerMovement.TeamType.Destroyer)
        {
            currentState =
                BotState.Advance;
        }
        else
        {
            if (Random.value < 0.5f)
            {
                currentState =
                    BotState.Defend;

                FindProtectorPosition();
            }
            else
            {
                currentState =
                    BotState.Advance;
            }
        }

        hasDestination = false;
        currentTargetLevel = null;

        Debug.Log(
            "Bot State: " +
            currentState +
            " | Team: " +
            team
        );
    }

    // =========================
    // Update
    // =========================

    void Update()
    {
        if (!IsServer ||
            agent == null)
        {
            return;
        }

        if (GameStateManager.Instance == null ||
            !GameStateManager.Instance.IsPlaying())
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

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

        if (Time.time >=
            nextDecisionTime)
        {
            nextDecisionTime =
                Time.time +
                decisionInterval;

            UpdateDecision();
        }

        switch (currentState)
        {
            case BotState.Defend:

                UpdateDefend();
                break;

            case BotState.Advance:

                UpdateAdvance();
                break;
        }
    }

    // =========================
    // Decision
    // =========================

    void UpdateDecision()
    {
        if (treeTarget == null)
        {
            FindTree();
        }

        if (levelTargets == null ||
            levelTargets.Length == 0)
        {
            FindLevels();
        }

        // لاحقًا هنا سنضيف:
        //
        // Fight
        // Tree Health
        // Character Role
        // Mini Boss
        // Crystal
        //
        // TopLevel لن يصبح هدفًا
        // إلا عند وجود سبب متعلق بالكريستال.
    }

    // =========================
    // Defend
    // =========================

    void UpdateDefend()
    {
        if (treeTarget == null)
            return;

        if (!hasDestination)
        {
            FindProtectorPosition();

            SetNewDestination(
                protectorTarget
            );
        }
    }

    // =========================
    // Advance
    // =========================

    void UpdateAdvance()
    {
        // أعلى هدف هنا هو Level_3 فقط.
        Transform highestLevel =
            GetHighestCombatLevel();

        if (highestLevel == null)
        {
            if (!hasDestination)
            {
                SetNewDestination(
                    FindClosestTreePosition()
                );
            }

            return;
        }

        // لا نغير الهدف إلا عند فتح
        // مستوى قتالي أعلى.
        if (currentTargetLevel !=
            highestLevel)
        {
            currentTargetLevel =
                highestLevel;

            Vector3 newTarget =
                FindPointOnLevel(
                    highestLevel
                );

            SetNewDestination(
                newTarget
            );

            return;
        }

        if (!hasDestination)
        {
            Vector3 newTarget =
                FindPointOnLevel(
                    highestLevel
                );

            SetNewDestination(
                newTarget
            );
        }
    }

    // =========================
    // Highest Combat Level
    // =========================

    Transform GetHighestCombatLevel()
    {
        if (levelTargets == null)
            return null;

        // levelTargets يحتوي:
        //
        // Level_1
        // Level_2
        // Level_3
        //
        // ولا يحتوي TopLevel.

        for (int i =
             levelTargets.Length - 1;
             i >= 0;
             i--)
        {
            Transform level =
                levelTargets[i];

            if (level == null)
                continue;

            if (!level.gameObject
                    .activeInHierarchy)
            {
                continue;
            }

            return level;
        }

        return null;
    }

    // =========================
    // New Destination
    // =========================

    void SetNewDestination(
        Vector3 destination)
    {
        if (agent == null ||
            !agent.isOnNavMesh)
        {
            return;
        }

        currentDestination =
            destination;

        hasDestination = true;

        agent.SetDestination(
            currentDestination
        );
    }

    // =========================
    // Point On Level
    // =========================

    Vector3 FindPointOnLevel(
        Transform level)
    {
        if (level == null)
            return transform.position;

        Renderer renderer =
            level.GetComponent<Renderer>();

        Vector3 center =
            level.position;

        float radius = 3f;

        if (renderer != null)
        {
            radius =
                Mathf.Min(
                    renderer.bounds.extents.x,
                    renderer.bounds.extents.z
                );

            radius *= 0.65f;

            center =
                renderer.bounds.center;
        }

        for (int attempt = 0;
             attempt < 12;
             attempt++)
        {
            Vector2 random =
                Random.insideUnitCircle *
                radius;

            Vector3 candidate =
                new Vector3(
                    center.x + random.x,
                    center.y,
                    center.z + random.y
                );

            NavMeshHit hit;

            if (NavMesh.SamplePosition(
                    candidate,
                    out hit,
                    3f,
                    NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        NavMeshHit centerHit;

        if (NavMesh.SamplePosition(
                center,
                out centerHit,
                5f,
                NavMesh.AllAreas))
        {
            return centerHit.position;
        }

        return transform.position;
    }

    // =========================
    // Protector Position
    // =========================

    void FindProtectorPosition()
    {
        if (treeTarget == null)
            return;

        for (int attempt = 0;
             attempt < 12;
             attempt++)
        {
            Vector2 circle =
                Random.insideUnitCircle;

            if (circle.sqrMagnitude <
                0.01f)
            {
                continue;
            }

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
                ) *
                radius;

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

        protectorTarget =
            transform.position;
    }

    // =========================
    // Tree Position
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