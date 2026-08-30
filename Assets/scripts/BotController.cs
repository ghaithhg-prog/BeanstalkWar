using UnityEngine;
using Unity.Netcode;

public class BotController : NetworkBehaviour
{
    [Header("Team")]
    public PlayerMovement.TeamType team;

    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Target")]
    public Transform treeTarget;

    private Rigidbody rb;

    // مكان دفاع الحامي
    private Vector3 protectorTarget;

    // هل تم تجهيز الهدف؟
    private bool targetInitialized = false;

    public override void OnNetworkSpawn()
    {
        // السيرفر فقط يتحكم في AI
        enabled = IsServer;

        if (!IsServer)
            return;

        rb = GetComponent<Rigidbody>();

        FindTree();

        InitializeTarget();
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

        // Protectors يقفون حول الشجرة
        if (team ==
            PlayerMovement.TeamType.Protector)
        {
            Vector2 randomCircle =
                Random.insideUnitCircle.normalized
                * Random.Range(5f, 8f);

            protectorTarget =
                treeTarget.position +
                new Vector3(
                    randomCircle.x,
                    0f,
                    randomCircle.y
                );
        }

        targetInitialized = true;
    }

    // =========================
    // Update
    // =========================

    void FixedUpdate()
    {
        if (!IsServer)
            return;

        // لا يتحرك قبل GO
        if (GameStateManager.Instance == null ||
            !GameStateManager.Instance.IsPlaying())
        {
            return;
        }

        if (treeTarget == null)
        {
            FindTree();

            if (treeTarget == null)
                return;
        }

        if (!targetInitialized)
            InitializeTarget();

        MoveBot();
    }

    // =========================
    // Movement
    // =========================

    void MoveBot()
    {
        Vector3 targetPosition;

        if (team ==
            PlayerMovement.TeamType.Destroyer)
        {
            // المدمر يتجه نحو الشجرة
            targetPosition =
                treeTarget.position;
        }
        else
        {
            // الحامي يتجه إلى موقع دفاعي
            targetPosition =
                protectorTarget;
        }

        Vector3 direction =
            targetPosition -
            transform.position;

        // حركة أفقية فقط
        direction.y = 0f;

        float distance =
            direction.magnitude;

        // توقف عندما يصل
        if (distance < 1.5f)
            return;

        direction.Normalize();

        Vector3 nextPosition =
            rb.position +
            direction *
            moveSpeed *
            Time.fixedDeltaTime;

        rb.MovePosition(
            nextPosition
        );

        // تدوير الـBot نحو اتجاه الحركة
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction
                );

            rb.MoveRotation(
                Quaternion.Slerp(
                    rb.rotation,
                    targetRotation,
                    8f *
                    Time.fixedDeltaTime
                )
            );
        }
    }
}