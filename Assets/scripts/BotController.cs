using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public class BotController : NetworkBehaviour
{
    public enum BotState
    {
        Defend,
        Advance,
        Fight,
        DeliverCrystal,
        EscortCarrier,
        HuntCarrier,
        GuardDroppedCrystal
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

    [Header("Fight")]
    public float detectionRange = 8f;
    public float loseEnemyRange = 12f;
    public float fightStoppingDistance = 2.5f;
    public float fightTimeout = 6f;

    [Header("Target")]
    public Transform treeTarget;

    [Header("Levels")]
    public Transform[] levelTargets;

    public Transform topLevelTarget;

    [Header("Protector")]
    public float minDefenseRadius = 5f;
    public float maxDefenseRadius = 8f;

    [Header("Decision")]
    public float decisionInterval = 0.5f;

    [Header("Tree Avoidance")]
    public float treeAvoidRadius = 4f;

    private NavMeshAgent agent;
    private Vector3 currentDestination;
    private bool hasDestination = false;
    private Transform currentTargetLevel;
    private Vector3 protectorTarget;
    private bool agentReady = false;
    private float nextDecisionTime = 0f;
    private float personalOffset;

    private Transform enemyTarget;
    private BotState stateBeforeFight;
    private float defendStartTime;
    private float fightStartTime;
    private float destinationSetTime;

    public override void OnNetworkSpawn()
    {
        enabled = IsServer;

        if (!IsServer)
            return;

        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("Bot has no NavMeshAgent.");
            enabled = false;
            return;
        }

        personalOffset = Random.Range(-0.5f, 0.5f);
        ConfigureAgent();
        FindTree();
        FindLevels();
        PrepareAgent();
        ChooseInitialState();
    }

    void ConfigureAgent()
    {
        agent.speed = moveSpeed + personalOffset;
        agent.acceleration = acceleration;
        agent.angularSpeed = angularSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.avoidancePriority = Random.Range(20, 80);
    }

    void PrepareAgent()
    {
        if (agent == null) return;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            agentReady = true;
        }
        else
        {
            agentReady = false;
        }
    }

    void FindTree()
    {
        GameObject tree = GameObject.FindGameObjectWithTag("Tree");
        if (tree != null) treeTarget = tree.transform;
    }

    void FindLevels()
    {
        GameObject treeLevels = GameObject.Find("TreeLevels");
        if (treeLevels == null) return;

        levelTargets = new Transform[3];
        levelTargets[0] = treeLevels.transform.Find("Level_1");
        levelTargets[1] = treeLevels.transform.Find("Level_2");
        levelTargets[2] = treeLevels.transform.Find("Level_3");

        topLevelTarget = treeLevels.transform.Find("TopLevel");
    }

    void ChooseInitialState()
    {
        if (team == PlayerMovement.TeamType.Destroyer)
        {
            if (Random.value < 0.95f)
                currentState = BotState.Advance;
            else
                currentState = BotState.Defend;
        }
        else
        {
            if (Random.value < 0.15f)
            {
                currentState = BotState.Defend;
                FindProtectorPosition();
            }
            else
            {
                currentState = BotState.Advance;
            }
        }

        defendStartTime = Time.time;
        hasDestination = false;
        currentTargetLevel = null;
    }

    void Update()
    {
        if (!IsServer || agent == null) return;

        if (GameStateManager.Instance == null || !GameStateManager.Instance.IsPlaying())
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            return;
        }

        if (!agentReady)
        {
            PrepareAgent();
            if (!agentReady) return;
        }

        if (!agent.isOnNavMesh)
        {
            agentReady = false;
            return;
        }

        agent.isStopped = false;

        if (hasDestination &&
            Vector3.Distance(transform.position, currentDestination) > 1f &&
            Time.time - destinationSetTime > 5f)
        {
            hasDestination = false;
            currentTargetLevel = null;
        }

        if (Time.time >= nextDecisionTime)
        {
            nextDecisionTime = Time.time + decisionInterval;
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

            case BotState.Fight:
                UpdateFight();
                break;

            case BotState.DeliverCrystal:
                UpdateDeliverCrystal();
                break;

            case BotState.EscortCarrier:
                UpdateEscortCarrier();
                break;

            case BotState.HuntCarrier:
                UpdateHuntCarrier();
                break;

            case BotState.GuardDroppedCrystal:
                UpdateGuardDroppedCrystal();
                break;
        }
    }

    void UpdateDecision()
    {
        if (treeTarget == null) FindTree();
        if (levelTargets == null || levelTargets.Length == 0) FindLevels();

        // 1. إذا كان البوت يحمل الكريستالة، هدفه الوحيد إيصالها للمذبح
        if (CrystalItem.Instance != null && CrystalItem.Instance.IsCarrier(gameObject))
        {
            currentState = BotState.DeliverCrystal;
            return;
        }

        // 2. فحص قتال الأعداء
        if (currentState == BotState.Fight)
        {
            if (!IsEnemyStillValid()) LeaveFight();
            return;
        }

        // 3. الأولوية التكتيكية للكريستالة لكلا الفريقين
        if (CrystalItem.Instance != null)
        {
            if (team == PlayerMovement.TeamType.Protector)
            {
                if (CrystalItem.Instance.isCarried.Value)
                {
                    currentState = BotState.EscortCarrier;
                    return;
                }
                
                if (CrystalItem.Instance.isDropped.Value)
                {
                    SetNewDestination(CrystalItem.Instance.transform.position);
                    return;
                }
            }
            else // Destroyer
            {
                if (CrystalItem.Instance.isCarried.Value)
                {
                    GameObject carrier = CrystalItem.Instance.GetCarrierObject();
                    if (carrier != null)
                    {
                        EnterFight(carrier.transform);
                        return;
                    }
                }

                if (CrystalItem.Instance.isDropped.Value)
                {
                    currentState = BotState.GuardDroppedCrystal;
                    return;
                }
            }
        }

        Transform foundEnemy = FindClosestEnemy();
        if (foundEnemy != null)
        {
            EnterFight(foundEnemy);
            return;
        }

        if (currentState == BotState.Defend)
        {
            if (Time.time - defendStartTime > 4f)
            {
                currentState = BotState.Advance;
                hasDestination = false;
                currentTargetLevel = null;
            }
        }
    }

    Transform FindClosestEnemy()
    {
        CharacterCombat[] characters = FindObjectsByType<CharacterCombat>(FindObjectsSortMode.None);
        Transform closest = null;
        float closestDistanceSqr = detectionRange * detectionRange;

        foreach (CharacterCombat character in characters)
        {
            if (character == null || character.gameObject == gameObject) continue;
            if (character.team == team) continue;

            Health health = character.GetComponent<Health>();
            if (health == null || health.isDead) continue;

            float sqrDistance = (character.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance <= closestDistanceSqr)
            {
                closestDistanceSqr = sqrDistance;
                closest = character.transform;
            }
        }

        return closest;
    }

    void EnterFight(Transform target)
    {
        if (target == null) return;

        stateBeforeFight = currentState;
        currentState = BotState.Fight;
        enemyTarget = target;
        hasDestination = false;
        fightStartTime = Time.time;

        if (agent != null)
            agent.stoppingDistance = fightStoppingDistance;
    }

    // تم تحديث هذا الجزء لإضافة الهجوم الفعلي
    void UpdateFight()
    {
        if (enemyTarget == null)
        {
            LeaveFight();
            return;
        }

        float distance = Vector3.Distance(transform.position, enemyTarget.position);

        if (distance <= fightStoppingDistance)
        {
            // وصلنا إلى العدو، نتوقف ونبدأ الهجوم
            if (agent != null && agent.isOnNavMesh)
                agent.isStopped = true;

            CharacterCombat combat = GetComponent<CharacterCombat>();
            if (combat != null)
            {
                combat.TryAIAttack(enemyTarget);
            }
        }
        else
        {
            // ما زلنا نطارد العدو
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(enemyTarget.position);
            }
        }
    }

    bool IsEnemyStillValid()
    {
        if (enemyTarget == null) return false;

        Health health = enemyTarget.GetComponent<Health>();
        if (health == null || health.isDead) return false;

        CharacterCombat combat = enemyTarget.GetComponent<CharacterCombat>();
        if (combat == null || combat.team == team) return false;

        float distance = Vector3.Distance(transform.position, enemyTarget.position);

        if (distance > loseEnemyRange) return false;
        if (Time.time - fightStartTime > fightTimeout) return false;

        return true;
    }

    void LeaveFight()
    {
        enemyTarget = null;
        currentState = stateBeforeFight;
        hasDestination = false;
        currentTargetLevel = null;

        if (agent != null)
            agent.stoppingDistance = stoppingDistance;
    }

    void UpdateDefend()
    {
        if (treeTarget == null) return;

        if (!hasDestination)
        {
            FindProtectorPosition();
            SetNewDestination(protectorTarget);
        }
    }

    void UpdateAdvance()
    {
        Transform highestLevel = GetHighestCombatLevel();

        if (highestLevel == null)
        {
            if (!hasDestination)
                SetNewDestination(FindClosestTreePosition());
            return;
        }

        if (currentTargetLevel != highestLevel)
        {
            currentTargetLevel = highestLevel;
            Vector3 newTarget = FindPointOnLevel(highestLevel);
            SetNewDestination(newTarget);
            return;
        }

        if (!hasDestination)
        {
            Vector3 newTarget = FindPointOnLevel(highestLevel);
            SetNewDestination(newTarget);
        }
    }

    Transform GetHighestCombatLevel()
    {
        if (levelTargets == null) return null;

        for (int i = levelTargets.Length - 1; i >= 0; i--)
        {
            Transform level = levelTargets[i];
            if (level == null || !level.gameObject.activeInHierarchy) continue;
            return level;
        }

        return null;
    }

    void SetNewDestination(Vector3 destination)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        currentDestination = destination;
        hasDestination = true;
        destinationSetTime = Time.time;
        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(currentDestination);
    }

    Vector3 FindPointOnLevel(Transform level)
    {
        if (level == null) return transform.position;

        Renderer renderer = level.GetComponent<Renderer>();
        Vector3 center = level.position;
        float radius = 3f;

        if (renderer != null)
        {
            radius = Mathf.Min(renderer.bounds.extents.x, renderer.bounds.extents.z) * 0.65f;
            center = renderer.bounds.center;
        }

        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector2 random = Random.insideUnitCircle * radius;
            Vector3 candidate = new Vector3(center.x + random.x, center.y, center.z + random.y);

            if (treeTarget != null)
            {
                Vector3 horizontal = candidate - treeTarget.position;
                horizontal.y = 0f;

                if (horizontal.magnitude < treeAvoidRadius)
                    continue;
            }

            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidate, out hit, 3f, NavMesh.AllAreas))
                return hit.position;
        }

        NavMeshHit centerHit;
        if (NavMesh.SamplePosition(center, out centerHit, 5f, NavMesh.AllAreas))
            return centerHit.position;

        return transform.position;
    }

    void FindProtectorPosition()
    {
        if (treeTarget == null) return;

        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector2 circle = Random.insideUnitCircle;
            if (circle.sqrMagnitude < 0.01f) continue;

            circle.Normalize();
            float radius = Random.Range(minDefenseRadius, maxDefenseRadius);

            Vector3 candidate = treeTarget.position + new Vector3(circle.x, 0f, circle.y) * radius;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidate, out hit, 4f, NavMesh.AllAreas))
            {
                protectorTarget = hit.position;
                return;
            }
        }

        protectorTarget = transform.position;
    }

    Vector3 FindClosestTreePosition()
    {
        if (treeTarget == null) return transform.position;

        Vector3 direction = transform.position - treeTarget.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            direction = Vector3.forward;

        direction.Normalize();

        Vector3 desired = treeTarget.position + direction * 5f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(desired, out hit, 5f, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }

    // =========================
    // Crystal AI Behaviors
    // =========================

    void UpdateDeliverCrystal()
    {
        CrystalAltar altar = FindClosestAltar();
        if (altar != null)
        {
            SetNewDestination(altar.transform.position);
        }
        else if (treeTarget != null)
        {
            SetNewDestination(treeTarget.position);
        }

        if (CrystalItem.Instance != null)
        {
            Transform enemy = FindClosestEnemy();
            if (enemy != null && Vector3.Distance(transform.position, enemy.position) <= 5f)
            {
                CrystalItem.Instance.RequestShockwaveRpc();
            }
        }
    }

    void UpdateEscortCarrier()
    {
        if (CrystalItem.Instance == null || !CrystalItem.Instance.isCarried.Value)
        {
            currentState = BotState.Advance;
            return;
        }

        GameObject carrier = CrystalItem.Instance.GetCarrierObject();
        if (carrier == null)
        {
            currentState = BotState.Advance;
            return;
        }

        Vector3 escortPos = carrier.transform.position + carrier.transform.right * 3f;
        SetNewDestination(escortPos);
    }

    void UpdateHuntCarrier()
    {
        if (CrystalItem.Instance == null || !CrystalItem.Instance.isCarried.Value)
        {
            currentState = BotState.Advance;
            return;
        }

        GameObject carrier = CrystalItem.Instance.GetCarrierObject();
        if (carrier != null)
        {
            EnterFight(carrier.transform);
        }
        else
        {
            currentState = BotState.Advance;
        }
    }

    void UpdateGuardDroppedCrystal()
    {
        if (CrystalItem.Instance == null || !CrystalItem.Instance.isDropped.Value)
        {
            currentState = BotState.Advance;
            return;
        }

        Vector3 guardPos = CrystalItem.Instance.transform.position;
        if (Vector3.Distance(transform.position, guardPos) > 4f)
        {
            SetNewDestination(guardPos);
        }
    }

    CrystalAltar FindClosestAltar()
    {
        CrystalAltar[] altars = FindObjectsByType<CrystalAltar>(FindObjectsSortMode.None);
        CrystalAltar closest = null;
        float minDist = float.MaxValue;

        foreach (CrystalAltar altar in altars)
        {
            if (altar == null) continue;
            float dist = Vector3.Distance(transform.position, altar.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = altar;
            }
        }

        return closest;
    }
}